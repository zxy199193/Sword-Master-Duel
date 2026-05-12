using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家攻击状态：融合双端数值，配置并呼出打击条，等待玩家目押操作
/// </summary>
public class HitBarActionState : BattleState
{
    public HitBarActionState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("<color=cyan>[HitBarActionState] 玩家回合：发动攻击，进入打击条判定！</color>");
        battleManager.isPlayerAttacking = true;

        SkillSlot attackSlot = battleManager.currentPlayerSkill;
        SkillData attackSkill = attackSlot.skillData;
        int level = attackSlot.level;

        BattleEntity attacker = battleManager.playerEntity;

        float enemyModifier = battleManager.enemyEntity.tempHitWidthModifier;
        float statusWidthModifier = attacker.GetHitBarWidthModifier();

        HitBarConfig leveledConfig = attackSkill.GetLeveledHitBarConfig(level);

        List<HitSection> extraSections = new List<HitSection>();
        if (attackSkill.skillType == SkillType.Attack && attacker.isPlayer)
        {
            PlayerProfile profile = GameManager.Instance.playerProfile;
            List<EquipmentData> equips = new List<EquipmentData>();
            if (profile.equippedWeapon != null) equips.Add(profile.equippedWeapon);
            if (profile.equippedArmor != null) equips.Add(profile.equippedArmor);
            if (profile.equippedAccessories != null) equips.AddRange(profile.equippedAccessories);

            foreach (var eq in equips)
            {
                if (eq == null || eq.equipEffects == null) continue;
                foreach (var effect in eq.equipEffects)
                {
                    if (effect is GlobalBattleRules.ExtraHitSectionEquipEffect extraHitEff)
                    {
                        extraSections.Add(new HitSection
                        {
                            level = extraHitEff.sectionLevel,
                            axisPosition = extraHitEff.axisPosition,
                            width = extraHitEff.width
                        });
                    }
                }
            }
        }

        HitBarConfig finalConfig = new HitBarConfig
        {
            actionTime = leveledConfig.actionTime,
            sections = new HitSection[leveledConfig.sections.Length + extraSections.Count]
        };

        for (int i = 0; i < leveledConfig.sections.Length; i++)
        {
            HitSection original = leveledConfig.sections[i];
            finalConfig.sections[i] = new HitSection
            {
                level = original.level,
                axisPosition = original.axisPosition,
                width = Mathf.Max(1f, original.width + enemyModifier + statusWidthModifier)
            };
        }

        for (int i = 0; i < extraSections.Count; i++)
        {
            HitSection extra = extraSections[i];
            finalConfig.sections[leveledConfig.sections.Length + i] = new HitSection
            {
                level = extra.level,
                axisPosition = extra.axisPosition,
                width = Mathf.Max(1f, extra.width + enemyModifier + statusWidthModifier)
            };
        }

        battleManager.hitBarManager.StartHitBar(finalConfig, attackSkill.hitTimes, OnHitComplete, attacker, attackSlot);
    }

    private void OnHitComplete(List<HitSection?> results, bool isTimeout)
    {
        battleManager.currentHitResults = results;
        battleManager.currentAttackTimeout = isTimeout;
        battleManager.ChangeState(new DamageSettleState(battleManager));
    }
}