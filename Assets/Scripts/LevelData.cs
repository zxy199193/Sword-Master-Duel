using UnityEngine;

/// <summary>
/// 大关配置：定义该关卡3个战斗节点各自的敌人强度等级。
/// 具体敌人在运行时由 GameManager 从 EnemyDifficultyDatabase 中按等级随机抽取。
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "SwordMaster/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("关卡基础信息")]
    public string levelTitle;

    [Header("战斗节点强度配置")]
    [Tooltip("3个战斗节点分别对应的敌人强度等级 (1~5)。" +
             "例如填 [2, 3, 4] 则第1战从2级池随机，第2战从3级池随机，第3战从4级池随机。")]
    [Range(1, 5)] public int node1Difficulty = 1;
    [Range(1, 5)] public int node2Difficulty = 2;
    [Range(1, 5)] public int node3Difficulty = 3;

    /// <summary>
    /// 按节点索引（0~2）获取对应的强度等级
    /// </summary>
    public int GetNodeDifficulty(int nodeIndex)
    {
        switch (nodeIndex)
        {
            case 0: return node1Difficulty;
            case 1: return node2Difficulty;
            case 2: return node3Difficulty;
            default:
                Debug.LogWarning($"[LevelData] 节点索引越界: {nodeIndex}，返回默认强度1");
                return 1;
        }
    }
}