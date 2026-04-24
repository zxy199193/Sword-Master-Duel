using UnityEngine;

/// <summary>
/// 战斗结束结算状态
/// </summary>
public class BattleEndState : BattleState
{
    public BattleEndState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        // 清理残留飘字
        battleManager.ClearAllPopups();


        bool isPlayerDead = battleManager.playerEntity.currentBasicLife <= 0;
        bool isEnemyDead = battleManager.enemyEntity.currentBasicLife <= 0;

        if (isPlayerDead && isEnemyDead)
        {
            Debug.Log("<color=yellow>[BattleEnd] 双方同归于尽，算作败北！</color>");
            battleManager.ShowBroadcast("平手");
            GameManager.Instance.OnBattleResolution(false); 
        }
        else if (isPlayerDead)
        {
            Debug.Log("<color=red>[BattleEnd] 我方败北！</color>");
            battleManager.ShowBroadcast("失败");
            GameManager.Instance.OnBattleResolution(false);
        }
        else
        {
            Debug.Log("<color=green>[BattleEnd] 战斗胜利！</color>");
            battleManager.ShowBroadcast("胜利");
            int gold = battleManager.enemyEntity.roleData.goldReward;
            int exp  = battleManager.enemyEntity.roleData.expReward;
            GameManager.Instance.OnBattleResolution(true, gold, exp);
        }
    }

    public override void Execute()
    {
        // 留空：状态机停在此处，由 GameManager 接管后续的 UI 切出与场景清理
    }
}