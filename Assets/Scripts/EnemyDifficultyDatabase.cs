using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局敌人强度数据库：将所有 RoleData 按强度等级(1~5)分组，
/// 供 GameManager 在生成关卡敌人时按需随机抽取。
/// 在 Project 窗口右键 → SwordMaster → Enemy Difficulty Database 创建。
/// </summary>
[CreateAssetMenu(fileName = "EnemyDifficultyDatabase", menuName = "SwordMaster/Enemy Difficulty Database")]
public class EnemyDifficultyDatabase : ScriptableObject
{
    [System.Serializable]
    public class DifficultyTier
    {
        [Tooltip("强度等级（1=最弱，5=最强），仅用于标识，请与数组下标+1对应填写")]
        [Range(1, 5)] public int level = 1;

        [Tooltip("该强度等级下的所有敌人候选池")]
        public List<RoleData> enemies = new List<RoleData>();
    }

    [Header("强度分级敌人池 (共5级，index 0 = Lv.1，index 4 = Lv.5)")]
    [Tooltip("请保持数组长度固定为5，index 0 对应强度1，index 4 对应强度5")]
    public DifficultyTier[] tiers = new DifficultyTier[5];

    private void OnValidate()
    {
        // 强制保持5个等级的数组长度，并自动填入对应的 level 值
        if (tiers == null || tiers.Length != 5)
        {
            DifficultyTier[] newTiers = new DifficultyTier[5];
            for (int i = 0; i < 5; i++)
            {
                if (tiers != null && i < tiers.Length && tiers[i] != null)
                    newTiers[i] = tiers[i];
                else
                    newTiers[i] = new DifficultyTier();

                newTiers[i].level = i + 1;
            }
            tiers = newTiers;
        }
        else
        {
            // 自动修正 level 标识值与索引对齐
            for (int i = 0; i < tiers.Length; i++)
            {
                if (tiers[i] == null) tiers[i] = new DifficultyTier();
                tiers[i].level = i + 1;
            }
        }
    }

    /// <summary>
    /// 从指定强度等级(1~5)的候选池中随机返回一个敌人。
    /// 若该等级池为空，返回 null 并输出警告。
    /// </summary>
    public RoleData GetRandomEnemy(int difficultyLevel)
    {
        int idx = Mathf.Clamp(difficultyLevel - 1, 0, tiers.Length - 1);
        var tier = tiers[idx];

        if (tier == null || tier.enemies == null || tier.enemies.Count == 0)
        {
            Debug.LogWarning($"[EnemyDifficultyDatabase] 强度 {difficultyLevel} 的敌人池为空，无法生成敌人！");
            return null;
        }

        return tier.enemies[Random.Range(0, tier.enemies.Count)];
    }
}
