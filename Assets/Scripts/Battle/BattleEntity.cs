using UnityEngine;
using System;
using System.Collections.Generic;

public class BattleEntity : MonoBehaviour
{
    [Header("Core Data")]
    public RoleData roleData;
    public bool isPlayer;

    [Header("Runtime Stats (Read Only)")]
    public int currentBasicLife;
    public int currentExtraLife;
    public int currentStamina;

    public int staminaMaxPenalty = 0;

    [HideInInspector]
    public List<SkillSlot> runtimeSkills = new List<SkillSlot>();

    [HideInInspector]
    public float[] runtimeSubSkillProbabilities = new float[4];

    [HideInInspector]
    public Dictionary<SkillSlot, int[]> runtimeSkillWeights = new Dictionary<SkillSlot, int[]>();
    [HideInInspector] public SkillData lastUsedAttackSkill; // 用于“看破”状态判定
    [HideInInspector] public SkillSlot lockedNextTurnSkill; // 用于蓄力等跨回合技能的动作锁定

    [Header("Turn Temporary Data")]
    public float tempDamageReduction = 0;
    public float tempHitWidthModifier = 0;
    public bool isImmuneToSubSkills = false;
    public bool hasTakenDamageThisBattle = false;
    private HashSet<GlobalBattleRules.ApplyStatusOnLowHealthEquipEffect> triggeredLowHealthEffects = new HashSet<GlobalBattleRules.ApplyStatusOnLowHealthEquipEffect>();

    [Header("Status System")]
    public Dictionary<StatusType, int> activeStatuses = new Dictionary<StatusType, int>();

    [Header("Component References")]
    public Animator animator;

    public Action OnAnimHitPoint;
    public Action OnHpChanged;
    public Action OnStaminaChanged;
    public Action OnStatusChanged;
    public Action OnDeath;

    // ==========================================
    // Initialization
    // ==========================================

    public void Initialize(RoleData data, bool playerFlag)
    {
        roleData = data;
        isPlayer = playerFlag;
        staminaMaxPenalty = 0;
        hasTakenDamageThisBattle = false;
        triggeredLowHealthEffects.Clear();

        if (animator != null && roleData.animatorController != null)
        {
            animator.runtimeAnimatorController = roleData.animatorController;
        }

        if (isPlayer && GameManager.Instance != null)
        {
            currentBasicLife = GameManager.Instance.playerProfile.currentHp;
            currentStamina = GameManager.Instance.playerProfile.currentStamina;
            currentExtraLife = GameManager.Instance.playerProfile.currentExtraLife;

            int maxDurability = GameManager.Instance.playerProfile.equippedArmor != null ? GameManager.Instance.playerProfile.equippedArmor.durability : 0;
            if (currentExtraLife > maxDurability) currentExtraLife = maxDurability;
        }
        else
        {
            if (roleData != null)
            {
                currentBasicLife = GetFinalMaxLife();
                currentStamina = GetFinalMaxStamina();
                if (roleData.equippedArmor != null) currentExtraLife = roleData.equippedArmor.durability;
                else currentExtraLife = 0;
            }
        }

        runtimeSkills.Clear();
        activeStatuses.Clear();  // 清除上一场战斗残留的所有状态
        lockedNextTurnSkill = null;

        if (isPlayer && GameManager.Instance != null)
        {
            PlayerProfile profile = GameManager.Instance.playerProfile;
            bool hasUpgrade = profile.HasSkillUpgradeEffect();

            Action<List<SkillSlot>> AddSlotsToRuntime = (sourceList) =>
            {
                if (sourceList == null) return;
                foreach (var slot in sourceList)
                {
                    if (slot == null || slot.skillData == null) continue;
                    SkillData inst = Instantiate(slot.skillData);
                    SkillSlot runtimeSlot = new SkillSlot { skillData = inst, level = slot.level, quantity = slot.quantity, sourceSlot = slot };
                    
                    if (hasUpgrade && inst.skillType != SkillType.Item)
                    {
                        runtimeSlot.level = Mathf.Min(runtimeSlot.level + 1, 3);
                        if (inst.staminaCosts != null)
                        {
                            for (int i = 0; i < inst.staminaCosts.Length; i++)
                            {
                                inst.staminaCosts[i] = Mathf.Max(0, inst.staminaCosts[i] - 1);
                            }
                        }
                    }

                    runtimeSkills.Add(runtimeSlot);
                }
            };

            AddSlotsToRuntime(profile.equippedAttackSkills);
            AddSlotsToRuntime(profile.equippedDefendSkills);
            AddSlotsToRuntime(profile.equippedSpecialSkills);
            AddSlotsToRuntime(profile.equippedItems);
        }
        else
        {
            if (roleData != null)
            {
                if (roleData.subSkillProbabilities != null && roleData.subSkillProbabilities.Length == 4)
                {
                    float lastProb = 0f;
                    for (int i = 0; i < 4; i++)
                    {
                        if (roleData.subSkillProbabilities[i] != -1f) lastProb = roleData.subSkillProbabilities[i];
                        runtimeSubSkillProbabilities[i] = lastProb;
                    }
                }

                runtimeSkillWeights.Clear();
                if (roleData.npcSkills != null)
                {
                    foreach (var config in roleData.npcSkills)
                    {
                        if (config == null || config.skillSlot == null || config.skillSlot.skillData == null) continue;

                        SkillData inst = Instantiate(config.skillSlot.skillData);
                        SkillSlot runtimeSlot = new SkillSlot { skillData = inst, level = config.skillSlot.level, quantity = config.skillSlot.quantity };
                        runtimeSkills.Add(runtimeSlot);

                        int[] parsedWeights = new int[4];
                        int lastWeight = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            int configWeight = (config.phaseWeights != null && config.phaseWeights.Length > i) ? config.phaseWeights[i] : -1;
                            if (configWeight != -1) lastWeight = configWeight;
                            parsedWeights[i] = lastWeight;
                        }

                        runtimeSkillWeights.Add(runtimeSlot, parsedWeights);
                    }
                }
            }
        }

