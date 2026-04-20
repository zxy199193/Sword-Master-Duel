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

        bool isPlayerDead = battleManager.playerEntity.currentBasicLife <= 0;
        bool isEnemyDead = battleManager.enemyEntity.currentBasicLife <= 0;

        // 判断最终胜负
        if (isPlayerDead && isEnemyDead)
        {
            Debug.Log("<color=yellow>[BattleEnd] 双方同归于尽，算作败北！</color>");
            battleManager.ShowBroadcast("同归于尽...");
            GameManager.Instance.OnBattleResolution(false); // 失败默认没有奖励
        }
        else if (isPlayerDead)
        {
            Debug.Log("<color=red>[BattleEnd] 我方败北！</color>");
            battleManager.ShowBroadcast("我方败北！");
            GameManager.Instance.OnBattleResolution(false);
        }
        else
        {
            Debug.Log("<color=green>[BattleEnd] 战斗胜利！</color>");
            battleManager.ShowBroadcast("战斗胜利！");

            // ==========================================
            // 【核心修改】：同时获取金币和经验奖励
            // ==========================================
            int gold = battleManager.enemyEntity.roleData.goldReward;

            // 读取我们在 RoleData 里新加的经验奖励字段
            int exp = battleManager.enemyEntity.roleData.expReward;

            // 把金币和经验一起上报给 GameManager 进行结算和升级判定
            GameManager.Instance.OnBattleResolution(true, gold, exp);
        }
    }

    public override void Execute()
    {
        // 留空：状态机停在此处，由 GameManager 接管后续的 UI 切出与场景清理
    }
}