using UnityEngine;
using UnityEngine.UI;
using System;

public class ItemListUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Transform contentRoot;
    public GameObject bagItemPrefab;
    public Button closeBtn;

    private Action<SkillSlot> onEquipAction;
    private Action onUnequipAction;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Awake()
    {
        if (closeBtn) closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
    }

    // ==========================================
    // Public Methods
    // ==========================================

    public void OpenList(SkillSlot currentEquipped, Action<SkillSlot> onEquip, Action onUnequip)
    {
        gameObject.SetActive(true);
        onEquipAction = onEquip;
        onUnequipAction = onUnequip;

        foreach (Transform child in contentRoot) 
        {
            Destroy(child.gameObject);
        }

        PlayerProfile profile = GameManager.Instance.playerProfile;

        if (currentEquipped != null && currentEquipped.skillData != null)
        {
            CreateItemNode(currentEquipped, true);
        }

        foreach (var slot in profile.storageSkillsAndItems)
        {
            if (slot != null && slot.skillData != null && slot.skillData.skillType == SkillType.Item)
            {
                CreateItemNode(slot, false);
            }
        }
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void CreateItemNode(SkillSlot slot, bool isEquipped)
    {
        GameObject go = Instantiate(bagItemPrefab, contentRoot);
        BagItemUI itemUI = go.GetComponent<BagItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(slot, isEquipped,
                (clickedSlot) => { onEquipAction?.Invoke(clickedSlot); gameObject.SetActive(false); },
                (clickedSlot) => { onUnequipAction?.Invoke(); gameObject.SetActive(false); });
        }
    }
}