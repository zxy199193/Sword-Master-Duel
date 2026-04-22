using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class RoleSkillListUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Text titleText;
    public Transform contentRoot;
    public GameObject skillItemPrefab;
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

    public void OpenList(string tabName, List<SkillType> allowedTypes, SkillSlot currentEquipped, bool isOnlyOneLeft, Action<SkillSlot> onEquip, Action onUnequip)
    {
        gameObject.SetActive(true);
        onEquipAction = onEquip;
        onUnequipAction = onUnequip;
        
        if (titleText) titleText.text = $"选择{tabName}";

        foreach (Transform child in contentRoot) 
        {
            Destroy(child.gameObject);
        }

        PlayerProfile profile = GameManager.Instance.playerProfile;

        if (currentEquipped != null && currentEquipped.skillData != null)
        {
            CreateItemNode(currentEquipped, true, !isOnlyOneLeft);
        }

        foreach (var slot in profile.storageSkillsAndItems)
        {
            if (slot != null && slot.skillData != null && allowedTypes.Contains(slot.skillData.skillType))
            {
                CreateItemNode(slot, false, true);
            }
        }
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void CreateItemNode(SkillSlot slot, bool isEquipped, bool canUnequip)
    {
        GameObject go = Instantiate(skillItemPrefab, contentRoot);
        RoleSkillItemUI itemUI = go.GetComponent<RoleSkillItemUI>();
        
        if (itemUI != null)
        {
            itemUI.Setup(slot, isEquipped, canUnequip, (clickedSlot) =>
            {
                if (isEquipped) onUnequipAction?.Invoke();
                else onEquipAction?.Invoke(clickedSlot);
                
                gameObject.SetActive(false);
            });
        }
    }
}