using UnityEngine;
using System;

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
        manager.SpawnDamagePopup(caster.transform.position, $"+{finalHeal} HP", 1);
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
        manager.SpawnDamagePopup(target.transform.position, finalDamage.ToString(), 2);

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
        // 动态获取当前等级的持续回合数
        int idx = Mathf.Clamp(skillLevel - 1, 0, baseDurations.Length - 1);
        int currentBaseDuration = baseDurations.Length > 0 ? baseDurations[idx] : 0;

        // 持续时间算法：基础持续时间 + 向下取整(释放者精神力/6)，保底1回合
        int extraDuration = Mathf.FloorToInt(caster.roleData.mentality / 6f);
        int finalDuration = Mathf.Max(1, currentBaseDuration + extraDuration);

        BattleEntity actualTarget = applyToSelf ? caster : target;
        actualTarget.AddStatus(statusType, finalDuration);

        string statusName = statusType == StatusType.Tension ? "紧张" : "集中";
        manager.SpawnDamagePopup(actualTarget.transform.position, $"[{statusName}]", 1);
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
}