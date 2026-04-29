using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipShopCategory
{
    public string categoryName;
    [Header("常驻装备")]
    public List<EquipmentData> permanentEquips = new List<EquipmentData>();
    [Header("随机装备池")]
    public List<EquipmentData> randomEquipsPool = new List<EquipmentData>();
    [Header("单次随机数量")]
    public int randomCount = 3;
}

[Serializable]
public class ItemShopCategory
{
    public string categoryName;
    [Header("常驻道具")]
    public List<SkillData> permanentItems = new List<SkillData>();
    [Header("随机道具池")]
    public List<SkillData> randomItemsPool = new List<SkillData>();
    [Header("单次随机数量")]
    public int randomCount = 5;
}

[Serializable]
public class DojoSkillCategory
{
    [Header("常驻招式（每局固定出现）")]
    public List<SkillData> permanentSkills = new List<SkillData>();
    [Header("随机招式池（每大关刷新一次）")]
    public List<SkillData> randomSkillsPool = new List<SkillData>();
    [Header("单次随机数量")]
    public int randomCount = 4;
}

[CreateAssetMenu(fileName = "NewShopConfig", menuName = "SwordMaster/Shop Config")]
public class ShopConfig : ScriptableObject
{
    [Header("刷新金币消耗")]
    public int refreshCost = 50;

    [Header("分类商店配置")]
    public EquipShopCategory weaponShop = new EquipShopCategory { categoryName = "武器" };
    public EquipShopCategory armorShop = new EquipShopCategory { categoryName = "防具" };
    public EquipShopCategory accessoryShop = new EquipShopCategory { categoryName = "饰品" };
    public ItemShopCategory itemShop = new ItemShopCategory { categoryName = "道具" };

    [Header("道场招式配置")]
    public DojoSkillCategory dojoSkillShop = new DojoSkillCategory();
}