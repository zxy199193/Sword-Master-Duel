using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 状态图标 UI 组件，负责单个状态 (Buff/Debuff) 的视觉渲染
/// </summary>
public class StatusIconUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Text durationText;

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    /// <summary>
    /// 接收状态数据并刷新表现层
    /// </summary>
    /// <param name="iconSprite">状态的视觉图标</param>
    /// <param name="duration">该状态的剩余回合数</param>
    public void Setup(Sprite iconSprite, int duration)
    {
        // 防御性编程：确保 Image 组件和传入的 Sprite 都不为空
        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
        }

        // 防御性编程：确保 Text 组件未丢失
        if (durationText != null)
        {
            durationText.text = duration.ToString();
        }
    }
}