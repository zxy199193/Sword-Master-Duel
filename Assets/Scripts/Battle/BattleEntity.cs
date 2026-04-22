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

    [Header("Turn Temporary Data")]
    public float tempDamageReduction = 0;
    public float tempHitWidthModifier = 0;
    public bool isImmuneToSubSkills = false;

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
                currentStamina = GetFinalMaxStamina() / 2;
                if (roleData.equippedArmor != null) currentExtraLife = roleData.equippedArmor.durability;
                else currentExtraLife = 0;
            }
        }

        runtimeSkills.Clear();

        if (isPlayer && GameManager.Instance != null)
        {
            PlayerProfile profile = GameManager.Instance.playerProfile;
            Action<List<SkillSlot>> AddSlotsToRuntime = (sourceList) =>
            {
                if (sourceList == null) return;
                foreach (var slot in sourceList)
                {
                    if (slot == null || slot.skillData == null) continue;
                    SkillData inst = Instantiate(slot.skillData);
                    SkillSlot runtimeSlot = new SkillSlot { skillData = inst, level = slot.level, quantity = slot.quantity };
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
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.GetFinalStrength();

        int finalStr = roleData.strength;
        if (roleData.equippedWeapon != null) finalStr += roleData.equippedWeapon.bonusStrength;
        if (roleData.equippedArmor != null) finalStr += roleData.equippedArmor.bonusStrength;

        if (roleData.equippedAccessories != null)
        {
            foreach (var acc in roleData.equippedAccessories) if (acc != null) finalStr += acc.bonusStrength;
        }
        return finalStr;
    }

    public int GetFinalVitality()
    {
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.GetFinalVitality();

        int finalVit = roleData.vitality;
        if (roleData.equippedWeapon != null) finalVit += roleData.equippedWeapon.bonusVitality;
        if (roleData.equippedArmor != null) finalVit += roleData.equippedArmor.bonusVitality;

        if (roleData.equippedAccessories != null)
        {
            foreach (var acc in roleData.equippedAccessories) if (acc != null) finalVit += acc.bonusVitality;
        }
        return finalVit;
    }

    public int GetFinalEndurance()
    {
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.GetFinalEndurance();

        int finalEnd = roleData.endurance;
        if (roleData.equippedWeapon != null) finalEnd += roleData.equippedWeapon.bonusEndurance;
        if (roleData.equippedArmor != null) finalEnd += roleData.equippedArmor.bonusEndurance;

        if (roleData.equippedAccessories != null)
        {
            foreach (var acc in roleData.equippedAccessories) if (acc != null) finalEnd += acc.bonusEndurance;
        }
        return finalEnd;
    }

    public int GetFinalMentality()
    {
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.GetFinalMentality();
        int finalMen = roleData.mentality;
        if (roleData.equippedWeapon != null) finalMen += roleData.equippedWeapon.bonusMentality;
        if (roleData.equippedArmor != null) finalMen += roleData.equippedArmor.bonusMentality;
        if (roleData.equippedAccessories != null)
        {
            foreach (var acc in roleData.equippedAccessories) if (acc != null) finalMen += acc.bonusMentality;
        }
        return finalMen;
    }

    public int GetFinalMaxLife()
    {
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.GetFinalMaxLife();
        
        int total = roleData.maxBasicLife + GetFinalVitality() * 5;
        if (roleData.equippedWeapon != null) total += roleData.equippedWeapon.bonusLife;
        if (roleData.equippedArmor != null) total += roleData.equippedArmor.bonusLife;
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
            baseMax = roleData.maxStamina + GetFinalEndurance() * 2;
            if (roleData.equippedWeapon != null) baseMax += roleData.equippedWeapon.bonusStamina;
            if (roleData.equippedArmor != null) baseMax += roleData.equippedArmor.bonusStamina;
            if (roleData.equippedAccessories != null)
            {
                foreach (var acc in roleData.equippedAccessories) if (acc != null) baseMax += acc.bonusStamina;
            }
        }
        return Mathf.Max(1, baseMax - staminaMaxPenalty);
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

        if (activeStatuses.ContainsKey(StatusType.Dizzy))
        {
            float dizzyFluctuation = (Mathf.PerlinNoise(Time.time * 5f, 0f) - 0.5f) * 1f;
            statusMultiplier += dizzyFluctuation;
        }

        return globalBaseSpeed * mentalMultiplier * statusMultiplier;
    }

    public float GetFinalHitBarSlowdown(float globalBaseSlowdown)
    {
        float loadMultiplier = 1.0f;

        if (isPlayer && GameManager.Instance != null)
        {
            var loadState = GameManager.Instance.playerProfile.GetLoadWeightState();
            switch (loadState)
            {
                case GlobalBattleRules.LoadWeightState.Light: loadMultiplier = 1.3f; break;
                case GlobalBattleRules.LoadWeightState.Medium: loadMultiplier = 1.0f; break;
                case GlobalBattleRules.LoadWeightState.Heavy: loadMultiplier = 0.7f; break;
                case GlobalBattleRules.LoadWeightState.Extreme: loadMultiplier = 0.4f; break;
            }
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

    public void TakeDamage(int rawDamage)
    {
        int remainingDamage = rawDamage;
        if (currentExtraLife > 0)
        {
            if (currentExtraLife >= remainingDamage) 
            { 
                currentExtraLife -= remainingDamage; 
                remainingDamage = 0; 
            }
            else 
            { 
                remainingDamage -= currentExtraLife; 
                currentExtraLife = 0; 
                Debug.Log($"[{roleData.roleName}] 的防具破损！"); 
            }
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
                return; 
            }
        }
        OnHpChanged?.Invoke();
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
        int recoverAmount = roleData.staminaRecoverPerTurn + Mathf.FloorToInt(GetFinalMentality() / 6f);
        if (activeStatuses.ContainsKey(StatusType.Gathering))
        {
            recoverAmount += 1;
        }

        currentStamina += recoverAmount;

        int maxStam = GetFinalMaxStamina();
        if (currentStamina > maxStam) currentStamina = maxStam;

        OnStaminaChanged?.Invoke();
        TickStatuses(); 
    }

    public void AddStatus(StatusType type, int duration)
    {
        if (activeStatuses.ContainsKey(type)) activeStatuses[type] = Mathf.Max(activeStatuses[type], duration);
        else activeStatuses.Add(type, duration);
        OnStatusChanged?.Invoke();
    }

    public void TickStatuses()
    {
        var toRemove = new List<StatusType>();
        var keys = new List<StatusType>(activeStatuses.Keys);
        foreach (var key in keys)
        {
            activeStatuses[key]--;
            if (activeStatuses[key] <= 0)
            {
                toRemove.Add(key);

                if (key == StatusType.Overdrawn)
                {
                    staminaMaxPenalty += 10;
                    int newMax = GetFinalMaxStamina();
                    if (currentStamina > newMax) currentStamina = newMax;
                    OnStaminaChanged?.Invoke();
                    Debug.Log($"<color=#FF0000>[{roleData.roleName}] 的【透支】状态结束，体力上限永久扣除 10 点！</color>");
                }
            }
        }
        
        foreach (var key in toRemove) { activeStatuses.Remove(key); }
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