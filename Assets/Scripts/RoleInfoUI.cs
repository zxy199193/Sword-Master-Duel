using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 角色战斗信息面板 UI，负责血量、护盾、体力及状态栏的表现层更新
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

    [Header("UI References - Stamina")]
    public Slider staminaSlider;
    public Text staminaText;

    [Header("UI References - Status System")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    public StatusDatabase statusDatabase;

    // ==========================================
    // 运行时状态 (Runtime State)
    // ==========================================
    private BattleEntity targetEntity;

    // ==========================================
    // Unity 生命周期 (Lifecycle)
    // ==========================================

    private void OnDestroy()
    {
        // 严谨注销事件监听，防止切换场景或销毁UI时引发内存泄漏
        if (targetEntity != null)
        {
            targetEntity.OnHpChanged -= UpdateHpUI;
            targetEntity.OnStaminaChanged -= UpdateStaminaUI;
            targetEntity.OnStatusChanged -= UpdateStatusUI;
        }
    }

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    /// <summary>
    /// 绑定战斗实体，并初始化面板极值与事件订阅
    /// </summary>
    public void BindEntity(BattleEntity entity)
    {
        targetEntity = entity;

        if (roleNameText != null) roleNameText.text = entity.roleData.roleName;

        // 初始化进度条上限
        basicHpSlider.maxValue = entity.roleData.maxBasicLife;
        staminaSlider.maxValue = entity.roleData.maxStamina;
        extraHpSlider.maxValue = Mathf.Max(entity.currentExtraLife, 1f); // 防除零保护

        // 核心解耦：订阅实体数值与状态变化事件
        entity.OnHpChanged += UpdateHpUI;
        entity.OnStaminaChanged += UpdateStaminaUI;
        entity.OnStatusChanged += UpdateStatusUI;

        // 初始画面强制刷新
        UpdateHpUI();
        UpdateStaminaUI();
        UpdateStatusUI();
    }

    // ==========================================
    // 内部私有逻辑 (Private Methods)
    // ==========================================

    private void UpdateHpUI()
    {
        basicHpSlider.value = targetEntity.currentBasicLife;
        if (basicHpText != null) basicHpText.text = targetEntity.currentBasicLife.ToString();

        extraHpSlider.value = targetEntity.currentExtraLife;
        if (extraHpText != null) extraHpText.text = targetEntity.currentExtraLife.ToString();

        // 护盾归零时的视觉隐藏逻辑
        if (extraHpSlider != null)
        {
            extraHpSlider.gameObject.SetActive(targetEntity.currentExtraLife > 0);
        }
    }

    private void UpdateStaminaUI()
    {
        staminaSlider.value = targetEntity.currentStamina;
        if (staminaText != null) staminaText.text = targetEntity.currentStamina.ToString();
    }

    private void UpdateStatusUI()
    {
        if (statusContainer == null || statusIconPrefab == null || statusDatabase == null) return;

        // 1. 清空旧图标
        foreach (Transform child in statusContainer)
        {
            Destroy(child.gameObject);
        }

        if (targetEntity.activeStatuses == null) return;

        // 2. 遍历并生成最新状态图标
        foreach (var kvp in targetEntity.activeStatuses)
        {
            StatusData data = statusDatabase.GetStatus(kvp.Key);
            if (data == null) continue;

            GameObject go = Instantiate(statusIconPrefab, statusContainer);
            StatusIconUI iconUI = go.GetComponent<StatusIconUI>();

            if (iconUI != null)
            {
                iconUI.Setup(data.icon, kvp.Value);
            }
        }
    }
}