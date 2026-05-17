using UnityEngine;
using static GlobalBattleRules;

public class BattleInitState : BattleState
{
    public BattleInitState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("[BattleInitState] 战斗初始化开始...");

        if (battleManager.playerEntity == null || battleManager.enemyEntity == null) return;

        // 实体数据初始化
        battleManager.playerEntity.Initialize(battleManager.playerEntity.roleData, true);
        battleManager.enemyEntity.Initialize(battleManager.enemyEntity.roleData, false);

        // 发放基础体力回复
        battleManager.playerEntity.RecoverStamina();
        battleManager.enemyEntity.RecoverStamina();

        // 清理残留飘字
        battleManager.ClearAllPopups();

        // UI 绑定
        if (battleManager.playerInfoUI != null) battleManager.playerInfoUI.BindEntity(battleManager.playerEntity);
        if (battleManager.enemyInfoUI != null) battleManager.enemyInfoUI.BindEntity(battleManager.enemyEntity);

        battleManager.TriggerEquipEffects(battleManager.playerEntity, EquipTriggerTiming.OnBattleStart, null);
        battleManager.TriggerEquipEffects(battleManager.enemyEntity, EquipTriggerTiming.OnBattleStart, null);

        // 状态流转
        battleManager.ChangeState(new PreparationState(battleManager));
    }
}