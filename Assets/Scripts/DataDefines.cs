using System;
using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 核心枚举 (Enums)
// ==========================================
public enum SkillType { Attack, Defend, Dodge, Special, Item }
public enum SectionLevel { Level0, Level1, Level2, Level3, Level4, Level5, Level6, Level99 }
public enum StatusType { Tension, Focus }
public enum AttributeType { Strength, Mentality, Stamina }

// ==========================================
// 核心数据结构 (Structs)
// ==========================================

[Serializable]
public struct HitSection
{
    [Tooltip("区间等级")]
    public SectionLevel level;
    [Tooltip("区间中轴线位置 (0-100)")]
    public float axisPosition;
    [Tooltip("区间宽度")]
    public float width;
}

[Serializable]
public struct HitBarConfig
{
    [Tooltip("滑块基础移动速度")]
    public float baseSpeed;
    [Tooltip("斩击后的基础减速度")]
    public float baseSlowdown;
    [Tooltip("操作倒计时 (秒)")]
    public float actionTime;
    [Tooltip("区间列表 (从上层往下层配置，高等级放前面)")]
    public HitSection[] sections;
}

[Serializable]
public struct SkillWeight
{
    [Tooltip("要分配权重的技能/道具资产")]
    public SkillData skill;
    [Tooltip("被抽中的权重 (例如100和50，前者抽中概率是后者的两倍。0则不会被抽中)")]
    public int weight;
}

[Serializable]
public struct AIPhaseConfig
{
    [Tooltip("生命值低于此百分比时进入该阶段 (0.0~1.0，例如 0.5 代表半血以下)")]
    [Range(0f, 1f)]
    public float hpPercentageThreshold;

    [Tooltip("该阶段使用副技能(道具/特殊技能)的基础概率 (0.0~1.0)")]
    [Range(0f, 1f)]
    public float subSkillProbability;

    [Tooltip("该阶段的主技能(攻击/防守/闪避)权重池")]
    public List<SkillWeight> mainSkillWeights;

    [Tooltip("该阶段的副技能(道具/特殊)权重池")]
    public List<SkillWeight> subSkillWeights;
}

// ==========================================
// 多态技能特效 (Skill Effects) - 已升级为动态等级数组版
// ==========================================

[Serializable]
public abstract class SkillEffect
{
    // 【核心改动】：所有特效的执行方法现在必须接收 skillLevel 参数
    public abstract void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel);
}

[Serializable]
public class HealEffect : SkillEffect
{
    [Tooltip("各等级下回复的生命值 (请填入3个值)")]
    public int[] healAmounts = new int[3];

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 动态根据等级获取数值，如果是道具默认拿第1个
        int idx = Mathf.Clamp(skillLevel - 1, 0, healAmounts.Length - 1);
        int finalHeal = healAmounts.Length > 0 ? healAmounts[idx] : 0;

        caster.currentBasicLife = Mathf.Min(caster.roleData.maxBasicLife, caster.currentBasicLife + finalHeal);

        // 【核心修复 1】：必须呼叫这个事件，左上角的血条 UI 才会跟着涨！
        caster.OnHpChanged?.Invoke();

        // 【核心修复 2】：使用最新的 bool 传参方法。如果是玩家用的，就飘在玩家头上。
        manager.SpawnDamagePopup(caster.isPlayer, $"+{finalHeal}", 2);
    }
}

[Serializable]
public class RecoverStaminaEffect : SkillEffect
{
    [Tooltip("各等级下回复的体力值 (请填入3个值)")]
    public int[] staminaAmounts = new int[3];

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, staminaAmounts.Length - 1);
        int finalStamina = staminaAmounts.Length > 0 ? staminaAmounts[idx] : 0;

        // 核心逻辑：恢复体力并限制不超过最大上限
        caster.currentStamina = Mathf.Min(caster.roleData.maxStamina, caster.currentStamina + finalStamina);

        // 【关键】：呼叫体力改变事件，刷新 UI 面板上的体力条/数值
        caster.OnStaminaChanged?.Invoke();

        // 表现层：在头上飘字。这里用 <color> 标签把字变成了蓝色(SP常用的颜色)，以区分加血的绿字
        manager.SpawnDamagePopup(caster.isPlayer, $"<color=#00FFFF>+{finalStamina} SP</color>", 1);

        Debug.Log($"[RecoverStaminaEffect] {caster.roleData.roleName} 恢复了 {finalStamina} 点体力！");
    }
}


