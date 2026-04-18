using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class EquipListUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Text titleText;
    public Transform contentRoot;      // ScrollView 的 Content 节点
    public GameObject equipItemPrefab; // 挂载了 EquipItemUI 的预制体
    public Button closeBtn;

    // 回调委托：当玩家做出选择时，通知主面板处理数据
    private Action<EquipmentData> onEquipAction;
    private Action onUnequipAction;

    private void Start()
    {
        if (closeBtn) closeBtn.onClick.AddListener(ClosePanel);
    }

    /// <summary>
    /// 打开列表并渲染指定类型的装备
    /// </summary>
    public void OpenList(EquipmentType typeToFilter, EquipmentData currentEquipped, Action<EquipmentData> onEquip, Action onUnequip)
    {
        gameObject.SetActive(true);
        onEquipAction = onEquip;
        onUnequipAction = onUnequip;

        string typeName = typeToFilter == EquipmentType.Weapon ? "武器" : (typeToFilter == EquipmentType.Armor ? "防具" : "饰品");
        if (titleText) titleText.text = $"选择{typeName}";

        // 清空旧列表
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        PlayerProfile profile = GameManager.Instance.playerProfile;

        // 1. 首先渲染当前身上穿戴的装备 (如果存在)
        if (currentEquipped != null)
        {
            // 规则：武器不能卸下
            bool canUnequip = typeToFilter != EquipmentType.Weapon;
            CreateItemNode(currentEquipped, true, canUnequip);
        }

        // 2. 遍历无限仓库，筛选同类型未穿戴的装备
        foreach (var equip in profile.storageEquipments)
        {
            if (equip.equipType == typeToFilter)
            {
                CreateItemNode(equip, false, true);
            }
        }
    }

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
                    onUnequipAction?.Invoke(); // 卸下
                }
                else
                {
                    onEquipAction?.Invoke(clickedData); // 穿戴新装备
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