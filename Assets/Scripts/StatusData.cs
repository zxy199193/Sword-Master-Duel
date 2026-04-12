using UnityEngine;

/// <summary>
/// 战斗状态 (Buff/Debuff) 的静态数据配置类
/// </summary>
[CreateAssetMenu(fileName = "NewStatus", menuName = "SwordMaster/Status Data")]
public class StatusData : ScriptableObject
{
    [Header("Core Settings")]
    public StatusType statusType;
    public string statusName;
    public Sprite icon;

    [Header("Details")]
    [TextArea]
    public string description;
}