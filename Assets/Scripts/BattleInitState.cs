using UnityEngine;

public class BattleInitState : BattleState
{
    public BattleInitState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("[BattleInitState] 战斗初始化开始...");

        // 核心安全校验
        if (battleManager.playerEntity == null || battleManager.enemyEntity == null)
        {
            Debug.LogError("[BattleInitState] 致命错误：BattleManager 上的实体引用丢失！");
            return;
        }

        // ==========================================
        // 1. 实体数据初始化
        // ==========================================
        battleManager.playerEntity.Initialize(battleManager.playerEntity.roleData, true);
        battleManager.enemyEntity.Initialize(battleManager.enemyEntity.roleData, false);

        // ==========================================
        // 2. UI 绑定
        // ==========================================
        if (battleManager.playerInfoUI != null)
        {
            battleManager.playerInfoUI.BindEntity(battleManager.playerEntity);
        }

        if (battleManager.enemyInfoUI != null)
        {
            battleManager.enemyInfoUI.BindEntity(battleManager.enemyEntity);
        }

        Debug.Log("[BattleInitState] 参战实体与 UI 初始化完毕。");

        // ==========================================
        // 3. 状态流转 (必须放在所有初始化逻辑的最后！)
        // ==========================================
        battleManager.ChangeState(new PreparationState(battleManager));
    }
}