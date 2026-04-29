using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 角色信息面板UI：负责血量、护盾与体力的数值显示
/// 体力改为图标（圆点）显示，支持三种状态：可用 / 待用 / 空槽
/// </summary>
public class RoleInfoUI : MonoBehaviour
{
    [Header("UI References - Info")]
    public Text roleNameText;

    [Header("UI References - Basic HP")]
    public Slider basicHpSlider;
    public Text basicHpText;

    [Header("UI References - Extra HP (Shield)")]
    public Slider extraHpSlider;
    public Text extraHpText;

    [Header("UI References - Stamina Icons")]
    [Tooltip("体力图标的父容器（挂 HorizontalLayoutGroup）")]
    public Transform staminaIconContainer;

    [Tooltip("体力图标 Prefab（需含 StaminaIconUI 组件）")]
    public GameObject staminaIconPrefab;

    [Tooltip("敌方体力条从右到左排列（勾选）；玩家从左到右（不勾）")]
    public bool reverseOrder = false;

    [Header("Layout Settings")]
    [Tooltip("1点生命值对应的像素长度。比如填1，则100HP对应100宽")]
    public float hpToWidthMultiplier = 1f;

    [Header("Status System")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    public StatusDatabase statusDatabase;

    // ==========================================
    // 私有状态
    // ==========================================
    private BattleEntity targetEntity;

    /// <summary>
    /// 当前"待用体力"点数（已选技能但未确认消耗）
    /// </summary>
    private int pendingStaminaCost = 0;

    /// <summary>
    /// 图标对象池（复用，避免频繁 Instantiate/Destroy）
    /// </summary>
    private readonly List<StaminaIconUI> iconPool = new List<StaminaIconUI>();

    // ==========================================
    // 公共接口
    // ==========================================

    public void BindEntity(BattleEntity entity)
    {
        targetEntity = entity;
        pendingStaminaCost = 0;

        if (roleNameText != null) roleNameText.text = entity.roleData.roleName;

        // 初始化基础HP数值
        basicHpSlider.maxValue = entity.GetFinalMaxLife();

        // 订阅事件
        entity.OnHpChanged      += UpdateHpUI;
        entity.OnStaminaChanged += UpdateStaminaUI;
        entity.OnStatusChanged  += UpdateStatusUI;

        // 初始刷新
        UpdateHpUI();
        UpdateStaminaUI();
        UpdateStatusUI();
    }

    /// <summary>
    /// 设置待用体力点数（选中技能后预览消耗），传 0 表示清除
    /// </summary>
    public void SetPendingStamina(int cost)
    {
        pendingStaminaCost = Mathf.Max(0, cost);
        UpdateStaminaUI();
    }

    // ==========================================
    // 核心 UI 同步逻辑
    // ==========================================

    private void UpdateHpUI()
    {
        // 1. 处理基础血条 (Basic HP)
        int finalMaxHp = targetEntity.GetFinalMaxLife();
        basicHpSlider.maxValue = finalMaxHp;
        float basicWidth = finalMaxHp * hpToWidthMultiplier;
        SetRectWidth(basicHpSlider.GetComponent<RectTransform>(), basicWidth);

        basicHpSlider.value = targetEntity.currentBasicLife;
        if (basicHpText != null) basicHpText.text = targetEntity.currentBasicLife.ToString();

        // 2. 处理额外血条 (Extra HP / Shield)
        if (targetEntity.currentExtraLife > 0)
        {
            extraHpSlider.gameObject.SetActive(true);

            float extraWidth = targetEntity.currentExtraLife * hpToWidthMultiplier;
            SetRectWidth(extraHpSlider.GetComponent<RectTransform>(), extraWidth);

            extraHpSlider.maxValue = targetEntity.currentExtraLife;
            extraHpSlider.value    = targetEntity.currentExtraLife;

            if (extraHpText != null) extraHpText.text = targetEntity.currentExtraLife.ToString();
        }
        else
        {
            extraHpSlider.gameObject.SetActive(false);
        }

        // ==========================================
        // 强制刷新父节点的布局排版，消除缝隙
        // ==========================================
        if (basicHpSlider.transform.parent != null)
        {
            RectTransform layoutGroupRect = basicHpSlider.transform.parent.GetComponent<RectTransform>();
            if (layoutGroupRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroupRect);
            }
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaIconContainer == null || staminaIconPrefab == null || targetEntity == null) return;

        int maxStamina     = targetEntity.GetFinalMaxStamina();
        int current        = targetEntity.currentStamina;

        // 待用体力最多只能抵扣当前剩余体力（不会显示负数）
        int actualPending  = Mathf.Min(pendingStaminaCost, current);
        int actualAvailable = current - actualPending;     // 扣除待用后剩余的可用
        int actualEmpty    = maxStamina - current;         // 真实空槽（当前体力外的空位）

        // 确保图标池数量与 maxStamina 相符
        EnsureIconPool(maxStamina);

        // 按逻辑顺序构建状态列表
        // 逻辑顺序（从"满"到"空"）：可用 → 待用 → 空槽
        var states = new List<StaminaIconState>(maxStamina);
        for (int i = 0; i < actualAvailable; i++) states.Add(StaminaIconState.Available);
        for (int i = 0; i < actualPending;   i++) states.Add(StaminaIconState.Pending);
        for (int i = 0; i < actualEmpty;     i++) states.Add(StaminaIconState.Empty);

        // 敌方：镜像排列（空槽在左，可用在右）
        if (reverseOrder) states.Reverse();

        for (int i = 0; i < iconPool.Count; i++)
        {
            iconPool[i].SetState(states[i]);
        }
    }

