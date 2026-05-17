using System.Collections.Generic;
using UnityEngine;

public enum TaskDifficulty
{
    Easy,
    Medium,
    Hard,
    Extreme
}

[System.Serializable]
public class TaskTierConfig
{
    public TaskDifficulty difficulty;
    
    [Header("Rewards")]
    public int goldReward = 30;
    public int expReward = 20;
    public List<LevelExtraRewardEntry> rewardPool = new List<LevelExtraRewardEntry>();

    [Header("Enemy Pool")]
    public List<RoleData> enemyPool = new List<RoleData>();
}

[CreateAssetMenu(fileName = "TaskDatabase", menuName = "SwordMaster/Task Database")]
public class TaskDatabase : ScriptableObject
{
    public List<TaskTierConfig> taskTiers = new List<TaskTierConfig>();

    public TaskTierConfig GetConfig(TaskDifficulty difficulty)
    {
        return taskTiers.Find(t => t.difficulty == difficulty);
    }
}
