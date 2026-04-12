using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 技能列表子项 UI，负责展示单个技能的详细参数并处理点击事件
/// </summary>
public class SkillItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Text nameText;
    public Text detailsText;
    public Button selectButton;

    // ==========================================
    // 运行时状态 (Runtime State)
    // ==========================================
    private SkillData boundSkill;
    private Action<SkillData> onSelectedCallback;

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    /// <summary>
    /// 初始化技能 UI 项
    /// </summary>
    /// <param name="skill">绑定的技能数据</param>
    /// <param name="callback">被选中时的回调函数</param>
    public void Init(SkillData skill, Action<SkillData> callback)
    {
        boundSkill = skill;
        onSelectedCallback = callback;

        if (nameText != null)
        {
            nameText.text = skill.skillName;
        }

        UpdateDetailsText(skill);

        // 绑定事件前注销旧监听，防止复用对象池时重复触发
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClicked);
    }

    // ==========================================
    // 内部私有逻辑 (Private Methods)
    // ==========================================

    /// <summary>
    /// 根据不同的招式类型，动态拼接对应的参数详情文本
    /// </summary>
    private void UpdateDetailsText(SkillData skill)
    {
        if (detailsText == null) return;

        string details = $"消耗体力: <color=yellow>{skill.staminaCost}</color>\n";

        switch (skill.skillType)
        {
            case SkillType.Attack:
                details += $"基础伤害: <color=red>{skill.basicDamage}</color> | 连击数: {skill.hitTimes}";
                break;
            case SkillType.Defend:
                details += $"基础减伤: <color=green>{skill.basicDefend}</color> | 命中修正: +{skill.hitAmend}";
                break;
            case SkillType.Dodge:
                details += $"命中修正: <color=blue>{skill.hitAmend}</color>";
                break;
        }

        detailsText.text = details;
    }

    private void OnSelectClicked()
    {
        onSelectedCallback?.Invoke(boundSkill);
    }
}