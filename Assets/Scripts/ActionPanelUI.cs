using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 玩家操作面板 UI：负责主次技能选择、体力预校验以及选择结果的实时回显
/// </summary>
public class ActionPanelUI : MonoBehaviour
{
    [Header("UI References - Categories")]
    public Button attackBtn;
    public Button defendBtn;
    public Button specialBtn;
    public Button itemBtn;

    [Header("UI References - Controls")]
    public Button readyButton;
    public SkillListUI skillListUI;

    [Header("UI References - Selection Display")]
    [Tooltip("选好技能后显示的父节点")]
    public GameObject selectionNode;
    [Tooltip("用于显示技能名称组合的文本组件")]
    public Text selectionText;

    [Header("Core References")]
    public BattleManager battleManager;

    // ==========================================
    // 运行时状态 (Runtime State)
    // ==========================================
    private SkillData selectedMainAction;
    private SkillData selectedSubAction;

    // ==========================================
    // Unity 生命周期
    // ==========================================
    private void Start()
    {
        attackBtn.onClick.AddListener(() => OpenSkillList(SkillType.Attack));
        defendBtn.onClick.AddListener(() => OpenSkillList(SkillType.Defend, SkillType.Dodge));
        specialBtn.onClick.AddListener(() => OpenSkillList(SkillType.Special));
        itemBtn.onClick.AddListener(() => OpenSkillList(SkillType.Item));

        readyButton.onClick.AddListener(OnReadyClicked);
    }

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        selectedMainAction = null;
        selectedSubAction = null;

        // 回合开始时重置显示状态
        UpdateSelectionDisplay();
        CheckReadyButtonState();
    }

    // ==========================================
    // 内部私据逻辑 (Private Methods)
    // ==========================================

    private void OpenSkillList(params SkillType[] typesToOpen)
    {
        var playerSkills = battleManager.playerEntity.runtimeSkills;

        // 计算当前真实的可用体力 (预扣除逻辑)
        int currentTotalStamina = battleManager.playerEntity.currentStamina;
        int availableStamina = currentTotalStamina;

        bool isOpeningMainAction = Array.Exists(typesToOpen, t => t == SkillType.Attack || t == SkillType.Defend || t == SkillType.Dodge);

        if (isOpeningMainAction && selectedSubAction != null)
        {
            availableStamina -= selectedSubAction.staminaCost;
        }
        else if (!isOpeningMainAction && selectedMainAction != null)
        {
            availableStamina -= selectedMainAction.staminaCost;
        }

        skillListUI.OpenList(playerSkills, availableStamina, (chosenSkill) =>
        {
            if (chosenSkill.skillType == SkillType.Attack ||
                chosenSkill.skillType == SkillType.Defend ||
                chosenSkill.skillType == SkillType.Dodge)
            {
                selectedMainAction = chosenSkill;
            }
            else
            {
                selectedSubAction = chosenSkill;
            }

            // 更新显示节点与确认按钮状态
            UpdateSelectionDisplay();
            CheckReadyButtonState();
        }, typesToOpen);
    }

    /// <summary>
    /// 根据当前选择的技能，动态拼接并显示文本
    /// 遵循：次要技能(Sub) + 主要技能(Main) 的展示顺序
    /// </summary>
    private void UpdateSelectionDisplay()
    {
        if (selectionNode == null || selectionText == null) return;

        // 如果一个都没选，直接隐藏节点
        if (selectedMainAction == null && selectedSubAction == null)
        {
            selectionNode.SetActive(false);
            return;
        }

        selectionNode.SetActive(true);
        string displayText = "";

        // 情况 1: 两个都选了
        if (selectedSubAction != null && selectedMainAction != null)
        {
            displayText = $"{selectedSubAction.skillName} + {selectedMainAction.skillName}";
        }
        // 情况 2: 只选了次要技能
        else if (selectedSubAction != null)
        {
            displayText = selectedSubAction.skillName;
        }
        // 情况 3: 只选了主要技能
        else if (selectedMainAction != null)
        {
            displayText = selectedMainAction.skillName;
        }

        selectionText.text = displayText;
    }

    private void CheckReadyButtonState()
    {
        readyButton.interactable = (selectedMainAction != null);
    }

    private void OnReadyClicked()
    {
        gameObject.SetActive(false);
        battleManager.OnPlayerActionConfirmed(selectedMainAction, selectedSubAction);
    }
}