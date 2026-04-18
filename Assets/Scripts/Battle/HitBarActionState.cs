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
        // 【已删除】：速度计算的代码
        float statusWidthModifier = attacker.GetHitBarWidthModifier();

        HitBarConfig leveledConfig = attackSkill.GetLeveledHitBarConfig(level);

        HitBarConfig finalConfig = new HitBarConfig
        {
            // 【已删除】：baseSpeed 和 baseSlowdown 的赋值，因为已经被全局接管了
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
                width = Mathf.Max(1f, original.width + enemyModifier + statusWidthModifier)
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