using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局状态数据库，用于集中管理和检索所有的状态 (Buff/Debuff) 配置
/// </summary>
[CreateAssetMenu(fileName = "StatusDatabase", menuName = "SwordMaster/Status Database")]
public class StatusDatabase : ScriptableObject
{
    [Header("Core Data")]
    public List<StatusData> allStatuses;

    private Dictionary<StatusType, StatusData> cache;

    // ==========================================
    // Public API
    // ==========================================

    /// <summary>
    /// 根据状态类型快速检索对应的状态数据 (带懒加载缓存机制)
    /// </summary>
    public StatusData GetStatus(StatusType type)
    {
        if (cache == null)
        {
            InitializeCache();
        }

        return cache.TryGetValue(type, out StatusData statusData) ? statusData : null;
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void InitializeCache()
    {
        cache = new Dictionary<StatusType, StatusData>();

        if (allStatuses == null) return;

        foreach (var s in allStatuses)
        {
            if (s != null && !cache.ContainsKey(s.statusType))
            {
                cache.Add(s.statusType, s);
            }
        }
    }
}