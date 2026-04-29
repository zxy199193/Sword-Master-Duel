using UnityEngine;
using UnityEngine.UI;
using System;

public class BagItemUI : MonoBehaviour
{
    [Header("UI 节点 - 基础信息")]
    public Text nameText;
    public Image iconImage;
    public Text descText;

    [Header("UI 节点 - 数量")]
    public GameObject quantityNode;
    public Text quantityText;

    [Header("UI 节点 - 状态与操作")]
    public GameObject equippedBadge;
    public Button selectBtn;
    public Button unequipBtn;

    // ==========================================
    // Public Methods
    // ==========================================

    /// <summary>角色界面使用（道具无卸下限制，始终可卸下）</summary>
    public void Setup(SkillSlot itemSlot, bool isEquipped,
        Action<SkillSlot> onSelect, Action<SkillSlot> onUnequip)
    {
        if (nameText) nameText.text = itemSlot.skillData.skillName;
        if (iconImage) iconImage.sprite = itemSlot.skillData.skillIcon;
        if (descText) descText.text = itemSlot.skillData.description;

        if (quantityNode) quantityNode.SetActive(true);
        if (quantityText) quantityText.text = itemSlot.quantity.ToString();

        if (equippedBadge) equippedBadge.SetActive(isEquipped);

        if (isEquipped)
        {
            if (selectBtn) selectBtn.gameObject.SetActive(false);
            if (unequipBtn)
            {
                unequipBtn.gameObject.SetActive(true);
                unequipBtn.interactable = true; // 道具无限制，始终可卸下
                unequipBtn.onClick.RemoveAllListeners();
                unequipBtn.onClick.AddListener(() => onUnequip?.Invoke(itemSlot));
            }
        }
        else
        {
            if (unequipBtn) unequipBtn.gameObject.SetActive(false);
            if (selectBtn)
            {
                selectBtn.gameObject.SetActive(true);
                selectBtn.onClick.RemoveAllListeners();
                selectBtn.onClick.AddListener(() => onSelect?.Invoke(itemSlot));
            }
        }
    }
}