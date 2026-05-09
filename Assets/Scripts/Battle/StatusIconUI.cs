using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 状态图标 UI 组件，负责单个状态 (Buff/Debuff) 的视觉渲染。
/// 手机端：点击图标显示浮窗，点击屏幕任意其他位置关闭浮窗。
/// 关闭检测由 StatusTooltipManager.Update() 统一处理。
/// </summary>
public class StatusIconUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image iconImage;
    public Text durationText;

    [Header("Tooltip (Floating Window)")]
    public GameObject tooltipNode;
    public Text tooltipNameText;
    public Text tooltipDescText;

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    /// <summary>
    /// 接收状态数据并刷新表现层
    /// </summary>
    public void Setup(StatusData data, int duration)
    {
        if (data == null) return;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (durationText != null)
            durationText.text = duration.ToString();

        if (tooltipNameText != null)
            tooltipNameText.text = data.statusName;

        if (tooltipDescText != null)
            tooltipDescText.text = data.description;

        if (tooltipNode != null)
            tooltipNode.SetActive(false); // 默认隐藏
    }

    // ==========================================
    // UI 交互事件
    // ==========================================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tooltipNode == null) return;

        bool isOpen = tooltipNode.activeSelf;

        // 先关闭所有其他 StatusIconUI 浮窗（同屏只显示一个）
        if (StatusTooltipManager.Instance != null)
            StatusTooltipManager.Instance.CloseAllStatusIconTooltips();

        // 切换：若原来是关闭状态则打开，否则保持关闭（已被上面关闭）
        if (!isOpen)
        {
            tooltipNode.SetActive(true);
            // 注册到管理器，使 Update() 检测到屏幕点击时能关闭它
            if (StatusTooltipManager.Instance != null)
                StatusTooltipManager.Instance.RegisterOpenStatusIcon(this);
        }
    }

    /// <summary>
    /// 由 StatusTooltipManager 在检测到屏幕点击时调用，强制关闭浮窗。
    /// </summary>
    public void ForceCloseTooltip()
    {
        if (tooltipNode != null)
            tooltipNode.SetActive(false);
    }

    private void OnDisable()
    {
        ForceCloseTooltip();
        if (StatusTooltipManager.Instance != null)
            StatusTooltipManager.Instance.UnregisterStatusIcon(this);
    }
}