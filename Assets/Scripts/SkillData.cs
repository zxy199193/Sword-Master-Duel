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
    public int skillLevel = 1;

    [Tooltip("对应的动画Trigger名称 (如 Attack, Slash_3Hit 等)")]
    public string animationTriggerName = "Attack";

    [Header("Combat Stats")]
    [Tooltip("消耗的体力")]
    public int staminaCost;
    [Tooltip("基础伤害 (仅攻击招式有效)")]
    public float basicDamage;
    [Tooltip("基础减伤 (仅防御招式有效)")]
    public float basicDefend;
    [Tooltip("命中修正 (仅防守/闪避有效。正数加宽，负数变窄)")]
    public float hitAmend;

    [Header("Item Properties")]
    public int quantity;
    public bool isConsumable;

    [Header("Hit Bar Settings")]
    [Tooltip("该招式攻击/连击次数")]
    public int hitTimes = 1;
    public HitBarConfig hitBarConfig;

    [Header("Skill Effects")]
    [Tooltip("附带的多态特殊效果")]
    [SerializeReference]
    public List<SkillEffect> effects = new List<SkillEffect>();
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

        // 自动检索所有继承自 SkillEffect 的非抽象类
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(SkillEffect)) && !t.IsAbstract);

        // 动态生成添加按钮
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