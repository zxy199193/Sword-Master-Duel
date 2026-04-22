using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class EquipListUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Text titleText;
    public Transform contentRoot;
    public GameObject equipItemPrefab;
    public Button closeBtn;

    private Action<EquipmentData> onEquipAction;
    private Action onUnequipAction;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Start()
    {
        if (closeBtn) closeBtn.onClick.AddListener(ClosePanel);
    }

    // ==========================================
    // Public Methods
    // ==========================================

    public void OpenList(EquipmentType typeToFilter, EquipmentData currentEquipped, Action<EquipmentData> onEquip, Action onUnequip)
    {
        gameObject.SetActive(true);
        onEquipAction = onEquip;
        onUnequipAction = onUnequip;

        string typeName = typeToFilter == EquipmentType.Weapon ? "武器" : (typeToFilter == EquipmentType.Armor ? "防具" : "饰品");
        if (titleText) titleText.text = $"选择{typeName}";

        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        PlayerProfile profile = GameManager.Instance.playerProfile;

        if (currentEquipped != null)
        {
            bool canUnequip = typeToFilter != EquipmentType.Weapon;
            CreateItemNode(currentEquipped, true, canUnequip);
        }

        foreach (var equip in profile.storageEquipments)
        {
            if (equip.equipType == typeToFilter)
            {
                CreateItemNode(equip, false, true);
            }
        }
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void CreateItemNode(EquipmentData data, bool isEquipped, bool canUnequip)
    {
        GameObject go = Instantiate(equipItemPrefab, contentRoot);
        EquipItemUI itemUI = go.GetComponent<EquipItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(data, isEquipped, canUnequip, (clickedData) =>
            {
                if (isEquipped)
                {
                    onUnequipAction?.Invoke();
                }
                else
                {
                    onEquipAction?.Invoke(clickedData);
                }
                ClosePanel();
            });
        }
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}