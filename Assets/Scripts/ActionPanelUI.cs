using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 玩家操作面板 UI，负责回合开始时的主次级行动决策流转与体力预校验
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

        // TODO: 清空分类按钮上显示的“已选择：XXX”文字
        CheckReadyButtonState();
    }

    // ==========================================
    // 内部私有逻辑 (Private Methods)
    // ==========================================
    private void OpenSkillList(params SkillType[] typesToOpen)
    {
        var playerSkills = battleManager.playerEntity.roleData.equippedSkills;

        // 【核心修复】：计算当前真实的可用体力 (预扣除逻辑)
        int currentTotalStamina = battleManager.playerEntity.currentStamina;
        int availableStamina = currentTotalStamina;

        // 判断当前打开的是主行为还是副行为列表
        bool isOpeningMainAction = Array.Exists(typesToOpen, t => t == SkillType.Attack || t == SkillType.Defend || t == SkillType.Dodge);

        if (isOpeningMainAction && selectedSubAction != null)
        {
            // 如果在选主行为，需预先扣除已经选好的副行为的体力
            availableStamina -= selectedSubAction.staminaCost;
        }
        else if (!isOpeningMainAction && selectedMainAction != null)
        {
            // 如果在选副行为，需预先扣除已经选好的主行为的体力
            availableStamina -= selectedMainAction.staminaCost;
        }

        // 将计算好的可用体力传给二级列表面板
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

            CheckReadyButtonState();
        }, typesToOpen);
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