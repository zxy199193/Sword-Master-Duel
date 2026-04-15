using UnityEngine;
using System.Collections.Generic;

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

        SkillData chosenSkill = battleManager.currentEnemySkill;

        if (chosenSkill == null)
        {
            Debug.LogWarning("[EnemyActionState] 敌人当前无有效攻击技能，直接进入结算。");
            battleManager.ChangeState(new DamageSettleState(battleManager));
            return;
        }

        float widthModifier = battleManager.playerEntity.tempHitWidthModifier;
        HitBarConfig leveledConfig = chosenSkill.GetLeveledHitBarConfig();

        HitBarConfig finalConfig = new HitBarConfig
        {
            baseSpeed = leveledConfig.baseSpeed,
            baseSlowdown = leveledConfig.baseSlowdown,
            actionTime = leveledConfig.actionTime,
            sections = new HitSection[leveledConfig.sections.Length]
        };

        for (int i = 0; i < finalConfig.sections.Length; i++)
        {
            HitSection original = leveledConfig.sections[i];
            finalConfig.sections[i] = new HitSection
            {
                level = original.level,
                axisPosition = original.axisPosition,
                width = Mathf.Max(1f, original.width + widthModifier)
            };
        }

        Vector2 deviation = battleManager.enemyEntity.roleData.hitBarDeviation;

        // ==========================================
        // 【核心修改】：传入 enemyEntity 和 chosenSkill，且标记 isAI 为 true
        // ==========================================
        battleManager.hitBarManager.StartHitBar(
            finalConfig,
            chosenSkill.hitTimes,
            OnHitComplete,
            battleManager.enemyEntity,
            chosenSkill,
            true,
            deviation
        );
    }

    private void OnHitComplete(List<HitSection?> results, bool isTimeout)
    {
        battleManager.currentHitResults = results;
        battleManager.currentAttackTimeout = isTimeout;

        battleManager.ChangeState(new DamageSettleState(battleManager));
    }
}