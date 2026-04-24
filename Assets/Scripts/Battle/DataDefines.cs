using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// ==========================================
// 核心枚举 (Enums)
// ==========================================
public enum SkillType { Attack, Defend, Dodge, Special, Item }
public enum SectionLevel { Level0, Level1, Level2, Level3, Level4, Level5, Level6, Level99 }
public enum StatusType { Tension, Focus, Agile, Gathering, Dizzy, Impatient, Excited, Tenacious, Overdrawn, Obscured, Spikes, Smoked, Burn, Clone, FireEnchant, Recover, Paralyzed, Frozen, Insight, Sharpened, Lightweight }

public enum ShopCategory { Weapon, Armor, Accessory, Item }
public enum AttributeType { Vitality, Endurance, Strength, Mentality }

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
public class NPCActionConfig
{
    public SkillSlot skillSlot;

    [Tooltip("4个阶段的权重：100~75%, 75~50%, 50~25%, 25~0%。填 -1 代表继承上一阶段的权重")]
    public int[] phaseWeights = new int[4] { 10, -1, -1, -1 };
}

// ==========================================
// 多态技能特效 (Skill Effects) - 已升级为动态等级数组版
// ==========================================

[Serializable]
public abstract class SkillEffect
{
    // 原本的执行方法（结算后触发，用于回血、上Buff、飘字等）
    public abstract void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel);

    // 允许特效在伤害乘算前，提供“基础伤害修正值”
    public virtual int GetBaseDamageModifier(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        return 0;
    }

    // ==========================================
    // 防御特效在判定伤害时，根据对方的打击条等级提供额外减伤
    // ==========================================
    public virtual int GetDefenseModifier(BattleEntity defender, BattleEntity attacker, BattleManager manager, int skillLevel, SectionLevel? hitLevel)
    {
        return 0;
    }
    public virtual bool IsHarmfulToTarget() { return false; }
    public virtual void OnEvadeSuccess(BattleEntity defender, BattleEntity attacker, BattleManager manager, int skillLevel)
    {
    }
    public virtual void OnPreDamageSettle(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel, float hitMultiplier, float weaponMultiplier)
    {
    }
    public virtual void OnAttackHit(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel, SectionLevel hitLevel)
    {
    }
    public virtual void OnBeingHit(BattleEntity defender, BattleEntity attacker, BattleManager manager, int skillLevel, SectionLevel hitLevel)
    {
    }
}

[Serializable]
public class HealEffect : SkillEffect
{
    [Tooltip("各等级下回复的生命值 (请填入3个值)")]
    public int[] healAmounts = new int[3];

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, healAmounts.Length - 1);
        int finalHeal = healAmounts.Length > 0 ? healAmounts[idx] : 0;
        caster.currentBasicLife = Mathf.Min(caster.GetFinalMaxLife(), caster.currentBasicLife + finalHeal);
        caster.OnHpChanged?.Invoke();
        manager.SpawnRecoverPopup(caster.isPlayer, $"<color=lime>+{finalHeal}</color>", true);
    }
}

[Serializable]
public class ArmorRepairEffect : SkillEffect
{
    public int amount;
    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        if (target.currentExtraLife > 0)
        {
            int maxDur = 0;
            if (target.isPlayer && GameManager.Instance != null && GameManager.Instance.playerProfile.equippedArmor != null)
                maxDur = GameManager.Instance.playerProfile.equippedArmor.durability;
            else if (target.roleData.equippedArmor != null)
                maxDur = target.roleData.equippedArmor.durability;

            if (maxDur > 0)
            {
                target.currentExtraLife = Mathf.Min(target.currentExtraLife + amount, maxDur);
                target.OnHpChanged?.Invoke();
                manager.SpawnGeneralPopup(target.isPlayer, $"护甲修复 +{amount}");
            }
        }
        else
        {
            manager.SpawnGeneralPopup(target.isPlayer, "<color=red>修复失败: 防具已损坏</color>");
        }
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
        caster.currentStamina = Mathf.Min(caster.GetFinalMaxStamina(), caster.currentStamina + finalStamina);
        caster.OnStaminaChanged?.Invoke();
        manager.SpawnRecoverPopup(caster.isPlayer, $"<color=#00FFFF>+{finalStamina} SP</color>", false);
    }
}

