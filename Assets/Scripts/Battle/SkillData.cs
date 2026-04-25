using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Linq;
#endif

[CreateAssetMenu(fileName = "NewSkill", menuName = "SwordMaster/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    [TextArea] public string description;
    public Sprite skillIcon;
    public SkillType skillType;
    public string animationTriggerName = "Attack";

    [Header("Combat Stats (代表1级、2级、3级)")]
    public int[] staminaCosts = new int[3];
    public float[] basicDamages = new float[3];
    public float[] basicDefends = new float[3];
    public float[] hitAmends = new float[3];

    [Header("Item Properties")]
    public bool isConsumable;
    public int price = 100;

    [Header("Hit Bar Settings")]
    public int hitTimes = 1;
    public HitBarConfig hitBarConfig;

    [Header("Skill Effects")]
    public GameObject castEffectPrefab;
    [SerializeReference] public List<SkillEffect> effects = new List<SkillEffect>();

    // ==========================================
    // Public Methods
    // ==========================================

    public int GetStaminaCost(int level)
    {
        if (skillType == SkillType.Item) return staminaCosts.Length > 0 ? staminaCosts[0] : 0;
        int idx = Mathf.Clamp(level - 1, 0, staminaCosts.Length - 1);
        return staminaCosts.Length > 0 ? staminaCosts[idx] : 0;
    }

    public float GetBasicDamage(int level)
    {
        if (skillType == SkillType.Item) return 0;
        int idx = Mathf.Clamp(level - 1, 0, basicDamages.Length - 1);
        return basicDamages.Length > 0 ? basicDamages[idx] : 0;
    }

    public float GetBasicDefend(int level)
    {
        if (skillType == SkillType.Item) return 0;
        int idx = Mathf.Clamp(level - 1, 0, basicDefends.Length - 1);
        return basicDefends.Length > 0 ? basicDefends[idx] : 0;
    }

    public float GetHitAmend(int level)
    {
        if (skillType == SkillType.Item) return 0;
        int idx = Mathf.Clamp(level - 1, 0, hitAmends.Length - 1);
        return hitAmends.Length > 0 ? hitAmends[idx] : 0;
    }

    public int GetBaseDuration(int level)
    {
        if (effects == null || effects.Count == 0) return 0;
        var statusEffect = effects.OfType<ApplyStatusEffect>().FirstOrDefault();
        if (statusEffect != null && statusEffect.baseDurations != null)
        {
            int idx = Mathf.Clamp(level - 1, 0, statusEffect.baseDurations.Length - 1);
            return statusEffect.baseDurations[idx];
        }
        return 0;
    }

    public HitBarConfig GetLeveledHitBarConfig(int level)
    {
        HitBarConfig leveledConfig = hitBarConfig;
        if (skillType != SkillType.Attack || level <= 1 || hitBarConfig.sections == null)
            return leveledConfig;

        leveledConfig.sections = new HitSection[hitBarConfig.sections.Length];
        for (int i = 0; i < hitBarConfig.sections.Length; i++)
        {
            HitSection section = hitBarConfig.sections[i];
            int newLevelInt = (int)section.level + (level - 1);
            section.level = (SectionLevel)Mathf.Clamp(newLevelInt, 0, 6);
            leveledConfig.sections[i] = section;
        }
        return leveledConfig;
    }

    public List<StatusType> GetRelatedStatuses()
    {
        List<StatusType> statuses = new List<StatusType>();
        if (effects == null) return statuses;

        foreach (var effect in effects)
        {
            if (effect is ApplyStatusEffect applyStatus)
            {
                if (!statuses.Contains(applyStatus.statusType)) statuses.Add(applyStatus.statusType);
            }
            else if (effect is ApplyStatusOnHitLevelEffect hitStatus)
            {
                if (!statuses.Contains(hitStatus.statusType)) statuses.Add(hitStatus.statusType);
            }
            else if (effect is ChargeAttackEffect)
            {
                if (!statuses.Contains(StatusType.Charging)) statuses.Add(StatusType.Charging);
            }
        }
        return statuses;
    }
}

// ==========================================
// 编辑器扩展面板
// ==========================================
#if UNITY_EDITOR
[CustomEditor(typeof(SkillData))]
public class SkillDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SkillData skillData = (SkillData)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("快速添加技能特效", EditorStyles.boldLabel);

        var effectTypes = typeof(SkillEffect).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(SkillEffect)) && !t.IsAbstract);

        foreach (var type in effectTypes)
        {
            if (GUILayout.Button($"添加 {type.Name}"))
            {
                skillData.effects.Add((SkillEffect)Activator.CreateInstance(type));
                EditorUtility.SetDirty(skillData);
            }
        }
    }
}
#endif