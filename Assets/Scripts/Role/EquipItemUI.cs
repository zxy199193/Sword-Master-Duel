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

    [Header("UI 节点 - 状态与操作")]
    public GameObject equippedBadge;
    public Button actionBtn;
    public Text actionBtnText;

    [Header("UI 节点 - 商店专属")]
    public GameObject priceNode;
    public Text priceText;

    private EquipmentData currentData;

    // ==========================================
    // Public Methods
    // ==========================================

    public void Setup(EquipmentData equipData, bool isEquipped, bool canUnequip, Action<EquipmentData> onActionClicked)
    {
        PopulateBasicInfo(equipData);

        if (equippedBadge) equippedBadge.SetActive(isEquipped);
        if (priceNode) priceNode.SetActive(false);

        actionBtn.onClick.RemoveAllListeners();

        if (isEquipped)
        {
            if (!canUnequip) 
            {
                actionBtn.gameObject.SetActive(false);
            }
            else 
            { 
                actionBtn.gameObject.SetActive(true); 
                if (actionBtnText) actionBtnText.text = "卸下"; 
            }
        }
        else
        {
            actionBtn.gameObject.SetActive(true);
            if (actionBtnText) actionBtnText.text = "装备";
        }

        actionBtn.interactable = true;
        actionBtn.onClick.AddListener(() => onActionClicked?.Invoke(currentData));
    }

    public void SetupForShop(EquipmentData equipData, int price, bool canAfford, string btnText, Action<EquipmentData> onActionClicked)
    {
        PopulateBasicInfo(equipData);

        if (equippedBadge) equippedBadge.SetActive(false);

        if (priceNode) priceNode.SetActive(true);
        if (priceText)
        {
            priceText.text = price.ToString();
            priceText.color = canAfford ? Color.white : Color.red;
        }

        if (actionBtn != null)
        {
            actionBtn.gameObject.SetActive(true);
            actionBtn.interactable = canAfford;
            if (actionBtnText) actionBtnText.text = btnText;

            actionBtn.onClick.RemoveAllListeners();
            actionBtn.onClick.AddListener(() => onActionClicked?.Invoke(currentData));
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