[Serializable]
public class RemoveStatusEffect : SkillEffect
{
    [Tooltip("是否移除所有负面状态？")]
    public bool removeAllNegative;
    [Tooltip("如果是移除特定状态，请在这里列出")]
    public List<StatusType> specificTypes = new List<StatusType>();

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        List<StatusType> toRemove = new List<StatusType>();
        if (removeAllNegative)
        {
            foreach (var key in target.activeStatuses.Keys.ToList())
            {
                if (IsNegative(key)) toRemove.Add(key);
            }
        }
        else
        {
            foreach (var type in specificTypes)
            {
                if (target.activeStatuses.ContainsKey(type)) toRemove.Add(type);
            }
        }

        foreach (var type in toRemove) target.activeStatuses.Remove(type);
        
        if (toRemove.Count > 0)
        {
            target.OnStatusChanged?.Invoke();
            manager.SpawnGeneralPopup(target.isPlayer, "净化成功");
        }
    }

    private bool IsNegative(StatusType type)
    {
        return type == StatusType.Dizzy || type == StatusType.Impatient || type == StatusType.Overdrawn ||
               type == StatusType.Obscured || type == StatusType.Smoked || type == StatusType.Burn ||
               type == StatusType.Paralyzed || type == StatusType.Frozen;
    }
}

[Serializable]
public class DirectDamageEffect : SkillEffect
{
    [Tooltip("各等级下直接造成的伤害值 (请填入3个值)")]
    public int[] damageAmounts = new int[3];
    public override bool IsHarmfulToTarget() => true;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, damageAmounts.Length - 1);
        int finalDamage = damageAmounts.Length > 0 ? damageAmounts[idx] : 0;
        target.TakeDamage(finalDamage);
        target.PlayHitAnim();
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
    }
}

[Serializable]
public class HitBarInterferenceEffect : SkillEffect
{
    [Tooltip("各等级下缩小对方命中区间的宽度 (请填入3个值)")]
    public float[] reduceWidthAmounts = new float[3];
    public override bool IsHarmfulToTarget() => true;

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
    [Tooltip("是否忽略精神属性对持续时间的加成？(勾选后持续时间固定为配置值)")]
    public bool ignoreMentalityBonus;

    public override bool IsHarmfulToTarget() => !applyToSelf;

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, baseDurations.Length - 1);
        int currentBaseDuration = baseDurations.Length > 0 ? baseDurations[idx] : 0;

        int extraDuration = ignoreMentalityBonus ? 0 : Mathf.FloorToInt(caster.roleData.mentality / 6f);
        int finalDuration = Mathf.Max(1, currentBaseDuration + extraDuration);

        BattleEntity actualTarget = applyToSelf ? caster : target;
        actualTarget.AddStatus(statusType, finalDuration);

        string statusName = statusType.ToString();
        switch (statusType)
        {
            case StatusType.Tension: statusName = "紧张"; break;
            case StatusType.Focus: statusName = "集中"; break;
            case StatusType.Agile: statusName = "灵动"; break;
            case StatusType.Gathering: statusName = "聚气"; break;
            case StatusType.Dizzy: statusName = "眩晕"; break;
            case StatusType.Impatient: statusName = "急躁"; break;
            case StatusType.Excited: statusName = "亢奋"; break;
            case StatusType.Tenacious: statusName = "坚挺"; break;
            case StatusType.Overdrawn: statusName = "透支"; break;
            case StatusType.Obscured: statusName = "遮蔽"; break;
            case StatusType.Spikes: statusName = "钉刺"; break;
            case StatusType.Smoked: statusName = "烟幕"; break;
            case StatusType.Burn: statusName = "灼烧"; break;
            case StatusType.Clone: statusName = "分身"; break;
            case StatusType.FireEnchant: statusName = "火焰附加"; break;
            case StatusType.Recover: statusName = "恢复"; break;
            case StatusType.Paralyzed: statusName = "麻痹"; break;
            case StatusType.Frozen: statusName = "结冰"; break;
            case StatusType.Insight: statusName = "看破"; break;
            case StatusType.Sharpened: statusName = "锐化"; break;
            case StatusType.Lightweight: statusName = "轻盈"; break;
        }

        // 刷新 UI 与飘字
        actualTarget.OnStatusChanged?.Invoke();
        manager.SpawnGeneralPopup(actualTarget.isPlayer, $"[{statusName}]");

        Debug.Log($"[SkillEffect] {caster.roleData.roleName} 施放状态：{actualTarget.roleData.roleName} 获得 [{statusName}] ({finalDuration}回合)");
    }
}

