using UnityEngine;
using System;

// ==========================================
// 核心枚举 (Enums)
// ==========================================
public enum SkillType { Attack, Defend, Dodge, Special, Item }
public enum SectionLevel { Level0, Level1, Level2, Level3, Level4, Level5, Level6, Level99 }
public enum StatusType { Tension, Focus }
public enum AttributeType { Strength, Mentality, Stamina } // [整理]：从下方移至顶部统一管理

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
// 多态技能特效 (Skill Effects)
// ==========================================

[Serializable]
public abstract class SkillEffect
{
    [Tooltip("效果持续回合数 (0表示瞬发/一次性效果)")]
    public int duration;

    public abstract void Execute(BattleEntity caster, BattleEntity target, BattleManager manager);
}

[Serializable]
public class HealEffect : SkillEffect
{
    [Tooltip("回复的生命值")]
    public int healAmount;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager)
    {
        caster.currentBasicLife = Mathf.Min(caster.roleData.maxBasicLife, caster.currentBasicLife + healAmount);
        manager.SpawnDamagePopup(caster.transform.position, $"+{healAmount} HP", 1);
    }
}

[Serializable]
public class DirectDamageEffect : SkillEffect
{
    [Tooltip("直接造成的伤害值")]
    public int damageAmount;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager)
    {
        target.TakeDamage(damageAmount);
        target.PlayHitAnim();
        manager.SpawnDamagePopup(target.transform.position, damageAmount.ToString(), 2);

        if (target.currentBasicLife <= 0) target.PlayDieAnim();
    }
}

[Serializable]
public class AttributeModifierEffect : SkillEffect
{
    public AttributeType targetAttribute;
    [Tooltip("增加或减少的数值 (负数为Debuff)")]
    public int modifierValue;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager)
    {
        Debug.Log($"[SkillEffect] 触发属性修改: {targetAttribute} {(modifierValue > 0 ? "+" : "")}{modifierValue}");
        // TODO: 具体的属性加减逻辑
    }
}

[Serializable]
public class HitBarInterferenceEffect : SkillEffect
{
    [Tooltip("缩小对方命中区间的宽度")]
    public float reduceWidthAmount;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager)
    {
        Debug.Log($"[SkillEffect] 触发打击条干扰: 对方判定区间减少 {reduceWidthAmount}");
        // TODO: 具体的干扰逻辑
    }
}

[Serializable]
public class ApplyStatusEffect : SkillEffect
{
    [Tooltip("要施加的状态类型")]
    public StatusType statusType;
    [Tooltip("基础持续回合数")]
    public int baseDuration;
    [Tooltip("是否作用于自己？(勾选加给自己，否则加给目标)")]
    public bool applyToSelf;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager)
    {
        // 持续时间算法：基础持续时间 + 向下取整(释放者精神力/6)，保底1回合
        int extraDuration = Mathf.FloorToInt(caster.roleData.mentality / 6f);
        int finalDuration = Mathf.Max(1, baseDuration + extraDuration);

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