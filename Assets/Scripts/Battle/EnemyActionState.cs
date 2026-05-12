using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌方行动状态：配置并呼出由 AI 接管的打击条
/// </summary>
public class EnemyActionState : BattleState
{
    public EnemyActionState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("<color=red>[EnemyActionState] 敌方回合：发动攻击！</color>");
        battleManager.isPlayerAttacking = false;

        SkillSlot chosenSlot = battleManager.currentEnemySkill;
        if (chosenSlot == null || chosenSlot.skillData == null)
        {
            battleManager.ChangeState(new DamageSettleState(battleManager));
            return;
        }

        SkillData chosenSkill = chosenSlot.skillData;
        int level = chosenSlot.level;

        float widthModifier = battleManager.playerEntity.tempHitWidthModifier;
        HitBarConfig leveledConfig = chosenSkill.GetLeveledHitBarConfig(level);

        HitBarConfig finalConfig = new HitBarConfig
        {
            actionTime = leveledConfig.actionTime,
            sections = new HitSection[leveledConfig.sections.Length]
        };

        var widthModifiers = battleManager.enemyEntity.GetAllEquipEffects<GlobalBattleRules.ModifyHitSectionWidthEquipEffect>();

        for (int i = 0; i < finalConfig.sections.Length; i++)
        {
            HitSection original = leveledConfig.sections[i];
            float equipWidthBonus = 0f;
            foreach(var mod in widthModifiers) {
                if (mod.targetLevel == original.level) equipWidthBonus += mod.extraWidth;
            }

            finalConfig.sections[i] = new HitSection
            {
                level = original.level,
                axisPosition = original.axisPosition,
                width = Mathf.Max(1f, original.width + widthModifier + equipWidthBonus)
            };
        }
        
        battleManager.hitBarManager.StartHitBar(finalConfig, chosenSkill.hitTimes, OnHitComplete, battleManager.enemyEntity, chosenSlot, true);
    }

    private void OnHitComplete(List<HitSection?> results, bool isTimeout)
    {
        battleManager.currentHitResults = results;
        battleManager.currentAttackTimeout = isTimeout;
        battleManager.ChangeState(new DamageSettleState(battleManager));
    }
}