        OnHpChanged?.Invoke();
        OnStaminaChanged?.Invoke();
        CheckLowHealthEquipEffects();
    }

    // ==========================================
    // Public Data Getters
    // ==========================================

    public EquipmentData GetEquippedWeapon()
    {
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.equippedWeapon;
        return roleData.equippedWeapon;
    }

    public int GetFinalStrength()
    {
        int finalStr = 0;
        if (isPlayer && GameManager.Instance != null) finalStr = GameManager.Instance.playerProfile.GetFinalStrength();
        else
        {
            finalStr = roleData.strength;
            if (roleData.equippedWeapon != null) finalStr += roleData.equippedWeapon.bonusStrength;
            if (roleData.equippedArmor != null && currentExtraLife > 0) finalStr += roleData.equippedArmor.bonusStrength;

            if (roleData.equippedAccessories != null)
            {
                foreach (var acc in roleData.equippedAccessories) if (acc != null) finalStr += acc.bonusStrength;
            }
        }
        if (activeStatuses.ContainsKey(StatusType.Overclock)) finalStr = Mathf.FloorToInt(finalStr * 1.5f);
        return finalStr;
    }

    public int GetFinalVitality()
    {
        int finalVit = 0;
        if (isPlayer && GameManager.Instance != null) finalVit = GameManager.Instance.playerProfile.GetFinalVitality();
        else
        {
            finalVit = roleData.vitality;
            if (roleData.equippedWeapon != null) finalVit += roleData.equippedWeapon.bonusVitality;
            if (roleData.equippedArmor != null && currentExtraLife > 0) finalVit += roleData.equippedArmor.bonusVitality;

            if (roleData.equippedAccessories != null)
            {
                foreach (var acc in roleData.equippedAccessories) if (acc != null) finalVit += acc.bonusVitality;
            }
        }
        if (activeStatuses.ContainsKey(StatusType.Overclock)) finalVit = Mathf.FloorToInt(finalVit * 1.5f);
        return finalVit;
    }

    public int GetFinalEndurance()
    {
        int finalEnd = 0;
        if (isPlayer && GameManager.Instance != null) finalEnd = GameManager.Instance.playerProfile.GetFinalEndurance();
        else
        {
            finalEnd = roleData.endurance;
            if (roleData.equippedWeapon != null) finalEnd += roleData.equippedWeapon.bonusEndurance;
            if (roleData.equippedArmor != null && currentExtraLife > 0) finalEnd += roleData.equippedArmor.bonusEndurance;

            if (roleData.equippedAccessories != null)
            {
                foreach (var acc in roleData.equippedAccessories) if (acc != null) finalEnd += acc.bonusEndurance;
            }
        }
        if (activeStatuses.ContainsKey(StatusType.Overclock)) finalEnd = Mathf.FloorToInt(finalEnd * 1.5f);
        return finalEnd;
    }

    public int GetFinalMentality()
    {
        int finalMen = 0;
        if (isPlayer && GameManager.Instance != null) finalMen = GameManager.Instance.playerProfile.GetFinalMentality();
        else
        {
            finalMen = roleData.mentality;
            if (roleData.equippedWeapon != null) finalMen += roleData.equippedWeapon.bonusMentality;
            if (roleData.equippedArmor != null && currentExtraLife > 0) finalMen += roleData.equippedArmor.bonusMentality;
            if (roleData.equippedAccessories != null)
            {
                foreach (var acc in roleData.equippedAccessories) if (acc != null) finalMen += acc.bonusMentality;
            }
        }
        if (activeStatuses.ContainsKey(StatusType.Overclock)) finalMen = Mathf.FloorToInt(finalMen * 1.5f);
        return finalMen;
    }

    public List<EquipmentData> GetAllActiveEquips()
    {
        List<EquipmentData> allEquips = new List<EquipmentData>();
        if (GetEquippedWeapon() != null) allEquips.Add(GetEquippedWeapon());
        
        if (isPlayer && GameManager.Instance != null && GameManager.Instance.playerProfile.equippedArmor != null)
        {
            if (currentExtraLife > 0) allEquips.Add(GameManager.Instance.playerProfile.equippedArmor);
        }
        else if (roleData != null && roleData.equippedArmor != null && currentExtraLife > 0)
        {
            allEquips.Add(roleData.equippedArmor);
        }

        if (isPlayer && GameManager.Instance != null)
            allEquips.AddRange(GameManager.Instance.playerProfile.equippedAccessories);
        else if (roleData != null && roleData.equippedAccessories != null)
            allEquips.AddRange(roleData.equippedAccessories);

        return allEquips;
    }

    public bool HasEquipEffect<T>(out T foundEffect) where T : GlobalBattleRules.EquipEffect
    {
        foundEffect = null;
        var equips = GetAllActiveEquips();
        foreach (var eq in equips)
        {
            if (eq == null || eq.equipEffects == null) continue;
            foreach (var effect in eq.equipEffects)
            {
                if (effect is T typedEffect)
                {
                    foundEffect = typedEffect;
                    return true;
                }
            }
        }
        return false;
    }

    public List<T> GetAllEquipEffects<T>() where T : GlobalBattleRules.EquipEffect
    {
        List<T> list = new List<T>();
        var equips = GetAllActiveEquips();
        foreach (var eq in equips)
        {
            if (eq == null || eq.equipEffects == null) continue;
            foreach (var effect in eq.equipEffects)
            {
                if (effect is T typedEffect)
                {
                    list.Add(typedEffect);
                }
            }
        }
        return list;
    }

    public int GetFinalMaxLife()
    {
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.GetFinalMaxLife();
        
        int total = roleData.maxBasicLife + GetFinalVitality() * 6;
        if (roleData.equippedWeapon != null) total += roleData.equippedWeapon.bonusLife;
        if (roleData.equippedArmor != null && currentExtraLife > 0) total += roleData.equippedArmor.bonusLife;
        if (roleData.equippedAccessories != null)
        {
            foreach (var acc in roleData.equippedAccessories) if (acc != null) total += acc.bonusLife;
        }
        return total;
    }

    public int GetFinalMaxStamina()
    {
        int baseMax = 0;
        if (isPlayer && GameManager.Instance != null)
        {
            baseMax = GameManager.Instance.playerProfile.GetFinalMaxStamina();
        }
        else
        {
            baseMax = roleData.maxStamina + Mathf.FloorToInt(GetFinalEndurance() / 4f);
            if (roleData.equippedWeapon != null) baseMax += roleData.equippedWeapon.bonusStamina;
            if (roleData.equippedArmor != null) baseMax += roleData.equippedArmor.bonusStamina;
            if (roleData.equippedAccessories != null)
            {
                foreach (var acc in roleData.equippedAccessories) if (acc != null) baseMax += acc.bonusStamina;
            }
        }
        
        // 加上饰品的额外体力上限
        if (roleData.equippedAccessories != null)
        {
            foreach (var acc in roleData.equippedAccessories) if (acc != null) baseMax += acc.bonusStamina;
        }

        // Exhausted（虚脱）：体力上限压至 1
        if (activeStatuses.ContainsKey(StatusType.Exhausted))
            return 1;

        return Mathf.Max(1, baseMax - staminaMaxPenalty);
    }

    public int GetHpRecoverPerTurn()
    {
        return (GetFinalVitality() / 4) * 2;  // 每4点活力 +2 生命恢复
    }

    public float GetFinalActionTime(float baseTime)
    {
        if (activeStatuses.ContainsKey(StatusType.Impatient)) return baseTime * 0.4f;
        return baseTime;
    }

    public float GetFinalHitBarSpeed(float globalBaseSpeed)
    {
        int mentality = GetFinalMentality();

        float speedReduction = mentality * 0.03f; // 每点精神减慢3%
        float mentalMultiplier = Mathf.Max(0.2f, 1.0f - speedReduction);

        float statusMultiplier = 1.0f;
        if (activeStatuses.ContainsKey(StatusType.Tension)) statusMultiplier += 0.3f;
        if (activeStatuses.ContainsKey(StatusType.Focus)) statusMultiplier -= 0.3f;
        if (activeStatuses.ContainsKey(StatusType.Enraged)) statusMultiplier += 0.6f;

        if (activeStatuses.ContainsKey(StatusType.Dizzy))
        {
            float dizzyFluctuation = (Mathf.PerlinNoise(Time.time * 5f, 0f) - 0.5f) * 1f;
            statusMultiplier += dizzyFluctuation;
        }

        return globalBaseSpeed * mentalMultiplier * statusMultiplier;
    }

    public GlobalBattleRules.LoadWeightState GetEffectiveLoadState()
    {
        if (isPlayer && GameManager.Instance != null)
        {
            int currentLoad = GameManager.Instance.playerProfile.GetCurrentLoadWeight();
            if (activeStatuses.ContainsKey(StatusType.Frozen)) currentLoad += 6;
            if (activeStatuses.ContainsKey(StatusType.Lightweight)) currentLoad -= 20;
            
            int maxLoad = GameManager.Instance.playerProfile.GetMaxLoad();
            float ratio = maxLoad > 0 ? (float)currentLoad / maxLoad : 0f;

            if (ratio < 0.3f) return GlobalBattleRules.LoadWeightState.Light;
            if (ratio <= 1.0f) return GlobalBattleRules.LoadWeightState.Medium;
            if (ratio <= 1.5f) return GlobalBattleRules.LoadWeightState.Heavy;
            return GlobalBattleRules.LoadWeightState.Extreme;
        }
        return GlobalBattleRules.LoadWeightState.Medium;
    }

    public float GetLoadRatio()
    {
        if (isPlayer && GameManager.Instance != null)
        {
            int currentLoad = GameManager.Instance.playerProfile.GetCurrentLoadWeight();
            if (activeStatuses.ContainsKey(StatusType.Frozen)) currentLoad += 6;
            if (activeStatuses.ContainsKey(StatusType.Lightweight)) currentLoad -= 20;
            int maxLoad = GameManager.Instance.playerProfile.GetMaxLoad();
            return maxLoad > 0 ? (float)currentLoad / maxLoad : 0f;
        }
        return 0.5f; // 敌方无负重系统，默认返回中等
    }

    public float GetFinalHitBarSlowdown(float globalBaseSlowdown)
    {
        float loadMultiplier = 1.0f;
        var loadState = GetEffectiveLoadState();
        switch (loadState)
        {
            case GlobalBattleRules.LoadWeightState.Light: loadMultiplier = 1.3f; break;
            case GlobalBattleRules.LoadWeightState.Medium: loadMultiplier = 1.0f; break;
            case GlobalBattleRules.LoadWeightState.Heavy: loadMultiplier = 0.7f; break;
            case GlobalBattleRules.LoadWeightState.Extreme: loadMultiplier = 0.4f; break;
        }
        return globalBaseSlowdown * loadMultiplier;
    }

    // ==========================================
    // Combat & Status Logic
    // ==========================================

    public void ResetTurnData() 
    { 
        tempDamageReduction = 0; 
        tempHitWidthModifier = 0; 
        isImmuneToSubSkills = false; 
    }

    private bool HasFirstDamageImmunity()
    {
        List<EquipmentData> equips = new List<EquipmentData>();
        if (isPlayer && GameManager.Instance != null)
        {
            PlayerProfile profile = GameManager.Instance.playerProfile;
            if (profile.equippedWeapon != null) equips.Add(profile.equippedWeapon);
            if (profile.equippedArmor != null && currentExtraLife > 0) equips.Add(profile.equippedArmor);
            if (profile.equippedAccessories != null) equips.AddRange(profile.equippedAccessories);
        }
        else if (roleData != null)
        {
            if (roleData.equippedWeapon != null) equips.Add(roleData.equippedWeapon);
            if (roleData.equippedArmor != null && currentExtraLife > 0) equips.Add(roleData.equippedArmor);
            if (roleData.equippedAccessories != null) equips.AddRange(roleData.equippedAccessories);
        }

        foreach (var eq in equips)
        {
            if (eq == null || eq.equipEffects == null) continue;
            foreach (var eff in eq.equipEffects)
            {
                if (eff is GlobalBattleRules.FirstDamageImmunityEquipEffect) return true;
            }
        }
        return false;
    }

    public bool TakeDamage(int rawDamage, bool ignoreShield = false)
    {
        if (rawDamage > 0 && !hasTakenDamageThisBattle)
        {
            if (HasFirstDamageImmunity())
            {
                hasTakenDamageThisBattle = true;
                BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=#00FFFF>首伤免疫!</color>");
                Debug.Log($"<color=#00FFFF>[{roleData.roleName}] 触发首伤免疫！</color>");
                return false;
            }
            hasTakenDamageThisBattle = true;
        }

        // 分身防御拦截
        if (rawDamage > 0 && activeStatuses.ContainsKey(StatusType.Clone))
        {
            activeStatuses.Remove(StatusType.Clone);
            OnStatusChanged?.Invoke();
            BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
            if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=cyan>分身抵挡!</color>");
            Debug.Log($"<color=cyan>[{roleData.roleName}] 的【分身】抵挡了一次伤害并消失了。</color>");
            return false;
        }

        if (ignoreShield)
        {
            if (activeStatuses.ContainsKey(StatusType.Tenacious) && currentBasicLife > 1 && rawDamage >= currentBasicLife)
            {
                rawDamage = currentBasicLife - 1;
                Debug.Log($"<color=#FF0000>[{roleData.roleName}] 触发了【坚挺】，强行锁血 1 点！</color>");
            }
            currentBasicLife -= rawDamage;
            if (currentBasicLife <= 0) 
            { 
                currentBasicLife = 0; 
                OnHpChanged?.Invoke(); 
                Die(); 
                PlayDieAnim(); 
                return true; 
            }
            OnHpChanged?.Invoke();
            CheckLowHealthEquipEffects();
            return true;
        }

        int remainingDamage = rawDamage;
        if (currentExtraLife > 0)
        {
            int actualDmg = Mathf.Min(remainingDamage, currentExtraLife);
            currentExtraLife -= actualDmg;
            remainingDamage -= actualDmg;
            
            if (currentExtraLife <= 0)
            {
                currentExtraLife = 0;
                BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=red>防具损坏!</color>");
                Debug.Log($"<color=red>[{roleData.roleName}] 的防具彻底损坏了！</color>");
            }
            
            OnHpChanged?.Invoke();
        }

        if (remainingDamage > 0)
        {
            if (activeStatuses.ContainsKey(StatusType.Tenacious) && currentBasicLife > 1 && remainingDamage >= currentBasicLife)
            {
                remainingDamage = currentBasicLife - 1;
                Debug.Log($"<color=#FF0000>[{roleData.roleName}] 触发了【坚挺】，强行锁血 1 点！</color>");
            }

            currentBasicLife -= remainingDamage;
            if (currentBasicLife <= 0) 
            { 
                currentBasicLife = 0; 
                OnHpChanged?.Invoke(); 
                Die(); 
                PlayDieAnim(); 
                return true; 
            }
        }
        OnHpChanged?.Invoke();
        CheckLowHealthEquipEffects();
        return true;
    }

    public bool ConsumeStamina(int amount)
    {
        if (currentStamina >= amount) 
        { 
            currentStamina -= amount; 
            OnStaminaChanged?.Invoke(); 
            return true; 
        }
        return false;
    }

    public void RecoverStamina()
    {
        // 记录恢复前的数值用于判断
        int oldStamina = currentStamina;
        int oldHp = currentBasicLife;

        // 每回合体力恢复
        int recoverAmount = roleData.staminaRecoverPerTurn + Mathf.FloorToInt(GetFinalEndurance() / 8f);
        if (activeStatuses.ContainsKey(StatusType.Gathering))
        {
            recoverAmount += 1;
        }
        currentStamina += recoverAmount;
        int maxStam = GetFinalMaxStamina();
        if (currentStamina > maxStam) currentStamina = maxStam;

        // 每回合生命恢复
        int hpRecover = GetHpRecoverPerTurn();
        if (activeStatuses.ContainsKey(StatusType.Recover)) hpRecover += 3;

        if (hpRecover > 0)
        {
            currentBasicLife = Mathf.Min(currentBasicLife + hpRecover, GetFinalMaxLife());
            OnHpChanged?.Invoke();
        }

        // 显示恢复飘字
        BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
        if (bm != null)
        {
            int staminaGained = currentStamina - oldStamina;
            int hpGained = currentBasicLife - oldHp;

            if (hpGained > 0) bm.SpawnRecoverPopup(isPlayer, $"<color=lime>生命 +{hpGained}</color>", true);
            if (staminaGained > 0) bm.SpawnRecoverPopup(isPlayer, $"<color=yellow>体力 +{staminaGained}</color>", false);
        }

        OnStaminaChanged?.Invoke();
        TickStatuses(); 
    }

    public void AddStatus(StatusType type, int duration, BattleEntity source = null)
    {
        // 加护免疫来自对方的负面状态
        if (source != null && source != this && activeStatuses.ContainsKey(StatusType.Protection) && IsNegativeStatus(type))
        {
            Debug.Log($"[{roleData.roleName}] 的加护免疫了来自对手的负面状态 {type}！");
            BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
            if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=yellow>加护免疫</color>");
            return;
        }

        // 免疫检查
        if (IsImmuneTo(type))
        {
            Debug.Log($"[{roleData.roleName}] 免疫了 {type} 效果！");
            return;
        }

        int finalDuration = duration;
        if (IsNegativeStatus(type))
        {
            int reduceTurns = GetDebuffDurationReduction();
            finalDuration -= reduceTurns;
            if (finalDuration <= 0)
            {
                Debug.Log($"[{roleData.roleName}] 通过装备效果抵消了 {type} 效果！");
                BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=#00FFFF>抵抗!</color>");
                return;
            }
        }

        if (activeStatuses.ContainsKey(type)) activeStatuses[type] = Mathf.Max(activeStatuses[type], finalDuration);
        else activeStatuses.Add(type, finalDuration);
        OnStatusChanged?.Invoke();
    }

    private bool IsNegativeStatus(StatusType type)
    {
        return type == StatusType.Dizzy || type == StatusType.Impatient || type == StatusType.Burn ||
               type == StatusType.Paralyzed || type == StatusType.Frozen || type == StatusType.Spikes ||
               type == StatusType.Smoked || type == StatusType.Exhausted || type == StatusType.Overdrawn ||
               type == StatusType.Cursed || type == StatusType.Poisoned;
    }

    private int GetDebuffDurationReduction()
    {
        List<EquipmentData> allEquips = GetAllActiveEquips();
        int totalReduce = 0;
        foreach (var equip in allEquips)
        {
            if (equip == null || equip.equipEffects == null) continue;
            foreach (var effect in equip.equipEffects)
            {
                if (effect is GlobalBattleRules.ReduceDebuffDurationEquipEffect reduceEffect)
                {
                    totalReduce += reduceEffect.reduceTurns;
                }
            }
        }
        return totalReduce;
    }

    private void CheckLowHealthEquipEffects()
    {
        if (currentBasicLife <= 0) return;

        float hpPercent = (float)currentBasicLife / GetFinalMaxLife() * 100f;

        List<EquipmentData> allEquips = GetAllActiveEquips();
        foreach (var equip in allEquips)
        {
            if (equip == null || equip.equipEffects == null) continue;
            foreach (var effect in equip.equipEffects)
            {
                if (effect is GlobalBattleRules.ApplyStatusOnLowHealthEquipEffect lowHealthEffect)
                {
                    if (hpPercent <= lowHealthEffect.hpThresholdPercent)
                    {
                        if (!triggeredLowHealthEffects.Contains(lowHealthEffect))
                        {
                            triggeredLowHealthEffects.Add(lowHealthEffect);
                            AddStatus(lowHealthEffect.statusType, lowHealthEffect.duration);
                            BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                            if (bm != null) bm.SpawnGeneralPopup(isPlayer, $"[{lowHealthEffect.statusType}]");
                        }
                    }
                    else
                    {
                        // hp is above threshold, reset the trigger
                        triggeredLowHealthEffects.Remove(lowHealthEffect);
                    }
                }
            }
        }
    }

    private bool IsImmuneTo(StatusType type)
    {
        // 检查所有装备的效果
        List<EquipmentData> allEquips = new List<EquipmentData>();
        if (GetEquippedWeapon() != null) allEquips.Add(GetEquippedWeapon());
        
        // 获取防具
        if (isPlayer && GameManager.Instance != null && GameManager.Instance.playerProfile.equippedArmor != null)
        {
            if (currentExtraLife > 0) allEquips.Add(GameManager.Instance.playerProfile.equippedArmor);
        }
        else if (roleData.equippedArmor != null && currentExtraLife > 0)
        {
            allEquips.Add(roleData.equippedArmor);
        }

        // 获取饰品
        if (isPlayer && GameManager.Instance != null)
            allEquips.AddRange(GameManager.Instance.playerProfile.equippedAccessories);
        else if (roleData.equippedAccessories != null)
            allEquips.AddRange(roleData.equippedAccessories);

        foreach (var equip in allEquips)
        {
            if (equip == null || equip.equipEffects == null) continue;
            foreach (var effect in equip.equipEffects)
            {
                if (effect is GlobalBattleRules.StatusImmunityEquipEffect immunityEffect)
                {
                    if (immunityEffect.immuneStatus == type) return true;
                }
            }
        }
        return false;
    }

    public void TickStatuses()
    {
        var toRemove = new List<StatusType>();
        var keys = new List<StatusType>(activeStatuses.Keys);
        foreach (var key in keys)
        {
            // 中毒：每回合开始基础生命值-3
            if (key == StatusType.Poisoned)
            {
                currentBasicLife -= 3;
                if (currentBasicLife < 0) currentBasicLife = 0;
                OnHpChanged?.Invoke();
                BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=green>中毒 -3</color>");
                if (currentBasicLife <= 0) { Die(); PlayDieAnim(); }
            }

            // 灼烧特殊逻辑：每回合扣除额外生命，若没了则熄灭
            if (key == StatusType.Burn)
            {
                // 分身可以抵挡灼烧伤害
                if (activeStatuses.ContainsKey(StatusType.Clone))
                {
                    activeStatuses.Remove(StatusType.Clone);
                    OnStatusChanged?.Invoke();
                    BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                    if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=cyan>分身抵挡!</color>");
                    Debug.Log($"<color=cyan>[{roleData.roleName}] 的【分身】抵挡了灼烧伤害！</color>");
                }
                else
                {
                    int burnDmg = 10;
                    if (currentExtraLife > 0)
                    {
                        BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                        int actualBurn = Mathf.Min(currentExtraLife, burnDmg);
                        currentExtraLife -= actualBurn;
                        
                        if (currentExtraLife <= 0)
                        {
                            currentExtraLife = 0;
                            if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=red>防具损坏!</color>");
                            Debug.Log($"<color=red>[{roleData.roleName}] 的防具在灼烧中损坏了！</color>");
                        }

                        OnHpChanged?.Invoke();
                        
                        if (bm != null) bm.SpawnGeneralPopup(isPlayer, $"<color=#FF4500>灼烧 -{actualBurn}</color>");
                    }
                }
                
                if (currentExtraLife <= 0)
                {
                    if (!toRemove.Contains(key)) toRemove.Add(key);
                    Debug.Log($"<color=#FF4500>[{roleData.roleName}] 的【灼烧】因为失去额外生命而熄灭了。</color>");
                    continue; // 状态已移除，不再进行常规时长扣减
                }
            }

            activeStatuses[key]--;
            if (activeStatuses[key] <= 0)
            {
                if (!toRemove.Contains(key)) toRemove.Add(key);

                if (key == StatusType.Overdrawn)
                {
                    // 透支结束：施加 2 回合虚脱，期间体力上限压至 1
                    BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                    AddStatus(StatusType.Exhausted, 2);
                    int newMax = GetFinalMaxStamina(); // 此时已含虚脱上限=1
                    if (currentStamina > newMax) currentStamina = newMax;
                    OnStaminaChanged?.Invoke();
                    if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=#FF6600>虚脱!</color>");
                    Debug.Log($"<color=#FF0000>[{roleData.roleName}] 的【透支】结束，进入 2 回合虚脱，体力上限压至 1！</color>");
                }
                
                if (key == StatusType.Overclock)
                {
                    // 超频结束：进入2回合虚脱，上限压至1
                    BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                    AddStatus(StatusType.Exhausted, 2);
                    int newMax = GetFinalMaxStamina(); // 会读取刚才加上的状态返回 1
                    if (currentStamina > newMax) currentStamina = newMax;
                    OnStaminaChanged?.Invoke();
                    if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=#FF6600>虚脱!</color>");
                    Debug.Log($"<color=#FF0000>[{roleData.roleName}] 的【超频】结束，进入 2 回合虚脱，体力上限压至 1！</color>");
                }
            }
        }
        
        foreach (var key in toRemove) 
        { 
            activeStatuses.Remove(key); 
            if (key == StatusType.Cursed)
            {
                TakeDamage(999);
                BattleManager bm = GameObject.FindObjectOfType<BattleManager>();
                if (bm != null) bm.SpawnGeneralPopup(isPlayer, "<color=#800080>诅咒爆发!</color>");
            }
        }
        OnStatusChanged?.Invoke();
    }

    public float GetHitBarWidthModifier()
    {
        float modifier = 0f;
        if (activeStatuses.ContainsKey(StatusType.Tension)) modifier -= 6f;
        if (activeStatuses.ContainsKey(StatusType.Focus)) modifier += 6f;
        if (activeStatuses.ContainsKey(StatusType.Smoked)) modifier -= 10f;
        return modifier;
    }

    public void TriggerHitPoint() => OnAnimHitPoint?.Invoke();
    public void PlayAnim(string animName) { if (animator != null) animator.SetTrigger(animName); }
    public void PlayHitAnim() => PlayAnim("Hit");
    public void PlayMissAnim() => PlayAnim("Miss");
    public void PlayDieAnim() => PlayAnim("Die");
    private void Die() { OnDeath?.Invoke(); }
}