    // ==========================================
    // 图标对象池管理
    // ==========================================

    /// <summary>
    /// 确保池中恰好有 <paramref name="count"/> 个图标，多退少补
    /// </summary>
    private void EnsureIconPool(int count)
    {
        // 补充不足的图标
        while (iconPool.Count < count)
        {
            GameObject go = Instantiate(staminaIconPrefab, staminaIconContainer);
            StaminaIconUI icon = go.GetComponent<StaminaIconUI>();
            if (icon == null)
            {
                Debug.LogError("[RoleInfoUI] staminaIconPrefab 缺少 StaminaIconUI 组件！");
                Destroy(go);
                break;
            }
            iconPool.Add(icon);
        }

        // 移除多余的图标（比如体力上限降低时）
        while (iconPool.Count > count)
        {
            int last = iconPool.Count - 1;
            Destroy(iconPool[last].gameObject);
            iconPool.RemoveAt(last);
        }
    }

    // ==========================================
    // 状态图标逻辑
    // ==========================================

    private void UpdateStatusUI()
    {
        if (statusContainer == null || statusIconPrefab == null || statusDatabase == null) return;
        foreach (Transform child in statusContainer) { Destroy(child.gameObject); }

        foreach (var kvp in targetEntity.activeStatuses)
        {
            StatusData data = statusDatabase.GetStatus(kvp.Key);
            if (data == null) continue;

            GameObject go = Instantiate(statusIconPrefab, statusContainer);
            StatusIconUI iconUI = go.GetComponent<StatusIconUI>();
            if (iconUI != null) iconUI.Setup(data, kvp.Value);
        }
    }

    // ==========================================
    // 内部工具逻辑
    // ==========================================

    /// <summary>
    /// 安全修改 RectTransform 的宽度，保持高度和中心点不变
    /// </summary>
    private void SetRectWidth(RectTransform rt, float width)
    {
        if (rt == null) return;
        Vector2 size = rt.sizeDelta;
        size.x = width;
        rt.sizeDelta = size;
    }

    private void OnDestroy()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHpChanged      -= UpdateHpUI;
            targetEntity.OnStaminaChanged -= UpdateStaminaUI;
            targetEntity.OnStatusChanged  -= UpdateStatusUI;
        }
    }
}