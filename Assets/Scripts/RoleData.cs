using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRole", menuName = "SwordMaster/Role Data")]
public class RoleData : ScriptableObject
{
    [Header("Basic Info")]
    public string roleName;
    public Sprite roleModel;

    [Header("Core Attributes")]
    public int maxBasicLife = 100;
    public int maxStamina = 50;
    public int strength = 10;
    public int mentality = 10;

    [Tooltip("每回合自动恢复体力")]
    public int staminaRecoverPerTurn = 3;

    [Header("Combat Loadout")]
    [Tooltip("该角色在战斗中可用的所有招式/道具")]
    public List<SkillData> equippedSkills = new List<SkillData>();

    [Header("AI Settings (NPC Only)")]
    [Tooltip("打击条判定偏差范围，如 X:-5, Y:5 代表在正负5范围内波动")]
    public Vector2 hitBarDeviation = new Vector2(-5f, 5f);
}