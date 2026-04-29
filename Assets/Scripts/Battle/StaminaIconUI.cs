using UnityEngine;

/// <summary>
/// 单个体力图标组件，通过切换三张子图来展示三种状态
/// </summary>
public class StaminaIconUI : MonoBehaviour
{
    [Header("State Images")]
    [Tooltip("可用体力图片（亮色实心）")]
    public GameObject availableIcon;

    [Tooltip("待用体力图片（半透明/预览色）")]
    public GameObject pendingIcon;

    [Tooltip("空体力图片（暗色空框）")]
    public GameObject emptyIcon;

    public void SetState(StaminaIconState state)
    {
        if (availableIcon != null) availableIcon.SetActive(state == StaminaIconState.Available);
        if (pendingIcon != null)   pendingIcon.SetActive(state == StaminaIconState.Pending);
        if (emptyIcon != null)     emptyIcon.SetActive(state == StaminaIconState.Empty);
    }
}

public enum StaminaIconState
{
    Available,  // 可用体力
    Pending,    // 待用体力（技能选定但未确认消耗）
    Empty       // 空体力槽
}
