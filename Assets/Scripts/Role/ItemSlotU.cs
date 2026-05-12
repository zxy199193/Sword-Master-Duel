using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI 引用 - 基础信息")]
    public Button slotBtn;
    public Image iconImg;
    public Text nameText;
    public GameObject emptyNode;

    [Header("UI 引用 - 道具专属节点")]
    public GameObject quantityNode;
    public Text quantityText;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Awake()
    {
        if (slotBtn == null) slotBtn = GetComponent<Button>();
    }

    // ==========================================
    // Public Methods
    // ==========================================

    public void UpdateUI(SkillSlot itemSlot, int maxCapacity = 2)
    {
        if (itemSlot != null && itemSlot.skillData != null)
        {
            if (iconImg) 
            { 
                iconImg.gameObject.SetActive(true); 
                iconImg.sprite = itemSlot.skillData.skillIcon; 
            }
            if (nameText) 
            { 
                nameText.gameObject.SetActive(true); 
                nameText.text = itemSlot.skillData.skillName; 
            }
            if (emptyNode) emptyNode.SetActive(false);

            if (quantityNode) quantityNode.SetActive(true);
            if (quantityText) quantityText.text = $"{itemSlot.quantity}/{maxCapacity}";
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