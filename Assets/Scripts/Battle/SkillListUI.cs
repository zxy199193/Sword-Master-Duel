using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class SkillListUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentRoot;
    public GameObject skillItemPrefab;
    public Button closeButton;

    [Header("修正数值 Toggle")]
    public Toggle modifiedStatsToggle;

    private Action<SkillSlot> onSkillSelectedCallback;
    private Action<SkillSlot> onSkillCanceledCallback;
    private BattleManager battleManager;

    // 保存所有当前生成的卡片，用于 Toggle 切换时刷新
    private List<SkillItemUI> spawnedItems = new List<SkillItemUI>();

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (modifiedStatsToggle != null)
        {
            modifiedStatsToggle.isOn = true; // 默认开启修正模式
            modifiedStatsToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    public void OpenList(List<SkillSlot> allSkills, BattleEntity caster, int availableStamina, Action<SkillSlot> onSelect, Action<SkillSlot> onCancel, SkillSlot currentMain, SkillSlot currentSub, BattleManager manager, params SkillType[] filterTypes)
    {
        battleManager = manager;
        onSkillSelectedCallback = onSelect;
        onSkillCanceledCallback = onCancel;
        gameObject.SetActive(true);

        foreach (Transform child in contentRoot) Destroy(child.gameObject);
        spawnedItems.Clear();

        bool showModified = modifiedStatsToggle != null && modifiedStatsToggle.isOn;

        foreach (var slot in allSkills)
        {
            if (slot != null && slot.skillData != null && Array.Exists(filterTypes, type => type == slot.skillData.skillType))
            {
                bool isSelected = (slot == currentMain || slot == currentSub);
                CreateSkillItemUI(slot, caster, availableStamina, showModified, isSelected);
            }
        }
    }

    public void ClosePanel() { gameObject.SetActive(false); }

    private void CreateSkillItemUI(SkillSlot slot, BattleEntity caster, int availableStamina, bool showModified, bool isSelected)
    {
        GameObject go = Instantiate(skillItemPrefab, contentRoot);
        SkillItemUI itemUI = go.GetComponent<SkillItemUI>();

        if (itemUI != null)
        {
            itemUI.Init(slot, caster, OnSkillSelected, OnSkillCanceled, isSelected, battleManager);

            bool isExhausted = (slot.skillData.skillType == SkillType.Item && slot.quantity <= 0);
            int actualCost = (battleManager != null) 
                ? battleManager.GetActualSkillCost(caster, slot) 
                : Mathf.RoundToInt(slot.skillData.GetStaminaCost(slot.level));
            bool isNoStamina = (actualCost > availableStamina);
            
            // 麻痹判定：无法使用闪避技能
            bool isParalyzed = caster.activeStatuses.ContainsKey(StatusType.Paralyzed) && slot.skillData.skillType == SkillType.Dodge;
            
            itemUI.SetAvailable(!isExhausted && !isNoStamina && !isParalyzed);

            // 按当前 Toggle 状态初始渲染
            itemUI.RefreshStats(showModified);
            spawnedItems.Add(itemUI);
        }
        else
        {
            Debug.LogError($"预制体 {skillItemPrefab.name} 上缺少 SkillItemUI 脚本！");
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        // Toggle 切换时刷新所有已生成的卡片
        foreach (var item in spawnedItems)
        {
            if (item != null) item.RefreshStats(isOn);
        }
    }

    private void OnSkillSelected(SkillSlot selectedSlot)
    {
        onSkillSelectedCallback?.Invoke(selectedSlot);
        ClosePanel();
    }

    private void OnSkillCanceled(SkillSlot canceledSlot)
    {
        onSkillCanceledCallback?.Invoke(canceledSlot);
        ClosePanel();
    }
}