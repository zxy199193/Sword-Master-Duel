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
    public void OpenList(List<SkillData> allSkills, int availableStamina, Action<SkillData> callback, params SkillType[] filterTypes)
    {
        onSkillSelectedCallback = callback;
        gameObject.SetActive(true);

        // 清空旧数据
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        // 遍历并生成符合过滤条件的技能UI
        foreach (var skill in allSkills)
        {
            if (Array.Exists(filterTypes, type => type == skill.skillType))
            {
                // 【核心修改】：将算好的可用体力传给生成方法
                CreateSkillItemUI(skill, availableStamina);
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

    private void CreateSkillItemUI(SkillData skill, int availableStamina)
    {
        GameObject go = Instantiate(skillItemPrefab, contentRoot);
        SkillItemUI itemUI = go.GetComponent<SkillItemUI>();

        if (itemUI != null)
        {
            itemUI.Init(skill, OnSkillSelected);

            // 【核心修复】：整合道具耗尽与体力不足的判定逻辑
            bool isExhausted = (skill.skillType == SkillType.Item && skill.quantity <= 0);
            bool isNoStamina = (skill.staminaCost > availableStamina);

            // 如果满足任一不可选条件，则进行 UI 置灰处理
            if (isExhausted || isNoStamina)
            {
                Button btn = go.GetComponent<Button>();
                if (btn == null) btn = go.GetComponentInChildren<Button>();

                if (btn != null)
                {
                    btn.interactable = false;
                }

                Text[] texts = go.GetComponentsInChildren<Text>();
                foreach (var t in texts)
                {
                    t.color = Color.gray;
                    if (t.text == skill.skillName)
                    {
                        // 根据具体原因加上相应的文字后缀提示
                        if (isExhausted) t.text += " (耗尽)";
                        else if (isNoStamina) t.text += " (体力不足)";
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"[SkillListUI] 致命错误：预制体 {skillItemPrefab.name} 上缺少 SkillItemUI 组件！");
        }
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        onSkillSelectedCallback?.Invoke(selectedSkill);
        ClosePanel();
    }
}