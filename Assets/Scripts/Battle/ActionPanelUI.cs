using UnityEngine;
using UnityEngine.UI;
using System;

public class ActionPanelUI : MonoBehaviour
{
    [Header("UI References - Categories")]
    public Button attackBtn;
    public Button defendBtn;
    public Button specialBtn;
    public Button itemBtn;
    public Button retreatBtn;

    [Header("UI References - Controls")]
    public Button readyButton;
    public SkillListUI skillListUI;

    [Header("UI References - Selection Display")]
    public GameObject selectionNode;
    public Text selectionText;

    [Header("Core References")]
    public BattleManager battleManager;

    // 改为 SkillSlot
    private SkillSlot selectedMainAction;
    private SkillSlot selectedSubAction;

    private void Start()
    {
        attackBtn.onClick.AddListener(() => OpenSkillList(SkillType.Attack));
        defendBtn.onClick.AddListener(() => OpenSkillList(SkillType.Defend, SkillType.Dodge));
        specialBtn.onClick.AddListener(() => OpenSkillList(SkillType.Special));
        itemBtn.onClick.AddListener(() => OpenSkillList(SkillType.Item));

        readyButton.onClick.AddListener(OnReadyClicked);
        retreatBtn.onClick.AddListener(OnRetreatClicked);
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        selectedMainAction = null;
        selectedSubAction  = null;

        bool isBoss = battleManager.enemyEntity.roleData.isBoss;
        retreatBtn.interactable = !isBoss;

        // 清除待用体力预览
        RefreshPendingStaminaDisplay();
        UpdateSelectionDisplay();
        CheckReadyButtonState();
    }

    private void OpenSkillList(params SkillType[] typesToOpen)
    {
        var playerSkills     = battleManager.playerEntity.runtimeSkills;
        int availableStamina = battleManager.playerEntity.currentStamina;

        bool isOpeningMainAction = Array.Exists(typesToOpen, t => t == SkillType.Attack || t == SkillType.Defend || t == SkillType.Dodge);

        // 读取耗蓝时，必须传入等级
        if (isOpeningMainAction && selectedSubAction != null)
        {
            availableStamina -= selectedSubAction.skillData.GetStaminaCost(selectedSubAction.level);
        }
        else if (!isOpeningMainAction && selectedMainAction != null)
        {
            availableStamina -= selectedMainAction.skillData.GetStaminaCost(selectedMainAction.level);
        }

        skillListUI.OpenList(playerSkills, battleManager.playerEntity, availableStamina, 
        (chosenSlot) =>
        {
            if (chosenSlot.skillData.skillType == SkillType.Attack  ||
                chosenSlot.skillData.skillType == SkillType.Defend  ||
                chosenSlot.skillData.skillType == SkillType.Dodge)
            {
                selectedMainAction = chosenSlot;
            }
            else
            {
                selectedSubAction = chosenSlot;
            }

            // 选完技能后立刻刷新体力预览
            RefreshPendingStaminaDisplay();
            UpdateSelectionDisplay();
            CheckReadyButtonState();

        }, 
        (canceledSlot) =>
        {
            if (selectedMainAction == canceledSlot) selectedMainAction = null;
            if (selectedSubAction == canceledSlot) selectedSubAction = null;

            // 撤销后刷新体力预览
            RefreshPendingStaminaDisplay();
            UpdateSelectionDisplay();
            CheckReadyButtonState();
        },
        selectedMainAction, selectedSubAction, battleManager, typesToOpen);
    }

    private void UpdateSelectionDisplay()
    {
        if (selectionNode == null || selectionText == null) return;

        if (selectedMainAction == null && selectedSubAction == null)
        {
            selectionNode.SetActive(false);
            return;
        }

        selectionNode.SetActive(true);
        string displayText = "";

        // 加上 .skillData 才能读到名字
        if (selectedSubAction != null && selectedMainAction != null)
        {
            displayText = $"{selectedSubAction.skillData.skillName} + {selectedMainAction.skillData.skillName}";
        }
        else if (selectedSubAction != null)
        {
            displayText = selectedSubAction.skillData.skillName;
        }
        else if (selectedMainAction != null)
        {
            displayText = selectedMainAction.skillData.skillName;
        }

        selectionText.text = displayText;
    }

    private void CheckReadyButtonState()
    {
        readyButton.interactable = (selectedMainAction != null);
    }

    private void OnReadyClicked()
    {
        // 点击"准备完成"后，清除待用预览（体力将在 BattleManager 中真实扣除，届时 OnStaminaChanged 会刷新图标）
        battleManager.playerInfoUI.SetPendingStamina(0);
        battleManager.OnPlayerActionConfirmed(selectedMainAction, selectedSubAction);
    }

    private void OnRetreatClicked()
    {
        Debug.Log("<color=orange>玩家选择了撤退！</color>");
        battleManager.playerInfoUI.SetPendingStamina(0);
        gameObject.SetActive(false);
        GameManager.Instance.OnBattleRetreat();
    }

    // ==========================================
    // 待用体力预览
    // ==========================================

    /// <summary>
    /// 根据当前已选主/副技能，计算合计体力消耗并推送给玩家体力条
    /// </summary>
    private void RefreshPendingStaminaDisplay()
    {
        if (battleManager == null || battleManager.playerInfoUI == null) return;

        int totalCost = 0;

        if (selectedMainAction != null)
            totalCost += battleManager.GetActualSkillCost(battleManager.playerEntity, selectedMainAction, selectedSubAction);

        if (selectedSubAction != null)
            totalCost += battleManager.GetActualSkillCost(battleManager.playerEntity, selectedSubAction);

        battleManager.playerInfoUI.SetPendingStamina(totalCost);
    }
}