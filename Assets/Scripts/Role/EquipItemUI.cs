using UnityEngine;
using UnityEngine.UI;
using System;

public class EquipItemUI : MonoBehaviour
{
    [Header("基础信息节点")]
    public Text nameText;
    public Image iconImage;
    public Text descText;
    public Text weightText;

    [Header("武器专属节点")]
    public GameObject weaponStatNode;
    public Text atkFactorText;

    [Header("防具专属节点")]
    public GameObject armorStatNode;
    public Text durabilityText;

    [Header("状态与操作节点")]
    public GameObject equippedBadge;
    public Button actionBtn;
    public Text actionBtnText;

    [Header("商店专属节点 (新增)")]
    public GameObject priceNode;
    public Text priceText;

    private EquipmentData currentData;

    // ==========================================
    // 提取公共数据绑定逻辑
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

    // ==========================================
    // 背包模式初始化
    // ==========================================
    public void Setup(EquipmentData equipData, bool isEquipped, bool canUnequip, Action<EquipmentData> onActionClicked)
    {
        PopulateBasicInfo(equipData);

        if (equippedBadge) equippedBadge.SetActive(isEquipped);
        if (priceNode) priceNode.SetActive(false); // 背包隐藏价格

        actionBtn.onClick.RemoveAllListeners();

        if (isEquipped)
        {
            if (!canUnequip) actionBtn.gameObject.SetActive(false);
            else { actionBtn.gameObject.SetActive(true); if (actionBtnText) actionBtnText.text = "卸下"; }
        }
        else
        {
            actionBtn.gameObject.SetActive(true);
            if (actionBtnText) actionBtnText.text = "装备";
        }

        actionBtn.interactable = true;
        actionBtn.onClick.AddListener(() => onActionClicked?.Invoke(currentData));
    }

    // ==========================================
    // 商店模式初始化 (新增)
    // ==========================================
    public void SetupForShop(EquipmentData equipData, int price, bool canAfford, string btnText, Action<EquipmentData> onActionClicked)
    {
        PopulateBasicInfo(equipData);

        if (equippedBadge) equippedBadge.SetActive(false); // 商店隐藏佩戴标记

        if (priceNode) priceNode.SetActive(true);
        if (priceText)
        {
            priceText.text = price.ToString();
            priceText.color = canAfford ? Color.white : Color.red; // 买不起爆红
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
}