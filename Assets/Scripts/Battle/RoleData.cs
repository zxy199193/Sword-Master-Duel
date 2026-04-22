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
    public RuntimeAnimatorController animatorController;

    [Header("基础属性")]
    public int maxBasicLife = 20;
    public int maxStamina = 10;
    public int strength = 0;
    public int mentality = 0;
    
    [Tooltip("每回合自动恢复体力")]
    public int staminaRecoverPerTurn = 3;

    [Header("敌方专属装备配置 (NPC有效)")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public List<EquipmentData> equippedAccessories = new List<EquipmentData>();

    [Header("AI 精准度偏差")]
    [Tooltip("AI QTE 反应时间宽容度 (秒)。值越小AI越精准(0即完美)，值越大容易偏离中心。")]
    public float aiReactionTolerance = 0.2f;

    [Header("敌方战斗携带配置 (NPC专用)")]
    [Tooltip("NPC在战斗中装配的技能/道具与对应阶段权重")]
    public List<NPCActionConfig> npcSkills = new List<NPCActionConfig>();

    [Tooltip("4个阶段释放副技能的概率：100~75%, 75~50%, 50~25%, 25~0%。填 -1 代表继承上一阶段概率")]
    public float[] subSkillProbabilities = new float[4] { 0.5f, -1f, -1f, -1f };

    [Header("关卡与奖励设定")]
    [Tooltip("是否为关底 Boss（开启后无法撤退）")]
    public bool isBoss;
    
    [Tooltip("击败该敌人可获得的金币奖励")]
    public int goldReward = 50;
    public int expReward = 50;

    // ==========================================
    // 编辑器自动化：强制锁定数组长度并填充默认值
    // ==========================================
    private void OnValidate()
    {
        if (subSkillProbabilities == null || subSkillProbabilities.Length != 4)
        {
            float[] newArr = new float[4] { 0.5f, -1f, -1f, -1f };
            if (subSkillProbabilities != null)
            {
                for (int i = 0; i < Mathf.Min(subSkillProbabilities.Length, 4); i++)
                    newArr[i] = subSkillProbabilities[i];
            }
            subSkillProbabilities = newArr;
        }

        if (npcSkills != null)
        {
            foreach (var config in npcSkills)
            {
                if (config.phaseWeights == null || config.phaseWeights.Length != 4)
                {
                    int[] newWeights = new int[4] { 10, -1, -1, -1 };
                    if (config.phaseWeights != null)
                    {
                        for (int i = 0; i < Mathf.Min(config.phaseWeights.Length, 4); i++)
                            newWeights[i] = config.phaseWeights[i];
                    }
                    config.phaseWeights = newWeights;
                }
            }
        }
    }
}