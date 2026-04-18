using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    public Button slotBtn;
    public Image iconImg;
    public Text nameText;
    public GameObject emptyNode;

    [Header("道具专属节点")]
    public GameObject quantityNode;
    public Text quantityText;

    private void Awake()
    {
        if (slotBtn == null) slotBtn = GetComponent<Button>();
    }

    public void UpdateUI(SkillSlot itemSlot)
    {
        if (itemSlot != null && itemSlot.skillData != null)
        {
            if (iconImg) { iconImg.gameObject.SetActive(true); iconImg.sprite = itemSlot.skillData.skillIcon; }
            if (nameText) { nameText.gameObject.SetActive(true); nameText.text = itemSlot.skillData.skillName; }
            if (emptyNode) emptyNode.SetActive(false);

            if (quantityNode) quantityNode.SetActive(true);
            if (quantityText) quantityText.text = itemSlot.quantity.ToString();
        }
        else
        {
            if (iconImg) iconImg.gameObject.SetActive(false);
            if (nameText) nameText.gameObject.SetActive(false);
            if (emptyNode) emptyNode.SetActive(true);
            if (quantityNode) quantityNode.SetActive(false);
        }
    }
}