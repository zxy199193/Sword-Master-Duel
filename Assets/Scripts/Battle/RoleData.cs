using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SkillSlot
{
    public SkillData skillData;

    [Tooltip("该角色装配此招式的等级 (道具固定填1)")]
    [Range(1, 3)] public int level = 1;

    [Tooltip("持有数量 (仅道具/消耗品有效)")]
    public int quantity = 1;
}

[CreateAssetMenu(fileName = "NewRole", menuName = "SwordMaster/Role Data")]
public class RoleData : ScriptableObject
{
    [Header("角色信息")]
    public string roleName;
    public Sprite roleModel;
    [Header("视觉与动画")]
    public RuntimeAnimatorController animatorController; // 专属动画控制器

    [Header("基础属性")]
    public int maxBasicLife = 20;
    public int maxStamina = 10;
    public int strength = 1;
    public int mentality = 1;

    [Tooltip("每回合自动恢复体力")]
    public int staminaRecoverPerTurn = 3;

    [Header("战斗携带配置 (招式与道具)")]
    [Tooltip("该角色在战斗中装配的技能/道具与对应等级")]
    public List<SkillSlot> equippedSkills = new List<SkillSlot>();

    [Header("敌方专属装备配置 (NPC有效)")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public List<EquipmentData> equippedAccessories = new List<EquipmentData>();

    [Header("关卡与奖励设定")]
    [Tooltip("是否为关底 Boss（开启后无法撤退）")]
    public bool isBoss;
    [Tooltip("击败该敌人可获得的金币奖励")]
    public int goldReward = 50;
    public int expReward = 50;

    [Header("AI 设定 (NPC有效)")]
    [Tooltip("AI QTE 反应时间宽容度 (秒)。值越小AI越精准(0即完美)，值越大容易偏离中心。")]
    public float aiReactionTolerance = 0.2f;

    [Tooltip("AI 多阶段配置 (按血量百分比从高到低配置)")]
    public List<AIPhaseConfig> aiPhases = new List<AIPhaseConfig>();
}