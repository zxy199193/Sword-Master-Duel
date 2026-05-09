using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 额外奖励池中的单个词条：可以是一件装备，或若干数量的道具。
/// </summary>
[System.Serializable]
public class LevelExtraRewardEntry
{
    public enum RewardType { Equipment, Item }

    [Tooltip("奖励类型：装备 or 道具")]
    public RewardType rewardType;

    [Tooltip("装备奖励（rewardType = Equipment 时有效）")]
    public EquipmentData equipment;

    [Tooltip("道具奖励（rewardType = Item 时有效）")]
    public SkillData item;

    [Tooltip("数量（装备固定填1，道具可多件）")]
    [Range(1, 99)] public int quantity = 1;

    // ──────────────────────────────────────────────
    // 辅助：获取显示用图标 & 名称
    // ──────────────────────────────────────────────

    public Sprite GetIcon()
    {
        if (rewardType == RewardType.Equipment && equipment != null) return equipment.icon;
        if (rewardType == RewardType.Item && item != null) return item.skillIcon;
        return null;
    }

    public string GetDisplayName()
    {
        if (rewardType == RewardType.Equipment && equipment != null) return equipment.equipName;
        if (rewardType == RewardType.Item && item != null) return item.skillName;
        return "未知奖励";
    }
}

/// <summary>
/// 大关配置：玩家可从 A、B 两组中选一组进行3场战斗。
/// 每组敌人强度独立配置，均可获得基础奖励（金币+经验），
/// 且AB各从额外奖励池中随机抽一项不同的奖励。
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "SwordMaster/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("关卡基础信息")]
    public string levelTitle;

    // ──────────────────────────────────────────────
    // A 组：3个战斗节点的强度
    // ──────────────────────────────────────────────
    [Header("A 组战斗节点强度 (1~5)")]
    [Range(1, 5)] public int groupA_Node1Difficulty = 1;
    [Range(1, 5)] public int groupA_Node2Difficulty = 2;
    [Range(1, 5)] public int groupA_Node3Difficulty = 3;

    // ──────────────────────────────────────────────
    // B 组：3个战斗节点的强度
    // ──────────────────────────────────────────────
    [Header("B 组战斗节点强度 (1~5)")]
    [Range(1, 5)] public int groupB_Node1Difficulty = 2;
    [Range(1, 5)] public int groupB_Node2Difficulty = 3;
    [Range(1, 5)] public int groupB_Node3Difficulty = 4;

    // ──────────────────────────────────────────────
    // 奖励配置
    // ──────────────────────────────────────────────
    [Header("基础奖励（通关 A 或 B 均可获得）")]
    public int baseGoldReward = 50;
    public int baseExpReward  = 30;

    [Header("额外奖励池（A/B 各随机抽1项，保证不重复；池只有1项时两组相同）")]
    public List<LevelExtraRewardEntry> extraRewardPool = new List<LevelExtraRewardEntry>();

    // ──────────────────────────────────────────────
    // 辅助方法
    // ──────────────────────────────────────────────

    public int GetGroupANodeDifficulty(int nodeIndex)
    {
        switch (nodeIndex)
        {
            case 0: return groupA_Node1Difficulty;
            case 1: return groupA_Node2Difficulty;
            case 2: return groupA_Node3Difficulty;
            default:
                Debug.LogWarning($"[LevelData] A组节点索引越界: {nodeIndex}，返回默认强度1");
                return 1;
        }
    }

    public int GetGroupBNodeDifficulty(int nodeIndex)
    {
        switch (nodeIndex)
        {
            case 0: return groupB_Node1Difficulty;
            case 1: return groupB_Node2Difficulty;
            case 2: return groupB_Node3Difficulty;
            default:
                Debug.LogWarning($"[LevelData] B组节点索引越界: {nodeIndex}，返回默认强度1");
                return 1;
        }
    }
}