[Serializable]
public class DirectDamageEffect : SkillEffect
{
    [Tooltip("各等级下直接造成的伤害值 (请填入3个值)")]
    public int[] damageAmounts = new int[3];

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, damageAmounts.Length - 1);
        int finalDamage = damageAmounts.Length > 0 ? damageAmounts[idx] : 0;

        target.TakeDamage(finalDamage);
        target.PlayHitAnim();

        // 【核心修复】：如果是敌人被打，就在敌人头上飘字
        manager.SpawnDamagePopup(target.isPlayer, finalDamage.ToString(), 2);

        if (target.currentBasicLife <= 0) target.PlayDieAnim();
    }
}

[Serializable]
public class AttributeModifierEffect : SkillEffect
{
    public AttributeType targetAttribute;
    [Tooltip("各等级下增加或减少的数值 (负数为Debuff，请填入3个值)")]
    public int[] modifierValues = new int[3];

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, modifierValues.Length - 1);
        int finalValue = modifierValues.Length > 0 ? modifierValues[idx] : 0;

        Debug.Log($"[SkillEffect] 触发属性修改: {targetAttribute} {(finalValue > 0 ? "+" : "")}{finalValue}");
        // TODO: 具体的属性加减逻辑
    }
}

[Serializable]
public class HitBarInterferenceEffect : SkillEffect
{
    [Tooltip("各等级下缩小对方命中区间的宽度 (请填入3个值)")]
    public float[] reduceWidthAmounts = new float[3];

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, reduceWidthAmounts.Length - 1);
        float finalWidth = reduceWidthAmounts.Length > 0 ? reduceWidthAmounts[idx] : 0;

        Debug.Log($"[SkillEffect] 触发打击条干扰: 对方判定区间减少 {finalWidth}");
        // TODO: 具体的干扰逻辑
    }
}

[Serializable]
public class ApplyStatusEffect : SkillEffect
{
    [Tooltip("要施加的状态类型")]
    public StatusType statusType;
    [Tooltip("各等级下的基础持续回合数 (请填入3个值)")]
    public int[] baseDurations = new int[3];
    [Tooltip("是否作用于自己？(勾选加给自己，否则加给目标)")]
    public bool applyToSelf;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, baseDurations.Length - 1);
        int currentBaseDuration = baseDurations.Length > 0 ? baseDurations[idx] : 0;

        int extraDuration = Mathf.FloorToInt(caster.roleData.mentality / 6f);
        int finalDuration = Mathf.Max(1, currentBaseDuration + extraDuration);

        BattleEntity actualTarget = applyToSelf ? caster : target;
        actualTarget.AddStatus(statusType, finalDuration);

        string statusName = statusType == StatusType.Tension ? "紧张" : "集中";

        // 【核心修复】：呼叫状态UI刷新，并修正飘字位置
        actualTarget.OnStatusChanged?.Invoke();
        manager.SpawnDamagePopup(actualTarget.isPlayer, $"[{statusName}]", 1);

        Debug.Log($"[SkillEffect] {caster.roleData.roleName} 施放状态：{actualTarget.roleData.roleName} 获得 [{statusName}] ({finalDuration}回合)");
    }
}

// ==========================================
// 全局战斗规则 (Global Rules)
// ==========================================
public static class GlobalBattleRules
{
    /// <summary>
    /// 获取各级命中区间的统一伤害倍率
    /// </summary>
    public static float GetHitMultiplier(SectionLevel level)
    {
        switch (level)
        {
            case SectionLevel.Level1: return 1.0f;
            case SectionLevel.Level2: return 1.3f;
            case SectionLevel.Level3: return 1.8f;
            case SectionLevel.Level4: return 2.5f;
            case SectionLevel.Level5: return 3.5f;
            case SectionLevel.Level6: return 5.0f;
            default: return 1.0f;
        }
    }
    public static Color GetSectionColor(SectionLevel level)
    {
        switch (level)
        {
            case SectionLevel.Level1: return new Color(1f, 0.8f, 0.2f); // 黄
            case SectionLevel.Level2: return new Color(0.4f, 1f, 0.4f); // 绿
            case SectionLevel.Level3: return new Color(0.2f, 0.8f, 1f); // 蓝
            case SectionLevel.Level4: return new Color(1f, 0.4f, 0.4f); // 红
            case SectionLevel.Level5: return new Color(0.8f, 0.2f, 1f); // 紫
            case SectionLevel.Level6: return Color.white;               // 白
            default: return new Color(0.3f, 0.3f, 0.3f);                // 灰
        }
    }
}