using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyGroup
{
    [Tooltip("仅用于策划备注，比如 '精英史莱姆组'，不显示在游戏中")]
    public string groupName = "普通组";

    [Tooltip("该组内的3名敌人")]
    public List<RoleData> enemies = new List<RoleData>(3);
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "SwordMaster/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("关卡基础信息")]
    public string levelTitle;

    [Header("随机敌人组配置池")]
    [Tooltip("每次进入该关卡时，会从下面的列表中随机抽取一组作为本关的 3 个敌人")]
    public List<EnemyGroup> possibleGroups = new List<EnemyGroup>();
}