[Serializable]
public class CounterExtraDamageEffect : SkillEffect
{
    [Tooltip("各等级下，如果对方也攻击，提升的基础伤害 (请填入3个值)")]
    public int[] extraDamages = new int[3];

    // 1. 参与伤害公式的前置计算
    public override int GetBaseDamageModifier(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        SkillSlot opponentSkill = caster.isPlayer ? manager.currentEnemySkill : manager.currentPlayerSkill;

        // 如果对方也用了攻击技能，返回这10点基础伤害加成
        if (opponentSkill != null && opponentSkill.skillData != null && opponentSkill.skillData.skillType == SkillType.Attack)
        {
            int idx = Mathf.Clamp(skillLevel - 1, 0, extraDamages.Length - 1);
            return extraDamages.Length > 0 ? extraDamages[idx] : 0;
        }
        return 0;
    }

    // 2. 攻击命中后的视觉表现
    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        SkillSlot opponentSkill = caster.isPlayer ? manager.currentEnemySkill : manager.currentPlayerSkill;

        if (opponentSkill != null && opponentSkill.skillData != null && opponentSkill.skillData.skillType == SkillType.Attack)
        {
            // 这里不再扣血，血量已经在上面混入普攻一起扣过了。这里只负责飘个帅气的字！
            manager.SpawnGeneralPopup(target.isPlayer, $"<color=#FF4500>居!</color>");
            Debug.Log($"[{caster.roleData.roleName}] 触发居合特效！基础伤害大幅度提升！");
        }
    }
}

[Serializable]
public class CriticalDefenseEffect : SkillEffect
{
    [Tooltip("各等级下，当对方打击条达到指定等级及以上时，额外提升的防御力 (请填入3个值)")]
    public int[] extraDefends = new int[3];

    [Tooltip("触发该效果所需的最低打击条评价 (默认 Level3)")]
    public SectionLevel minTriggerLevel = SectionLevel.Level3;

    // 在对方打中自己，算伤害的瞬间触发！
    public override int GetDefenseModifier(BattleEntity defender, BattleEntity attacker, BattleManager manager, int skillLevel, SectionLevel? hitLevel)
    {
        // 判定：如果对方按出了有效成绩，并且等级 >= 要求的最低等级（如 Level3）
        if (hitLevel.HasValue && hitLevel.Value >= minTriggerLevel)
        {
            int idx = Mathf.Clamp(skillLevel - 1, 0, extraDefends.Length - 1);
            int defBonus = extraDefends.Length > 0 ? extraDefends[idx] : 0;

            if (defBonus > 0)
            {
                // 当场飘个绿字提示玩家完美格挡了！
                manager.SpawnGeneralPopup(defender.isPlayer, $"<color=#32CD32>要害防御!</color>");
                Debug.Log($"[{defender.roleData.roleName}] 触发要害防御！对方打击评价为 {hitLevel.Value}，额外提供减伤：{defBonus}");
            }
            return defBonus;
        }
        return 0;
    }

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 留空，防御生效的逻辑和飘字已经在上面的拦截器里完成了
    }
}

[Serializable]
public class SubSkillImmunityEffect : SkillEffect
{
    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 赋予免疫标志
        caster.isImmuneToSubSkills = true;

        manager.SpawnGeneralPopup(caster.isPlayer, "<color=#00FFFF>回避准备</color>");
        Debug.Log($"[{caster.roleData.roleName}] 触发后撤步！本回合免疫对方的有害副技能！");
    }
}

[Serializable]
public class CounterStatusOnEvadeEffect : SkillEffect
{
    [Tooltip("闪避成功后，给攻击方施加的状态")]
    public StatusType statusToApply = StatusType.Tension;

