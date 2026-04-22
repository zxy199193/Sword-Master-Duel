using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 角色信息面板UI：负责血量、护盾与体力的数值显示，并动态同步血条的物理长度
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

    [Header("Layout Settings")]
    [Tooltip("1点生命值对应的像素长度。比如填1，则100HP对应100宽")]
    public float hpToWidthMultiplier = 1f;

    [Header("Status System")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    public StatusDatabase statusDatabase;

    private BattleEntity targetEntity;

    // ==========================================
    // 公共接口
    // ==========================================

    public void BindEntity(BattleEntity entity)
    {
        targetEntity = entity;

        if (roleNameText != null) roleNameText.text = entity.roleData.roleName;

        // 初始化基础数值
        basicHpSlider.maxValue = entity.roleData.maxBasicLife;
        staminaSlider.maxValue = entity.roleData.maxStamina;

        // 订阅事件
        entity.OnHpChanged += UpdateHpUI;
        entity.OnStaminaChanged += UpdateStaminaUI;
        entity.OnStatusChanged += UpdateStatusUI;

        // 初始刷新
        UpdateHpUI();
        UpdateStaminaUI();
        UpdateStatusUI();
    }

    // ==========================================
    // 核心 UI 同步逻辑
    // ==========================================

    private void UpdateHpUI()
    {
        // 1. 处理基础血条 (Basic HP)
        float basicWidth = targetEntity.roleData.maxBasicLife * hpToWidthMultiplier;
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
            extraHpSlider.value = targetEntity.currentExtraLife;

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
        staminaSlider.value = targetEntity.currentStamina;
        if (staminaText != null) staminaText.text = targetEntity.currentStamina.ToString();
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

    private void OnDestroy()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHpChanged -= UpdateHpUI;
            targetEntity.OnStaminaChanged -= UpdateStaminaUI;
            targetEntity.OnStatusChanged -= UpdateStatusUI;
        }
    }
}