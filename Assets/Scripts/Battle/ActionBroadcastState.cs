using UnityEngine;

/// <summary>
/// 动作广播与非攻击行为演出状态
/// </summary>
public class ActionBroadcastState : BattleState
{
    public ActionBroadcastState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("<color=cyan>[ActionBroadcastState] 开始播放双方战斗演出队列...</color>");
        battleManager.StartCoroutine(battleManager.RoutineActionBroadcast());
    }
}