using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 技能（招式/道具）列表面板 UI，负责动态生成并展示选项，处理资源不足的禁用逻辑
/// </summary>
public class SkillListUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentRoot;
    public GameObject skillItemPrefab;
    public Button closeButton;

    // ==========================================
    // 运行时状态 (Runtime State)
    // ==========================================
    private Action<SkillData> onSkillSelectedCallback;

    // ==========================================
    // Unity 生命周期 (Lifecycle)
    // ==========================================
    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    /// <summary>
    /// 根据过滤器类型和当前可用体力，打开并生成技能列表
    /// </summary>
    public void OpenList(List<SkillData> allSkills, BattleEntity caster, int availableStamina, Action<SkillData> callback, params SkillType[] filterTypes)
    {
        onSkillSelectedCallback = callback;
        gameObject.SetActive(true);

        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        foreach (var skill in allSkills)
        {
            if (Array.Exists(filterTypes, type => type == skill.skillType))
            {
                // 传给生成逻辑
                CreateSkillItemUI(skill, caster, availableStamina);
            }
        }
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    // ==========================================
    // 内部私有逻辑 (Private Methods)
    // ==========================================

    private void CreateSkillItemUI(SkillData skill, BattleEntity caster, int availableStamina)
    {
        GameObject go = Instantiate(skillItemPrefab, contentRoot);
        SkillItemUI itemUI = go.GetComponent<SkillItemUI>();

        if (itemUI != null)
        {
            // 初始化时带上 caster
            itemUI.Init(skill, caster, OnSkillSelected);

            // 变暗逻辑
            bool isExhausted = (skill.skillType == SkillType.Item && skill.quantity <= 0);
            bool isNoStamina = (skill.staminaCost > availableStamina);

            itemUI.SetAvailable(!isExhausted && !isNoStamina);
        }
        else
        {
            Debug.LogError($"预制体 {skillItemPrefab.name} 上缺少 SkillItemUI 脚本！");
        }
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        onSkillSelectedCallback?.Invoke(selectedSkill);
        ClosePanel();
    }
}