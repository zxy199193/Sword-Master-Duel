using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI 引用 - 基础节点")]
    public Button slotBtn;
    public Text nameText;
    public GameObject emptyNode;

    [Header("UI 引用 - 等级展示")]
    public GameObject levelNode;
    public Text levelText;

    // ==========================================
    // Public Methods
    // ==========================================

    public void UpdateUI(SkillSlot skillSlot)
    {
        if (skillSlot != null && skillSlot.skillData != null)
        {
            if (nameText) 
            { 
                nameText.gameObject.SetActive(true); 
                nameText.text = skillSlot.skillData.skillName; 
            }
            if (emptyNode) emptyNode.SetActive(false);

            if (levelNode) levelNode.SetActive(true);
            if (levelText) levelText.text = "Lv." + skillSlot.level;
        }
        else
        {
            if (nameText) nameText.gameObject.SetActive(false);
            if (emptyNode) emptyNode.SetActive(true);
            if (levelNode) levelNode.SetActive(false);
        }
    }
}