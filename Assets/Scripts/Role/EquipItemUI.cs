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
    public GameObject equippedBadge; // 【新增】：已装备的角标 / 图标节点
    public Button actionBtn;         // 操作按钮（也可以把整个底板挂 Button 拖给它）
    public Text actionBtnText;

    private EquipmentData currentData;

    public void Setup(EquipmentData equipData, bool isEquipped, bool canUnequip, Action<EquipmentData> onActionClicked)
    {
        currentData = equipData;

        // 1. 基础信息赋值
        if (nameText) nameText.text = currentData.equipName;
        if (iconImage) iconImage.sprite = currentData.icon;
        if (descText) descText.text = currentData.description;
        if (weightText) weightText.text = $"{currentData.weight}";

        // 2. 根据类型开关专属节点
        if (weaponStatNode) weaponStatNode.SetActive(currentData.equipType == EquipmentType.Weapon);
        if (armorStatNode) armorStatNode.SetActive(currentData.equipType == EquipmentType.Armor);

        if (currentData.equipType == EquipmentType.Weapon && atkFactorText)
            atkFactorText.text = $"{currentData.atkFactor}";
        else if (currentData.equipType == EquipmentType.Armor && durabilityText)
            durabilityText.text = $"{currentData.durability}";

        // 3. 【新增】：处理已装备状态与操作逻辑
        if (equippedBadge) equippedBadge.SetActive(isEquipped);

        actionBtn.onClick.RemoveAllListeners();

        if (isEquipped)
        {
            if (!canUnequip)
            {
                // 如果是武器（不能卸下），直接隐藏按钮或关闭交互
                actionBtn.gameObject.SetActive(false);
            }
            else
            {
                // 其他装备，显示为“卸下”
                actionBtn.gameObject.SetActive(true);
                if (actionBtnText) actionBtnText.text = "卸下";
            }
        }
        else
        {
            actionBtn.gameObject.SetActive(true);
            if (actionBtnText) actionBtnText.text = "装备";
        }

        actionBtn.onClick.AddListener(() => onActionClicked?.Invoke(currentData));
    }
}