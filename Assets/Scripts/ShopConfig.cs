using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewShopConfig", menuName = "SwordMaster/Shop Config")]
public class ShopConfig : ScriptableObject
{
    [Header("道场可购招式 (仅限1级)")]
    public List<SkillData> availableSkills = new List<SkillData>();

    [Header("商店可购装备")]
    public List<EquipmentData> availableEquipments = new List<EquipmentData>();

    [Header("商店可购道具 (可重复购买)")]
    public List<SkillData> availableItems = new List<SkillData>();

    [Header("摇奖池设定")]
    [Tooltip("开启后，摇奖可从上述列表中随机抽取奖励")]
    public bool isGachaActive = true;
}