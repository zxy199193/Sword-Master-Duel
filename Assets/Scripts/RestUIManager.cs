using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 休息室/营地 UI 管理器
/// 在每一大关（3个小节点）打完后弹出，提供整备功能
/// </summary>
public class RestUIManager : MonoBehaviour
{
    [Header("UI 按钮组件")]
    public Button restBtn;      // 休息（恢复生命）
    public Button trainBtn;     // 修炼（提升技能）
    public Button shopBtn;      // 购物（购买道具/装备）
    public Button continueBtn;  // 继续（进入下一大关）

    [Header("UI 引用 - 角色面板")]
    public Button openRolePanelBtn;      // 打开角色面板的入口按钮
    public RoleUIManager roleUIManager;  // 角色面板组件实例

    private void Start()
    {
        // 绑定按钮事件
        if (restBtn != null) restBtn.onClick.AddListener(OnRestClicked);
        if (trainBtn != null) trainBtn.onClick.AddListener(OnTrainClicked);
        if (shopBtn != null) shopBtn.onClick.AddListener(OnShopClicked);
        if (continueBtn != null) continueBtn.onClick.AddListener(OnContinueClicked);
        if (openRolePanelBtn != null && roleUIManager != null)
        {
            openRolePanelBtn.onClick.AddListener(() =>
            {
                roleUIManager.ShowPanel();
            });
        }
    }

    /// <summary>
    /// 由 GameManager 负责调用显示
    /// </summary>
    public void ShowPanel()
    {
        gameObject.SetActive(true);
        Debug.Log("<color=#00FF00>进入休息营地！请进行整备。</color>");

        // TODO: 可以在这里刷新一下当前金币的显示，或者判断金币不够时把购物按钮置灰等
    }

    // ==========================================
    // 按钮功能占位 (Placeholders)
    // ==========================================

    private void OnRestClicked()
    {
        Debug.Log("点击了【休息】 - 功能待开发 (计划：恢复一定比例生命值)");
        // TODO: 调用 GameManager.Instance.playerProfile.currentHp 增加逻辑
    }

    private void OnTrainClicked()
    {
        Debug.Log("点击了【修炼】 - 功能待开发 (计划：打开技能升级面板)");
    }

    private void OnShopClicked()
    {
        Debug.Log("点击了【购物】 - 功能待开发 (计划：打开商店面板扣除金币购买道具)");
    }

    // ==========================================
    // 核心推进逻辑
    // ==========================================

    private void OnContinueClicked()
    {
        Debug.Log("结束整备，继续前进！");

        // 1. 隐藏休息室界面
        gameObject.SetActive(false);

        // 2. 通知 GameManager 进入下一大关
        GameManager.Instance.AdvanceToNextMainLevel();
    }
}