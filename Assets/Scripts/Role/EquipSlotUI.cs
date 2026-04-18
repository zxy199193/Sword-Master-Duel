using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载在主界面单个装备槽位预制体上，负责管理自身的表现节点
/// </summary>
public class EquipSlotUI : MonoBehaviour
{
    public Button slotBtn;
    public Image iconImg;
    public Text nameText;
    public GameObject emptyNode;

    private void Awake()
    {
        if (slotBtn == null) slotBtn = GetComponent<Button>();
    }

    public void UpdateUI(EquipmentData equipData)
    {
        if (equipData != null)
        {
            if (iconImg) { iconImg.gameObject.SetActive(true); iconImg.sprite = equipData.icon; }
            if (nameText) { nameText.gameObject.SetActive(true); nameText.text = equipData.equipName; }
            if (emptyNode) emptyNode.SetActive(false);
        }
        else
        {
            if (iconImg) iconImg.gameObject.SetActive(false);
            if (nameText) nameText.gameObject.SetActive(false);
            if (emptyNode) emptyNode.SetActive(true);
        }
    }
}