using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌方行动状态：配置并呼出由 AI 接管的打击条
/// </summary>
public class EnemyActionState : BattleState
{
    public EnemyActionState(BattleManager manager) : base(manager) { }

    // ==========================================
    // 状态机生命周期
    // ==========================================

    public override void Enter()
    {
        Debug.Log("<color=red>[EnemyActionState] 敌方回合：发动攻击！</color>");
        battleManager.isPlayerAttacking = false;

        SkillData chosenSkill = battleManager.currentEnemySkill;

        // 安全保底：如果敌人没选技能或技能为空，直接跳过攻击进入结算
        if (chosenSkill == null)
        {
            Debug.LogWarning("[EnemyActionState] 敌人当前无有效攻击技能，直接进入结算。");
            battleManager.ChangeState(new DamageSettleState(battleManager));
            return;
        }

        // ==========================================
        // 核心逻辑：组装打击条配置
        // ==========================================

        // 读取玩家身上的防守Buff对打击条宽度的修正
        float widthModifier = battleManager.playerEntity.tempHitWidthModifier;

        // 【核心修复】：获取升档后的配置
        HitBarConfig leveledConfig = chosenSkill.GetLeveledHitBarConfig();

        HitBarConfig finalConfig = new HitBarConfig
        {
            baseSpeed = leveledConfig.baseSpeed,
            baseSlowdown = leveledConfig.baseSlowdown,
            actionTime = leveledConfig.actionTime,
            sections = new HitSection[leveledConfig.sections.Length]
        };

        // 深拷贝并注入宽度修正
        for (int i = 0; i < finalConfig.sections.Length; i++)
        {
            HitSection original = leveledConfig.sections[i];
            finalConfig.sections[i] = new HitSection
            {
                level = original.level, // 此时已经是升档后的颜色等级了
                axisPosition = original.axisPosition,
                width = Mathf.Max(1f, original.width + widthModifier) // 确保宽度不小于1
            };
        }

        // ==========================================
        // 启动 AI 打击条
        // ==========================================
        Vector2 deviation = battleManager.enemyEntity.roleData.hitBarDeviation;
        battleManager.hitBarManager.StartHitBar(finalConfig, chosenSkill.hitTimes, OnHitComplete, true, deviation);
    }

    // ==========================================
    // 回调逻辑
    // ==========================================

    private void OnHitComplete(List<HitSection?> results, bool isTimeout)
    {
        battleManager.currentHitResults = results;
        battleManager.currentAttackTimeout = isTimeout;

        battleManager.ChangeState(new DamageSettleState(battleManager));
    }
}