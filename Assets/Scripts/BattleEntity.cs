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
    public List<SkillData> runtimeSkills = new List<SkillData>(); // 运行时专属技能库

    [Header("Turn Temporary Data")]
    public float tempDamageReduction = 0;
    public float tempHitWidthModifier = 0;

    [Header("Status System")]
    public Dictionary<StatusType, int> activeStatuses = new Dictionary<StatusType, int>();

    [Header("Component References")]
    public Animator animator;


    // ==========================================
    // Events / Delegates
    // ==========================================
    public Action OnAnimHitPoint;
    public Action OnHpChanged;
    public Action OnStaminaChanged;
    public Action OnStatusChanged;
    public Action OnDeath;

    // ==========================================
    // 公共接口：初始化与回合管理
    // ==========================================

    public void Initialize(RoleData data, bool playerFlag)
    {
        roleData = data;
        isPlayer = playerFlag;

        currentBasicLife = roleData.maxBasicLife;
        currentExtraLife = 20; // TODO: 后期接入装备系统后修改
        currentStamina = roleData.maxStamina / 2;

        // ==========================================
        // 【核心新增】：基于配置表，动态克隆并生成运行时专属技能库
        // ==========================================
        runtimeSkills.Clear();
        if (roleData != null && roleData.equippedSkills != null)
        {
            foreach (var slot in roleData.equippedSkills)
            {
                if (slot.skillData == null) continue;

                // 实例化一个内存中的独立 ScriptableObject，防止污染原始资产
                SkillData inst = Instantiate(slot.skillData);
                // 盖上该角色专属的等级印章！
                inst.skillLevel = slot.level;

                runtimeSkills.Add(inst);
            }
        }

        OnHpChanged?.Invoke();
        OnStaminaChanged?.Invoke();
    }

    public void ResetTurnData()
    {
        tempDamageReduction = 0;
        tempHitWidthModifier = 0;
    }

    // ==========================================
    // 公共接口：核心数值运算 (HP & Stamina)
    // ==========================================

    public void TakeDamage(int rawDamage)
    {
        int remainingDamage = rawDamage;

        // 优先扣除防具/护盾额外生命
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

        // 扣除基础生命
        if (remainingDamage > 0)
        {
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
        Debug.LogWarning($"[{roleData.roleName}] 体力不足！");
        return false;
    }

    public void RecoverStamina()
    {
        currentStamina += roleData.staminaRecoverPerTurn;
        if (currentStamina > roleData.maxStamina)
        {
            currentStamina = roleData.maxStamina;
        }

        OnStaminaChanged?.Invoke();
        TickStatuses();
    }

    // ==========================================
    // 公共接口：状态系统 (Buff / Debuff)
    // ==========================================

    public void AddStatus(StatusType type, int duration)
    {
        if (activeStatuses.ContainsKey(type))
        {
            // 同状态叠加规则：刷新持续时间并取最大值
            activeStatuses[type] = Mathf.Max(activeStatuses[type], duration);
        }
        else
        {
            activeStatuses.Add(type, duration);
        }
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
            }
        }

        foreach (var key in toRemove)
        {
            activeStatuses.Remove(key);
            Debug.Log($"[{roleData.roleName}] 的 [{key}] 状态已解除。");
        }
        OnStatusChanged?.Invoke();
    }

    public float GetHitBarSpeedMultiplier()
    {
        float multiplier = 1.0f;
        if (activeStatuses.ContainsKey(StatusType.Tension)) multiplier += 0.3f;
        if (activeStatuses.ContainsKey(StatusType.Focus)) multiplier -= 0.3f;

        return Mathf.Max(0.1f, multiplier); // 保底机制：防止速度为非正数
    }

    public float GetHitBarWidthModifier()
    {
        float modifier = 0f;
        if (activeStatuses.ContainsKey(StatusType.Tension)) modifier -= 6f;
        if (activeStatuses.ContainsKey(StatusType.Focus)) modifier += 6f;

        return modifier;
    }

    // ==========================================
    // 公共接口：动画与演出驱动
    // ==========================================

    public void TriggerHitPoint()
    {
        OnAnimHitPoint?.Invoke();
    }

    public void PlayAnim(string animName)
    {
        if (animator != null)
        {
            animator.SetTrigger(animName);
        }
    }

    public void PlayHitAnim() => PlayAnim("Hit");
    public void PlayMissAnim() => PlayAnim("Miss");
    public void PlayDieAnim() => PlayAnim("Die");

    // ==========================================
    // 内部私有逻辑
    // ==========================================

    private void Die()
    {
        Debug.Log($"<color=red>【Combat】 {roleData.roleName} 阵亡！</color>");
        OnDeath?.Invoke();
    }
}