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
        selectedSubAction = null;

        bool isBoss = battleManager.enemyEntity.roleData.isBoss;
        retreatBtn.interactable = !isBoss;

        UpdateSelectionDisplay();
        CheckReadyButtonState();
    }

    private void OpenSkillList(params SkillType[] typesToOpen)
    {
        var playerSkills = battleManager.playerEntity.runtimeSkills;
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

        skillListUI.OpenList(playerSkills, battleManager.playerEntity, availableStamina, (chosenSlot) =>
        {
            if (chosenSlot.skillData.skillType == SkillType.Attack ||
                chosenSlot.skillData.skillType == SkillType.Defend ||
                chosenSlot.skillData.skillType == SkillType.Dodge)
            {
                selectedMainAction = chosenSlot;
            }
            else
            {
                selectedSubAction = chosenSlot;
            }

            UpdateSelectionDisplay();
            CheckReadyButtonState();
        }, typesToOpen);
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
        battleManager.OnPlayerActionConfirmed(selectedMainAction, selectedSubAction);
    }

    private void OnRetreatClicked()
    {
        Debug.Log("<color=orange>玩家选择了撤退！</color>");
        gameObject.SetActive(false);
        GameManager.Instance.OnBattleRetreat();
    }
}