    [Tooltip("各等级下的状态持续回合数 (请填入3个值)")]
    public int[] durations = new int[3];

    public override void OnEvadeSuccess(BattleEntity defender, BattleEntity attacker, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, durations.Length - 1);
        int dur = durations.Length > 0 ? durations[idx] : 0;

        if (dur > 0)
        {
            // 给劈空的倒霉蛋加上状态
            attacker.AddStatus(statusToApply, dur);

            // 飘字提示玩家无刀取成功！
            manager.SpawnGeneralPopup(attacker.isPlayer, $"<color=#FF8C00>破绽![{statusToApply}]</color>");
            Debug.Log($"[{defender.roleData.roleName}] 触发无刀取！闪避成功，导致 [{attacker.roleData.roleName}] 陷入 {statusToApply} 状态 {dur} 回合！");
        }
    }

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 留空，真正的逻辑都在上面的 OnEvadeSuccess 里
    }
}
[Serializable]
public class CounterAttackOnEvadeEffect : SkillEffect
{
    [Tooltip("闪避成功后，反击的基础伤害 (请填入3个值，如 6, 8, 11)")]
    public int[] counterDamages = new int[3];

    public override void OnEvadeSuccess(BattleEntity defender, BattleEntity attacker, BattleManager manager, int skillLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, counterDamages.Length - 1);
        int baseDmg = counterDamages.Length > 0 ? counterDamages[idx] : 0;

        if (baseDmg > 0)
        {
            // 伤害公式：技能反击基础伤害 + 自身(defender)的力量属性
            int finalDamage = baseDmg + defender.GetFinalStrength();

            // 扣血（作为惩罚性反击，这里直接造成伤害，无视对方此时的临时防御，突出一个“破绽真实伤害”）
            attacker.TakeDamage(finalDamage);

            // 让被反击的人播受击动画和飙血特效
            attacker.PlayHitAnim();
            manager.SpawnHitEffect(attacker.transform);

            // 飘字表现：防守方头上冒出“燕返！”，攻击方头上爆出红色的反击伤害数字
            manager.SpawnGeneralPopup(defender.isPlayer, "<color=#FF4500>燕返!</color>");
            manager.SpawnDamagePopup(attacker.isPlayer, finalDamage.ToString(), 2); // 传2让它用暴击样式的数字

            Debug.Log($"[{defender.roleData.roleName}] 触发燕返！成功闪避并反斩，对 [{attacker.roleData.roleName}] 造成了 {finalDamage} 点真实伤害！");

            // 万一这一下直接把对方反击死了，得让他倒下
            if (attacker.currentBasicLife <= 0)
            {
                attacker.PlayDieAnim();
            }
        }
    }

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 留空：核心逻辑都在闪避成功的回调里
    }
}

[Serializable]
public class AntiShieldDamageEffect : SkillEffect
{
    [Tooltip("各等级下，对额外生命造成的额外基础伤害 (请填入3个值，如 3, 5, 8)")]
    public int[] extraShieldDamages = new int[3];

    public override void OnPreDamageSettle(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel, float hitMultiplier, float weaponMultiplier)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, extraShieldDamages.Length - 1);
        int baseExtra = extraShieldDamages.Length > 0 ? extraShieldDamages[idx] : 0;

        // 仅当配置了额外伤害，且目标当前有额外生命时才生效
        if (baseExtra > 0 && target.currentExtraLife > 0)
        {
            // 按照你的公式计算：额外基础伤害 × 武器倍率 × 打击条倍率 (此伤害为破甲真伤，无视对方防御)
            int finalExtraDmg = Mathf.RoundToInt(baseExtra * weaponMultiplier * hitMultiplier);

            // 限制最大扣除量：不能超过对方当前的额外生命（溢出部分会被丢弃）
            int actualDeduct = Mathf.Min(target.currentExtraLife, finalExtraDmg);

            // 偷偷扣除额外生命
            target.currentExtraLife -= actualDeduct;
            target.OnHpChanged?.Invoke(); // 刷新UI上的血条/护盾条

            // 飘个灰色的字提示玩家碎甲成功！
            manager.SpawnGeneralPopup(target.isPlayer, $"<color=#A9A9A9>碎甲 {actualDeduct}</color>");
            Debug.Log($"[{caster.roleData.roleName}] 触发重劈！对额外生命造成了 {finalExtraDmg} 点伤害，实际扣除 {actualDeduct} 点！");
        }
    }

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 留空，核心破盾逻辑已在 OnPreDamageSettle 完成
    }
}
[Serializable]
public class ApplyStatusOnHitLevelEffect : SkillEffect
{
    [Tooltip("要施加的状态类型")]
    public StatusType statusType = StatusType.Dizzy;

