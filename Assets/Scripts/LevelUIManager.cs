using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡选择界面：显示 A、B 两组共6个敌人，及基础奖励和各组额外奖励。
/// 玩家点击"开始战斗（A组）"或"开始战斗（B组）"按钮进入对应战斗序列。
/// </summary>
public class LevelUIManager : MonoBehaviour
{
    // ==========================================
    // Inspector 绑定
    // ==========================================

    [Header("关卡标题")]
    public Text levelTitleText;

    [Header("A 组敌人展示（需绑定3个）")]
    public Image[] groupAIcons = new Image[3];
    public Text[]  groupANames = new Text[3];
    public Text[]  groupADescs = new Text[3];

    [Header("B 组敌人展示（需绑定3个）")]
    public Image[] groupBIcons = new Image[3];
    public Text[]  groupBNames = new Text[3];
    public Text[]  groupBDescs = new Text[3];

    [Header("开始战斗按钮")]
    public Button startBattleBtnA;   // 选 A 组
    public Button startBattleBtnB;   // 选 B 组

    [Header("基础奖励显示")]
    public Text baseGoldText;
    public Text baseExpText;

    [Header("额外奖励预制体（需挂 LevelRewardItemUI 脚本）")]
    public GameObject rewardItemPrefab;

    [Header("A 组额外奖励挂载节点")]
    public Transform groupARewardParent;

    [Header("B 组额外奖励挂载节点")]
    public Transform groupBRewardParent;

    [Header("返回休息场景")]
    [Tooltip("第一关时置灰不可点击；第二关起可点击返回休息场景")]
    public Button returnToRestBtn;

    [Header("角色面板")]
    public Button openRolePanelBtn;
    public RoleUIManager roleUIManager;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Start()
    {
        if (startBattleBtnA != null)
            startBattleBtnA.onClick.AddListener(() => OnStartBattleClicked(true));

        if (startBattleBtnB != null)
            startBattleBtnB.onClick.AddListener(() => OnStartBattleClicked(false));

        if (openRolePanelBtn != null && roleUIManager != null)
            openRolePanelBtn.onClick.AddListener(() => roleUIManager.ShowPanel());

        if (returnToRestBtn != null)
            returnToRestBtn.onClick.AddListener(OnReturnToRestClicked);
    }

    // ==========================================
    // Public Methods
    // ==========================================

    /// <summary>
    /// 由 GameManager.EnterLevelNodeUI() 调用，填充 A、B 两组数据并显示界面。
    /// </summary>
    public void UpdateAndShow(LevelData levelData,
                              List<RoleData> groupA, List<RoleData> groupB,
                              LevelExtraRewardEntry rewardA, LevelExtraRewardEntry rewardB)
    {
        gameObject.SetActive(true);

        // ── 关卡标题 ──
        if (levelTitleText != null)
            levelTitleText.text = $"关卡 {levelData.levelTitle}";

        // ── A / B 组敌人 ──
        RefreshEnemyGroup(groupA, groupAIcons, groupANames, groupADescs);
        RefreshEnemyGroup(groupB, groupBIcons, groupBNames, groupBDescs);

        // ── 基础奖励 ──
        if (baseGoldText != null) baseGoldText.text = $"{levelData.baseGoldReward}";
        if (baseExpText  != null) baseExpText.text  = $"{levelData.baseExpReward}";

        // ── A / B 组额外奖励（实例化预制体）──
        SpawnRewardItem(groupARewardParent, rewardA);
        SpawnRewardItem(groupBRewardParent, rewardB);

        // ── 返回休息按钮：第一关置灰 ──
        if (returnToRestBtn != null)
            returnToRestBtn.interactable = GameManager.Instance.currentMainLevelIndex > 0;
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void RefreshEnemyGroup(List<RoleData> enemies,
                                   Image[] icons, Text[] names, Text[] descs)
    {
        for (int i = 0; i < 3; i++)
        {
            bool hasEnemy = enemies != null && i < enemies.Count && enemies[i] != null;

            if (icons != null && icons.Length > i && icons[i] != null)
            {
                icons[i].gameObject.SetActive(hasEnemy);
                if (hasEnemy) icons[i].sprite = enemies[i].roleModel;
            }
            if (names != null && names.Length > i && names[i] != null)
            {
                names[i].gameObject.SetActive(hasEnemy);
                if (hasEnemy) names[i].text = enemies[i].roleName;
            }
            if (descs != null && descs.Length > i && descs[i] != null)
            {
                descs[i].gameObject.SetActive(hasEnemy);
                if (hasEnemy) descs[i].text = enemies[i].roleDescription;
            }
        }
    }

    /// <summary>
    /// 清空父节点下的旧预制体，然后实例化一个新的奖励 Item。
    /// </summary>
    private void SpawnRewardItem(Transform parent, LevelExtraRewardEntry entry)
    {
        if (parent == null || rewardItemPrefab == null) return;

        // 清理旧内容
        foreach (Transform child in parent)
            Destroy(child.gameObject);

        if (entry == null) return;

        // 实例化并初始化
        GameObject go = Instantiate(rewardItemPrefab, parent);
        LevelRewardItemUI ui = go.GetComponent<LevelRewardItemUI>();
        if (ui != null)
            ui.Setup(entry);
    }

    private void OnStartBattleClicked(bool isGroupA)
    {
        GameManager.Instance.SelectGroupAndStartCombat(isGroupA);
    }

    private void OnReturnToRestClicked()
    {
        gameObject.SetActive(false);
        GameManager.Instance.EnterRestFromLevelUI();
    }
}