using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SkillSlot
{
    public SkillData skillData;
    [Tooltip("该角色装配此招式的等级 (道具固定填1即可)")]
    [Range(1, 3)] public int level = 1;
}

[CreateAssetMenu(fileName = "NewRole", menuName = "SwordMaster/Role Data")]
public class RoleData : ScriptableObject
{
    [Header("角色信息")]
    public string roleName;
    public Sprite roleModel;

    [Header("基础属性")]
    public int maxBasicLife = 20;
    public int maxStamina = 10;
    public int strength = 1;
    public int mentality = 1;

    [Tooltip("每回合自动恢复体力")]
    public int staminaRecoverPerTurn = 3;

    [Header("战斗携带配置")]
    [Tooltip("该角色在战斗中装配的技能与对应等级")]
    public List<SkillSlot> equippedSkills = new List<SkillSlot>();

    [Header("AI 设定 (仅NPC有效)")]
    [Tooltip("打击条判定偏差范围，如 X:-5, Y:5 代表在正负5范围内波动")]
    public Vector2 hitBarDeviation = new Vector2(-5f, 5f);

    [Tooltip("AI 多阶段配置 (请按血量百分比从高到低配置，例如阶段一填 1.0，阶段二填 0.5)")]
    public List<AIPhaseConfig> aiPhases = new List<AIPhaseConfig>();
}