    [Tooltip("触发该效果所需的最低打击条评价 (例如填 Level3)")]
    public SectionLevel minTriggerLevel = SectionLevel.Level3;

    [Tooltip("各等级下的状态持续回合数 (请填入3个值)")]
    public int[] durations = new int[3];

    public override void OnAttackHit(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel, SectionLevel hitLevel)
    {
        // 枚举从 Level0 到 Level6 顺序排列，直接使用 >= 判断
        if (hitLevel >= minTriggerLevel)
        {
            int idx = Mathf.Clamp(skillLevel - 1, 0, durations.Length - 1);
            int dur = durations.Length > 0 ? durations[idx] : 0;

            if (dur > 0)
            {
                // 给目标上状态
                target.AddStatus(statusType, dur);

                string statusName = statusType.ToString();
                switch (statusType)
                {
                    case StatusType.Tension: statusName = "紧张"; break;
                    case StatusType.Focus: statusName = "集中"; break;
                    case StatusType.Agile: statusName = "灵动"; break;
                    case StatusType.Gathering: statusName = "聚气"; break;
                    case StatusType.Dizzy: statusName = "眩晕"; break;
                    case StatusType.Obscured: statusName = "遮蔽"; break;
                    case StatusType.Spikes: statusName = "钉刺"; break;
                    case StatusType.Smoked: statusName = "烟幕"; break;
                }

                // 刷新UI和飘字
                target.OnStatusChanged?.Invoke();
                manager.SpawnGeneralPopup(target.isPlayer, $"<color=#FFD700>精准命中![{statusName}]</color>");

                Debug.Log($"[{caster.roleData.roleName}] 触发特效！打击评价达到 {hitLevel}，成功施加 [{statusName}] 状态 {dur} 回合！");
            }
        }
    }

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 留空，核心逻辑已经转移到 OnAttackHit 中了
    }
}

[Serializable]
public class ApplyStatusOnBeingHitEffect : SkillEffect
{
    [Tooltip("要施加的状态类型")]
    public StatusType statusType = StatusType.Focus;

    [Tooltip("各等级下的状态持续回合数 (请填入3个值)")]
    public int[] durations = new int[3];

    public override void OnBeingHit(BattleEntity defender, BattleEntity attacker, BattleManager manager, int skillLevel, SectionLevel hitLevel)
    {
        int idx = Mathf.Clamp(skillLevel - 1, 0, durations.Length - 1);
        int dur = durations.Length > 0 ? durations[idx] : 0;

        if (dur > 0)
        {
            // 给防御方（自己）上状态
            defender.AddStatus(statusType, dur);

            string statusName = statusType.ToString();
            switch (statusType)
            {
                case StatusType.Tension: statusName = "紧张"; break;
                case StatusType.Focus: statusName = "集中"; break;
                case StatusType.Agile: statusName = "灵动"; break;
                case StatusType.Gathering: statusName = "聚气"; break;
                case StatusType.Dizzy: statusName = "眩晕"; break;
                case StatusType.Obscured: statusName = "遮蔽"; break;
                case StatusType.Spikes: statusName = "钉刺"; break;
                case StatusType.Smoked: statusName = "烟幕"; break;
                case StatusType.Burn: statusName = "灼烧"; break;
                case StatusType.Clone: statusName = "分身"; break;
            }

            // 刷新UI和飘字
            defender.OnStatusChanged?.Invoke();
            manager.SpawnGeneralPopup(defender.isPlayer, $"<color=#00FFFF>受击反制![{statusName}]</color>");

            Debug.Log($"[{defender.roleData.roleName}] 触发特效！在防御中被命中，成功施加 [{statusName}] 状态 {dur} 回合！");
        }
    }

