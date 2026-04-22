using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 状态图标 UI 组件，负责单个状态 (Buff/Debuff) 的视觉渲染
/// </summary>
public class StatusIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
    /// <param name="data">状态数据配置</param>
    /// <param name="duration">该状态的剩余回合数</param>
    public void Setup(StatusData data, int duration)
    {
        if (data == null) return;

        // 防御性编程：确保 Image 组件和传入的 Sprite 都不为空
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
        }

        // 防御性编程：确保 Text 组件未丢失
        if (durationText != null)
        {
            durationText.text = duration.ToString();
        }

        // 初始化浮窗数据
        if (tooltipNameText != null)
        {
            tooltipNameText.text = data.statusName;
        }

        if (tooltipDescText != null)
        {
            tooltipDescText.text = data.description;
        }

        if (tooltipNode != null)
        {
            tooltipNode.SetActive(false); // 默认隐藏
        }
    }

    // ==========================================
    // UI 交互事件
    // ==========================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipNode != null)
        {
            tooltipNode.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipNode != null)
        {
            tooltipNode.SetActive(false);
        }
    }
}