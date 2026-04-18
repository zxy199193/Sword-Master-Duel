using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "SwordMaster/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("关卡基础信息")]
    public string levelTitle = "1-1";  // 用于显示在 UI 上的标题
    public int mainLevelIndex = 1;     // 第几大关 (1~8)

    [Header("敌人配置 (请严格配置 3 个敌人)")]
    [Tooltip("索引 0 和 1 为普通敌人，索引 2 必须配置关底 Boss (记得在 RoleData 里勾选 isBoss)")]
    public List<RoleData> enemies = new List<RoleData>(3);
}