    public override void Execute(BattleEntity caster, BattleEntity target, BattleManager manager, int skillLevel)
    {
        // 留空
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
            case SectionLevel.Level3: return 1.6f;
            case SectionLevel.Level4: return 2.0f;
            case SectionLevel.Level5: return 2.5f;
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


    // ==========================================
    // 全局打击条配置 (统一管理)
    // ==========================================
    public static float globalHitBarBaseSpeed = 100f;      // 全局基础滑块速度
    public static float globalHitBarBaseSlowdown = 100f;   // 全局基础滑块减速度

    // 负重状态枚举
    public enum LoadWeightState { Light, Medium, Heavy, Extreme }

    // ==========================================
    // 装备专属机制 (Equip Effects)
    // ==========================================

    public enum EquipTriggerTiming { OnBattleStart, OnAttackHit, OnDefendHit, OnVictory }
    public enum EquipCondition { None, Equal, GreaterOrEqual, Less, Between }

    [Serializable]
    public abstract class EquipEffect
    {
        [Tooltip("该效果在什么时候触发？")]
        public EquipTriggerTiming triggerTiming;

        // 返回值代表对最终伤害的修正值 (0代表不修改)
        public abstract int Execute(BattleEntity wearer, BattleEntity opponent, SectionLevel? hitLevel, BattleManager manager);
    }

    [Serializable]
    public class ConditionalDamageEquipEffect : EquipEffect
    {
        [Header("触发条件")]
        public EquipCondition condition;
        public SectionLevel targetLevelMin;
        [Tooltip("仅当条件为 Between 时，该最大值才生效")]
        public SectionLevel targetLevelMax;

        [Header("效果")]
        [Tooltip("满足条件时的伤害修正值 (正数加伤，负数减伤)")]
        public int damageModifier;

        public override int Execute(BattleEntity wearer, BattleEntity opponent, SectionLevel? hitLevel, BattleManager manager)
        {
            if (hitLevel == null || condition == EquipCondition.None) return 0;

            int actual = (int)hitLevel.Value;
            int min = (int)targetLevelMin;
            int max = (int)targetLevelMax;

            bool isMet = false;
            switch (condition)
            {
                case EquipCondition.Equal: isMet = actual == min; break;
                case EquipCondition.GreaterOrEqual: isMet = actual >= min; break;
                case EquipCondition.Less: isMet = actual < min; break;
                case EquipCondition.Between: isMet = actual >= min && actual <= max; break;
            }

            if (isMet)
            {
                Debug.Log($"[装备特效] 触发伤害修正: {damageModifier}");
                return damageModifier;
            }
            return 0;
        }
    }

    [Serializable]
    public class ApplyStatusEquipEffect : EquipEffect
    {
        [Header("状态设定")]
        public StatusType statusType;
        public int duration;
        public bool applyToSelf = true;

        public override int Execute(BattleEntity wearer, BattleEntity opponent, SectionLevel? hitLevel, BattleManager manager)
        {
            BattleEntity target = applyToSelf ? wearer : opponent;
            target.AddStatus(statusType, duration);
            manager.SpawnGeneralPopup(target.isPlayer, $"[{statusType}]");
            return 0; // 状态附加不直接修改当下的伤害数字
        }
    }

    [Serializable]
    public class StatusImmunityEquipEffect : EquipEffect
    {
        public StatusType immuneStatus;

        public override int Execute(BattleEntity wearer, BattleEntity opponent, SectionLevel? hitLevel, BattleManager manager)
        {
            return 0; // 仅作为标记使用
        }
    }

    [Serializable]
    public class RewardModifierEquipEffect : EquipEffect
    {
        public bool isGold; // true为金币，false为经验
        public int bonusAmount;

        public override int Execute(BattleEntity wearer, BattleEntity opponent, SectionLevel? hitLevel, BattleManager manager)
        {
            return bonusAmount;
        }
    }

    [Serializable]
    public class SkillTypeModifierEquipEffect : EquipEffect
    {
        public SkillType targetSkillType;
        public int bonusValue;

        public override int Execute(BattleEntity wearer, BattleEntity opponent, SectionLevel? hitLevel, BattleManager manager)
        {
            return bonusValue;
        }
    }
}