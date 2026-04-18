using UnityEngine;
using UnityEngine.UI;
using System;

public class BagItemUI : MonoBehaviour
{
    public Text nameText;
    public Image iconImage;
    public Text descText;

    [Header("数量节点")]
    public GameObject quantityNode;
    public Text quantityText;

    [Header("状态与操作")]
    public GameObject equippedBadge;
    public Button actionBtn;
    public Text actionBtnText;

    // 【修改点】：参数改为 SkillSlot
    public void Setup(SkillSlot itemSlot, bool isEquipped, Action<SkillSlot> onActionClicked)
    {
        if (nameText) nameText.text = itemSlot.skillData.skillName;
        if (iconImage) iconImage.sprite = itemSlot.skillData.skillIcon;
        if (descText) descText.text = itemSlot.skillData.description;

        if (quantityNode) quantityNode.SetActive(true);
        // 【修改点】：直接读取 slot 的 quantity
        if (quantityText) quantityText.text = itemSlot.quantity.ToString();

        if (equippedBadge) equippedBadge.SetActive(isEquipped);

        actionBtn.onClick.RemoveAllListeners();

        if (isEquipped)
        {
            if (actionBtnText) actionBtnText.text = "卸下";
        }
        else
        {
            if (actionBtnText) actionBtnText.text = "携带";
        }

        actionBtn.onClick.AddListener(() => onActionClicked?.Invoke(itemSlot));
    }
}