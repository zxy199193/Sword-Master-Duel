using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家攻击状态：融合双端数值，配置并呼出打击条，等待玩家目押操作
/// </summary>
public class HitBarActionState : BattleState
{
    public HitBarActionState(BattleManager manager) : base(manager) { }

    // ==========================================
    // 状态机生命周期 (State Lifecycle)
    // ==========================================

    public override void Enter()
    {
        Debug.Log("<color=cyan>[HitBarActionState] 玩家回合：发动攻击，进入打击条判定！</color>");
        battleManager.isPlayerAttacking = true;

        SkillData attackSkill = battleManager.currentPlayerSkill;
        BattleEntity attacker = battleManager.playerEntity;

        // ==========================================
        // 核心逻辑：组装配置并融合数值修正 (Buff/Debuff)
        // ==========================================

        // 读取防守方的干扰值
        float enemyModifier = battleManager.enemyEntity.tempHitWidthModifier;

        // 读取攻击者自身的状态修饰值
        float speedMultiplier = attacker.GetHitBarSpeedMultiplier();
        float statusWidthModifier = attacker.GetHitBarWidthModifier();

        // 深拷贝打击条配置，注入实战修正数据
        HitBarConfig finalConfig = new HitBarConfig
        {
            baseSpeed = attackSkill.hitBarConfig.baseSpeed * speedMultiplier,
            baseSlowdown = attackSkill.hitBarConfig.baseSlowdown,
            actionTime = attackSkill.hitBarConfig.actionTime,
            sections = new HitSection[attackSkill.hitBarConfig.sections.Length]
        };

        for (int i = 0; i < finalConfig.sections.Length; i++)
        {
            HitSection original = attackSkill.hitBarConfig.sections[i];
            finalConfig.sections[i] = new HitSection
            {
                level = original.level,
                axisPosition = original.axisPosition,
                // 最终宽度 = 基础宽度 + 防守方干扰修正 + 自身状态修正
                width = Mathf.Max(1f, original.width + enemyModifier + statusWidthModifier)
            };
        }

        // ==========================================
        // 启动玩家打击条
        // ==========================================
        battleManager.hitBarManager.StartHitBar(finalConfig, attackSkill.hitTimes, OnHitComplete);
    }

    // ==========================================
    // 回调逻辑 (Callbacks)
    // ==========================================

    /// <summary>
    /// 玩家打击条操作结束后的回调 (正常结束或超时)
    /// </summary>
    private void OnHitComplete(List<HitSection?> results, bool isTimeout)
    {
        battleManager.currentHitResults = results;
        battleManager.currentAttackTimeout = isTimeout;

        // 结算交接
        battleManager.ChangeState(new DamageSettleState(battleManager));
    }
}