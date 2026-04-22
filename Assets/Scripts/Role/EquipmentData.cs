using UnityEngine;
using System.Collections.Generic;
using static GlobalBattleRules;

#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Linq;
#endif

public enum EquipmentType { Weapon, Armor, Accessory }
public enum ItemQuality { Common, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "NewEquipment", menuName = "SwordMaster/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("基础信息")]
    public string equipName;
    [TextArea] public string description;
    public Sprite icon;
    public EquipmentType equipType;
    public ItemQuality quality = ItemQuality.Common;

    [Header("通用属性")]
    public int weight = 5;
    public int price = 100;

    [Header("静态属性加成 (佩戴即生效)")]
    public int bonusLife = 0;
    public int bonusStamina = 0;
    public int bonusVitality = 0;
    public int bonusEndurance = 0;
    public int bonusStrength = 0;
    public int bonusMentality = 0;

    [Header("武器专属属性")]
    public float atkFactor = 1.0f;

    [Header("防具专属属性")]
    public int durability = 0;

    [Header("动态战斗效果 (序列化多态)")]
    [SerializeReference]
    public List<EquipEffect> equipEffects = new List<EquipEffect>();
}

// ==========================================
// 编辑器扩展面板
// ==========================================
#if UNITY_EDITOR
[CustomEditor(typeof(EquipmentData))]
public class EquipmentDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EquipmentData data = (EquipmentData)target;
        
        GUILayout.Space(15);
        GUILayout.Label("Add Equipment Effect", EditorStyles.boldLabel);

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(EquipEffect)) && !t.IsAbstract);

        foreach (var type in types)
        {
            if (GUILayout.Button("Add " + type.Name))
            {
                Undo.RecordObject(data, "Add Equip Effect");
                data.equipEffects.Add((EquipEffect)Activator.CreateInstance(type));
                EditorUtility.SetDirty(data);
            }
        }
    }
}
#endif