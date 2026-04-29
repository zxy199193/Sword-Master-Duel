using UnityEngine;
using UnityEngine.UI;
using System;

public class EquipItemUI : MonoBehaviour
{
    [Header("UI 节点 - 基础信息")]
    public Text nameText;
    public Image iconImage;
    public Text descText;
    public Text weightText;

    [Header("UI 节点 - 武器专属")]
    public GameObject weaponStatNode;
    public Text atkFactorText;

    [Header("UI 节点 - 防具专属")]
    public GameObject armorStatNode;
    public Text durabilityText;

    [Header("UI 节点 - 角色界面")]
    public GameObject equippedBadge;
    public Button selectBtn;
    public Button unequipBtn;

    [Header("UI 节点 - 商店专属")]
    public Button buyBtn;
    public Text buyBtnText;
    public GameObject ownedBadge;

    private EquipmentData currentData;

    // ==========================================
    // Public Methods
    // ==========================================

    /// <summary>角色界面使用</summary>
    public void Setup(EquipmentData equipData, bool isEquipped, bool canUnequip,
        Action<EquipmentData> onSelect, Action<EquipmentData> onUnequip)
    {
        PopulateBasicInfo(equipData);

        // 隐藏商店专属节点
        if (buyBtn) buyBtn.gameObject.SetActive(false);
        if (ownedBadge) ownedBadge.SetActive(false);

        if (equippedBadge) equippedBadge.SetActive(isEquipped);

        if (isEquipped)
        {
            if (selectBtn) selectBtn.gameObject.SetActive(false);
            if (unequipBtn)
            {
                unequipBtn.gameObject.SetActive(true);
                unequipBtn.interactable = canUnequip;
                unequipBtn.onClick.RemoveAllListeners();
                if (canUnequip)
                    unequipBtn.onClick.AddListener(() => onUnequip?.Invoke(currentData));
            }
        }
        else
        {
            if (unequipBtn) unequipBtn.gameObject.SetActive(false);
            if (selectBtn)
            {
                selectBtn.gameObject.SetActive(true);
                selectBtn.onClick.RemoveAllListeners();
                selectBtn.onClick.AddListener(() => onSelect?.Invoke(currentData));
            }
        }
    }

    /// <summary>商店界面使用</summary>
    public void SetupForShop(EquipmentData equipData, int price, bool canAfford, bool isOwned,
        Action<EquipmentData> onBuy)
    {
        PopulateBasicInfo(equipData);

        // 隐藏角色界面专属节点
        if (equippedBadge) equippedBadge.SetActive(false);
        if (selectBtn) selectBtn.gameObject.SetActive(false);
        if (unequipBtn) unequipBtn.gameObject.SetActive(false);

        if (isOwned)
        {
            if (buyBtn) buyBtn.gameObject.SetActive(false);
            if (ownedBadge) ownedBadge.SetActive(true);
        }
        else
        {
            if (ownedBadge) ownedBadge.SetActive(false);
            if (buyBtn)
            {
                buyBtn.gameObject.SetActive(true);
                buyBtn.interactable = canAfford;
                if (buyBtnText) buyBtnText.text = price.ToString();
                buyBtn.onClick.RemoveAllListeners();
                buyBtn.onClick.AddListener(() => onBuy?.Invoke(currentData));
            }
        }
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void PopulateBasicInfo(EquipmentData equipData)
    {
        currentData = equipData;

        if (nameText) nameText.text = currentData.equipName;
        if (iconImage) iconImage.sprite = currentData.icon;
        if (descText) descText.text = currentData.description;
        if (weightText) weightText.text = $"{currentData.weight}";

        if (weaponStatNode) weaponStatNode.SetActive(currentData.equipType == EquipmentType.Weapon);
        if (armorStatNode) armorStatNode.SetActive(currentData.equipType == EquipmentType.Armor);

        if (currentData.equipType == EquipmentType.Weapon && atkFactorText)
            atkFactorText.text = $"{currentData.atkFactor}";
        else if (currentData.equipType == EquipmentType.Armor && durabilityText)
            durabilityText.text = $"{currentData.durability}";
    }
}