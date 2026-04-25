using UnityEngine;

/// <summary>
/// 准备状态：回合开始，唤醒操作面板并等待玩家输入指令
/// </summary>
public class PreparationState : BattleState
{
    public PreparationState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("[PreparationState] 回合开始：等待玩家输入指令...");

        if (battleManager.playerEntity.lockedNextTurnSkill != null)
        {
            Debug.Log("[PreparationState] 玩家处于蓄力/锁定状态，自动执行锁定的动作");
            SkillSlot lockedSkill = battleManager.playerEntity.lockedNextTurnSkill;
            // 清除锁定
            battleManager.playerEntity.lockedNextTurnSkill = null;
            
            // 延迟一帧执行，避免状态机切换过快
            battleManager.StartCoroutine(DelayAutoConfirm(lockedSkill));
        }
        else if (battleManager.actionPanelUI != null)
        {
            battleManager.actionPanelUI.ShowPanel();
            battleManager.ShowBroadcast("决策阶段");
        }
        else
        {
            Debug.LogError("[PreparationState] 致命错误：BattleManager 未绑定 ActionPanelUI 引用！");
        }
    }

    public override void Execute()
    {
        // 待机状态，完全由 UI 事件驱动流转，无需帧更新逻辑
    }

    private System.Collections.IEnumerator DelayAutoConfirm(SkillSlot lockedSkill)
    {
        yield return null;
        battleManager.OnPlayerActionConfirmed(lockedSkill, null, true);
    }

    public override void Exit()
    {
        // UI 面板在操作确认后已自行隐藏
    }
}