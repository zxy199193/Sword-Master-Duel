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

    [HideInInspector]
    public List<SkillSlot> runtimeSkills = new List<SkillSlot>();

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

    public void Initialize(RoleData data, bool playerFlag)
    {
        roleData = data;
        isPlayer = playerFlag;

        // ==========================================
        // 【核心新增】：动态替换专属动画控制器！
        // ==========================================
        if (animator != null && roleData.animatorController != null)
        {
            animator.runtimeAnimatorController = roleData.animatorController;
        }

        if (isPlayer && GameManager.Instance != null)
        {
            currentBasicLife = GameManager.Instance.playerProfile.currentHp;
            currentStamina = GameManager.Instance.playerProfile.currentStamina;

            // 继承上一局打完剩下的护盾
            currentExtraLife = GameManager.Instance.playerProfile.currentExtraLife;

            int maxDurability = GameManager.Instance.playerProfile.equippedArmor != null ? GameManager.Instance.playerProfile.equippedArmor.durability : 0;
            if (currentExtraLife > maxDurability) currentExtraLife = maxDurability;
        }
        else
        {
            if (roleData != null)
            {
                currentBasicLife = roleData.maxBasicLife;
                currentStamina = roleData.maxStamina / 2;
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
            if (roleData != null && roleData.equippedSkills != null)
            {
                foreach (var slot in roleData.equippedSkills)
                {
                    if (slot == null || slot.skillData == null) continue;
                    SkillData inst = Instantiate(slot.skillData);
                    SkillSlot runtimeSlot = new SkillSlot { skillData = inst, level = slot.level, quantity = slot.quantity };
                    runtimeSkills.Add(runtimeSlot);
                }
            }
        }

        OnHpChanged?.Invoke();
        OnStaminaChanged?.Invoke();
    }

    // ==========================================
    // 统一属性获取接口
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

    // 【保留修复】：动态体力上限
    public int GetFinalMaxStamina()
    {
        if (isPlayer && GameManager.Instance != null) return GameManager.Instance.playerProfile.GetFinalMaxStamina();
        return roleData.maxStamina;
    }

    // ==========================================
    // 打击条速度计算 (加入眩晕波动)
    // ==========================================
    public float GetFinalHitBarSpeed(float globalBaseSpeed)
    {
        int mentality = GetFinalMentality();

        // 公式：每有 1 点精神，速度减 2%。限制最多减速到 20%
        float speedReduction = mentality * 0.02f;
        float mentalMultiplier = Mathf.Max(0.2f, 1.0f - speedReduction);

        // 叠加状态效果(紧张/专注)
        float statusMultiplier = 1.0f;
        if (activeStatuses.ContainsKey(StatusType.Tension)) statusMultiplier += 0.3f;
        if (activeStatuses.ContainsKey(StatusType.Focus)) statusMultiplier -= 0.3f;

        // ==========================================
        // 【核心新增】：眩晕状态的忽快忽慢逻辑
        // ==========================================
        if (activeStatuses.ContainsKey(StatusType.Dizzy))
        {
            // 使用柏林噪声 (PerlinNoise) 生成随时间连续变化的随机数
            // Mathf.PerlinNoise 返回 0~1。减去 0.5 变成 -0.5~0.5，再乘以 0.6 变成 -0.3~0.3 (即 ±30%)
            // Time.time * 3f 控制的是波动的剧烈程度，数字越大速度变化越快
            float dizzyFluctuation = (Mathf.PerlinNoise(Time.time * 5f, 0f) - 0.5f) * 1f;
            statusMultiplier += dizzyFluctuation;
        }

        return globalBaseSpeed * mentalMultiplier * statusMultiplier;
    }

    public float GetFinalHitBarSlowdown(float globalBaseSlowdown)
    {
        float loadMultiplier = 1.0f;

        // 仅玩家受负重影响，敌人默认 1.0
        if (isPlayer && GameManager.Instance != null)
        {
            var loadState = GameManager.Instance.playerProfile.GetLoadWeightState();
            switch (loadState)
            {
                case GlobalBattleRules.LoadWeightState.Light: loadMultiplier = 1.3f; break;     // +30%
                case GlobalBattleRules.LoadWeightState.Medium: loadMultiplier = 1.0f; break;    // 不变
                case GlobalBattleRules.LoadWeightState.Heavy: loadMultiplier = 0.7f; break;     // -30%
                case GlobalBattleRules.LoadWeightState.Extreme: loadMultiplier = 0.4f; break;   // -60%
            }
        }
        return globalBaseSlowdown * loadMultiplier;
    }

    // ==========================================
    // 战斗与状态逻辑
    // ==========================================
    public void ResetTurnData() { tempDamageReduction = 0; tempHitWidthModifier = 0; isImmuneToSubSkills = false; }
    public void TakeDamage(int rawDamage)
    {
        int remainingDamage = rawDamage;
        if (currentExtraLife > 0)
        {
            if (currentExtraLife >= remainingDamage) { currentExtraLife -= remainingDamage; remainingDamage = 0; }
            else { remainingDamage -= currentExtraLife; currentExtraLife = 0; Debug.Log($"[{roleData.roleName}] 的防具破损！"); }
        }
        if (remainingDamage > 0)
        {
            currentBasicLife -= remainingDamage;
            if (currentBasicLife <= 0) { currentBasicLife = 0; OnHpChanged?.Invoke(); Die(); PlayDieAnim(); return; }
        }
        OnHpChanged?.Invoke();
    }

    public bool ConsumeStamina(int amount)
    {
        if (currentStamina >= amount) { currentStamina -= amount; OnStaminaChanged?.Invoke(); return true; }
        return false;
    }

    public void RecoverStamina()
    {
        int recoverAmount = roleData.staminaRecoverPerTurn;
        if (activeStatuses.ContainsKey(StatusType.Gathering))
        {
            recoverAmount += 1;
        }

        currentStamina += recoverAmount;

        // 使用加点和装备后的最终体力上限钳制
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
        foreach (var key in keys) { activeStatuses[key]--; if (activeStatuses[key] <= 0) toRemove.Add(key); }
        foreach (var key in toRemove) { activeStatuses.Remove(key); }
        OnStatusChanged?.Invoke();
    }

    public float GetHitBarWidthModifier()
    {
        float modifier = 0f;
        if (activeStatuses.ContainsKey(StatusType.Tension)) modifier -= 6f;
        if (activeStatuses.ContainsKey(StatusType.Focus)) modifier += 6f;
        return modifier;
    }

    public void TriggerHitPoint() => OnAnimHitPoint?.Invoke();
    public void PlayAnim(string animName) { if (animator != null) animator.SetTrigger(animName); }
    public void PlayHitAnim() => PlayAnim("Hit");
    public void PlayMissAnim() => PlayAnim("Miss");
    public void PlayDieAnim() => PlayAnim("Die");
    private void Die() { OnDeath?.Invoke(); }
}