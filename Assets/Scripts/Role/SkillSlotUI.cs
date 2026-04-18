using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    public Button slotBtn;
    public Text nameText;
    public GameObject emptyNode;

    [Header("等级展示节点")]
    public GameObject levelNode;
    public Text levelText;

    public void UpdateUI(SkillSlot skillSlot)
    {
        if (skillSlot != null && skillSlot.skillData != null)
        {
            if (nameText) { nameText.gameObject.SetActive(true); nameText.text = skillSlot.skillData.skillName; }
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