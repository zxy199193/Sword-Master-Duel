using UnityEngine;

/// <summary>
/// 战斗结束结算状态
/// </summary>
public class BattleEndState : BattleState
{
    public BattleEndState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        battleManager.ShowBroadcast("战斗结束！");

        // 判断最终胜负
        if (battleManager.playerEntity.currentBasicLife <= 0 && battleManager.enemyEntity.currentBasicLife <= 0)
        {
            Debug.Log("<color=yellow>[BattleEnd] 双方同归于尽，平局！</color>");
            battleManager.ShowBroadcast("同归于尽，平局！");
        }
        else if (battleManager.playerEntity.currentBasicLife <= 0)
        {
            Debug.Log("<color=red>[BattleEnd] 我方败北！</color>");
            battleManager.ShowBroadcast("我方败北！");
        }
        else
        {
            Debug.Log("<color=green>[BattleEnd] 战斗胜利！</color>");
            battleManager.ShowBroadcast("战斗胜利！");
        }
    }

    public override void Execute()
    {
        // 留空：什么都不做。由于状态机留在了这里，就不会再触发后续循环，彻底解决疯狂刷屏的问题。
    }
}