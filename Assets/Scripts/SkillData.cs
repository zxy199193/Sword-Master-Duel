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
    [TextArea]
    public string description;
    public Sprite skillIcon;
    public SkillType skillType;

    [HideInInspector]
    public int skillLevel = 1;

    public string animationTriggerName = "Attack";

    [Header("Combat Stats (请填入3个值，分别代表1级、2级、3级)")]
    [Tooltip("各等级下的体力消耗")]
    public int[] staminaCosts = new int[3];

    [Tooltip("各等级下的基础伤害 (仅攻击有效)")]
    public float[] basicDamages = new float[3];

    [Tooltip("各等级下的基础减伤 (仅防御有效)")]
    public float[] basicDefends = new float[3];

    [Tooltip("各等级下的命中修正 (防守/闪避有效)")]
    public float[] hitAmends = new float[3];

    [Header("Item Properties")]
    public int quantity;
    public bool isConsumable;

    [Header("Hit Bar Settings")]
    public int hitTimes = 1;
    public HitBarConfig hitBarConfig;

    [Header("Skill Effects")]
    [Tooltip("释放该技能/道具时在自身播放的特效")]
    public GameObject castEffectPrefab;

    [SerializeReference]
    public List<SkillEffect> effects = new List<SkillEffect>();

    // ==========================================
    // 动态属性封装
    // ==========================================

    public int staminaCost
    {
        get
        {
            if (skillType == SkillType.Item) return staminaCosts.Length > 0 ? staminaCosts[0] : 0;
            int idx = Mathf.Clamp(skillLevel - 1, 0, staminaCosts.Length - 1);
            return staminaCosts.Length > 0 ? staminaCosts[idx] : 0;
        }
    }

    public float basicDamage
    {
        get
        {
            if (skillType == SkillType.Item) return 0;
            int idx = Mathf.Clamp(skillLevel - 1, 0, basicDamages.Length - 1);
            return basicDamages.Length > 0 ? basicDamages[idx] : 0;
        }
    }

    public float basicDefend
    {
        get
        {
            if (skillType == SkillType.Item) return 0;
            int idx = Mathf.Clamp(skillLevel - 1, 0, basicDefends.Length - 1);
            return basicDefends.Length > 0 ? basicDefends[idx] : 0;
        }
    }

    public float hitAmend
    {
        get
        {
            if (skillType == SkillType.Item) return 0;
            int idx = Mathf.Clamp(skillLevel - 1, 0, hitAmends.Length - 1);
            return hitAmends.Length > 0 ? hitAmends[idx] : 0;
        }
    }

    public int GetBaseDuration()
    {
        if (effects == null || effects.Count == 0) return 0;

        // 使用 LINQ 查找第一个施加状态的效果
        var statusEffect = effects.OfType<ApplyStatusEffect>().FirstOrDefault();

        if (statusEffect != null && statusEffect.baseDurations != null)
        {
            int idx = Mathf.Clamp(skillLevel - 1, 0, statusEffect.baseDurations.Length - 1);
            return statusEffect.baseDurations[idx];
        }

        return 0;
    }

    // ==========================================
    // 核心逻辑：获取进化后的打击条配置
    // ==========================================
    public HitBarConfig GetLeveledHitBarConfig()
    {
        HitBarConfig leveledConfig = hitBarConfig;

        if (skillType != SkillType.Attack || skillLevel <= 1 || hitBarConfig.sections == null)
            return leveledConfig;

        leveledConfig.sections = new HitSection[hitBarConfig.sections.Length];

        for (int i = 0; i < hitBarConfig.sections.Length; i++)
        {
            HitSection section = hitBarConfig.sections[i];
            int newLevelInt = (int)section.level + (skillLevel - 1);
            newLevelInt = Mathf.Clamp(newLevelInt, 0, 6);
            section.level = (SectionLevel)newLevelInt;
            leveledConfig.sections[i] = section;
        }

        return leveledConfig;
    }
}

// ==========================================
// 编辑器扩展面板 (Editor GUI)
// ==========================================
#if UNITY_EDITOR
[CustomEditor(typeof(SkillData))]
public class SkillDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        SkillData data = (SkillData)target;
        GUILayout.Space(15);
        GUILayout.Label("Add Skill Effect (SerializeReference)", EditorStyles.boldLabel);

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(SkillEffect)) && !t.IsAbstract);

        foreach (var type in types)
        {
            if (GUILayout.Button("Add " + type.Name))
            {
                Undo.RecordObject(data, "Add Effect");
                data.effects.Add((SkillEffect)Activator.CreateInstance(type));
                EditorUtility.SetDirty(data);
            }
        }
    }
}
#endif