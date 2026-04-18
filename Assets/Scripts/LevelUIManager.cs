using UnityEngine;
using UnityEngine.UI;

public class LevelUIManager : MonoBehaviour
{
    [Header("UI 文本")]
    public Text levelTitleText; // 显示关卡标题，如 "关卡 1-1"

    [Header("敌人头像展示 (必须配置3个)")]
    public Image[] enemyIcons = new Image[3];

    [Header("状态视觉反馈 (可选，长度必须为3)")]
    [Tooltip("敌人被击败后覆盖在上面的半透明黑底或红叉")]
    public GameObject[] defeatedOverlays = new GameObject[3];
    [Tooltip("当前即将要面对的敌人的高亮框或指示箭头")]
    public GameObject[] currentNodeIndicators = new GameObject[3];

    [Header("UI 引用 - 角色面板")]
    public Button openRolePanelBtn;      // 打开角色面板的入口按钮
    public RoleUIManager roleUIManager;  // 角色面板组件实例

    [Header("操作按钮")]
    public Button startBattleBtn;

    private void Start()
    {
        // 绑定开始战斗按钮
        startBattleBtn.onClick.AddListener(OnStartBattleClicked);
        if (openRolePanelBtn != null && roleUIManager != null)
        {
            openRolePanelBtn.onClick.AddListener(() =>
            {
                roleUIManager.ShowPanel();
            });
        }
    }

    /// <summary>
    /// 由 GameManager 调用：根据当前进度更新 UI 显示
    /// </summary>
    public void UpdateAndShow(LevelData levelData, int nodeIndex)
    {
        gameObject.SetActive(true);

        // 1. 更新关卡标题
        if (levelTitleText != null)
        {
            levelTitleText.text = $"关卡 {levelData.levelTitle}";
        }

        // 2. 遍历更新 3 个敌人的状态
        for (int i = 0; i < 3; i++)
        {
            // 防呆：如果策划没配置满 3 个敌人，直接隐藏
            if (i >= levelData.enemies.Count || levelData.enemies[i] == null)
            {
                enemyIcons[i].gameObject.SetActive(false);
                continue;
            }

            // 读取敌人模型/头像
            enemyIcons[i].sprite = levelData.enemies[i].roleModel;
            enemyIcons[i].gameObject.SetActive(true);

            // ==========================================
            // 进度视觉逻辑
            // ==========================================

            // 如果索引 < 当前进度，说明已经打过了 (显示击败遮罩)
            if (defeatedOverlays.Length > i && defeatedOverlays[i] != null)
            {
                defeatedOverlays[i].SetActive(i < nodeIndex);
            }

            // 如果索引 == 当前进度，说明是马上要打的 (显示高亮指示器)
            if (currentNodeIndicators.Length > i && currentNodeIndicators[i] != null)
            {
                currentNodeIndicators[i].SetActive(i == nodeIndex);
            }
        }
    }

    private void OnStartBattleClicked()
    {
        // 1. 隐藏关卡 UI
        gameObject.SetActive(false);

        // 2. 通知全局管家：切入战斗场景！
        GameManager.Instance.StartCombatNode();
    }
}