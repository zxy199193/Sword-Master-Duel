using UnityEngine;
using static GlobalBattleRules;

public class BattleInitState : BattleState
{
    public BattleInitState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("[BattleInitState] 战斗初始化开始...");

        if (battleManager.playerEntity == null || battleManager.enemyEntity == null) return;

        // ==========================================
        // 1. 实体数据初始化
        // ==========================================
        battleManager.playerEntity.Initialize(battleManager.playerEntity.roleData, true);
        battleManager.enemyEntity.Initialize(battleManager.enemyEntity.roleData, false);

        // 【新增修复 1】：第一场战斗开局，必须发放一次基础体力回复，防止 0 体力死锁！
        battleManager.playerEntity.RecoverStamina();
        battleManager.enemyEntity.RecoverStamina();

        // 【新增修复 2】：清理上一场战斗因为 SetActive(false) 被迫中断协程而残留的飘字！
        if (battleManager.floatingTextCanvas != null)
        {
            foreach (Transform child in battleManager.floatingTextCanvas.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        // ==========================================
        // 2. UI 绑定
        // ==========================================
        if (battleManager.playerInfoUI != null) battleManager.playerInfoUI.BindEntity(battleManager.playerEntity);
        if (battleManager.enemyInfoUI != null) battleManager.enemyInfoUI.BindEntity(battleManager.enemyEntity);

        battleManager.TriggerPlayerEquipEffects(EquipTriggerTiming.OnBattleStart, null);

        // ==========================================
        // 3. 状态流转
        // ==========================================
        battleManager.ChangeState(new PreparationState(battleManager));
    }
}