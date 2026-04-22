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

    // 回调类型改为 SkillSlot
    private Action<SkillSlot> onSkillSelectedCallback;

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    // 列表类型改为 List<SkillSlot>
    public void OpenList(List<SkillSlot> allSkills, BattleEntity caster, int availableStamina, Action<SkillSlot> callback, params SkillType[] filterTypes)
    {
        onSkillSelectedCallback = callback;
        gameObject.SetActive(true);

        foreach (Transform child in contentRoot) Destroy(child.gameObject);

        foreach (var slot in allSkills)
        {
            // 通过 slot.skillData 读取枚举
            if (slot != null && slot.skillData != null && Array.Exists(filterTypes, type => type == slot.skillData.skillType))
            {
                CreateSkillItemUI(slot, caster, availableStamina);
            }
        }
    }

    public void ClosePanel() { gameObject.SetActive(false); }

    private void CreateSkillItemUI(SkillSlot slot, BattleEntity caster, int availableStamina)
    {
        GameObject go = Instantiate(skillItemPrefab, contentRoot);
        // 注意：这里是你战斗 UI 专用的 SkillItemUI 组件
        SkillItemUI itemUI = go.GetComponent<SkillItemUI>();

        if (itemUI != null)
        {
            itemUI.Init(slot, caster, OnSkillSelected);

            // 读取解耦后的 quantity 和 GetStaminaCost
            bool isExhausted = (slot.skillData.skillType == SkillType.Item && slot.quantity <= 0);
            bool isNoStamina = (slot.skillData.GetStaminaCost(slot.level) > availableStamina);

            itemUI.SetAvailable(!isExhausted && !isNoStamina);
        }
        else
        {
            Debug.LogError($"预制体 {skillItemPrefab.name} 上缺少 SkillItemUI 脚本！");
        }
    }

    private void OnSkillSelected(SkillSlot selectedSlot)
    {
        onSkillSelectedCallback?.Invoke(selectedSlot);
        ClosePanel();
    }
}