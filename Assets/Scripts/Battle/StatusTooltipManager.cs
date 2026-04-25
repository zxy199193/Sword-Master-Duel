using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 全局的状态浮窗管理器。配合预制体，单例化运行。
/// </summary>
public class StatusTooltipManager : MonoBehaviour
{
    public static StatusTooltipManager Instance { get; private set; }

    [Header("Data References")]
    public StatusDatabase statusDatabase; // 请在 Inspector 中挂载全局 StatusDatabase

    [Header("UI References")]
    public GameObject tooltipRoot; // 整体浮窗节点（通常挂一个 VerticalLayoutGroup）
    public GameObject tooltipItemPrefab; // 单条状态显示的预制体 (Image + Text + Text)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 显示指定技能包含的状态
    /// </summary>
    /// <param name="statuses">包含的状态列表</param>
    /// <param name="targetPosition">锚定位置（如按钮的右侧）</param>
    public void ShowTooltip(List<StatusType> statuses, Vector3 targetPosition)
    {
        if (statuses == null || statuses.Count == 0) return;
        if (statusDatabase == null || tooltipRoot == null || tooltipItemPrefab == null) return;

        // 清空现有条目
        foreach (Transform child in tooltipRoot.transform)
        {
            Destroy(child.gameObject);
        }

        bool hasValidStatus = false;

        foreach (var type in statuses)
        {
            StatusData data = statusDatabase.GetStatus(type);
            if (data != null)
            {
                hasValidStatus = true;
                GameObject itemObj = Instantiate(tooltipItemPrefab, tooltipRoot.transform);
                
                StatusTooltipItemUI itemUI = itemObj.GetComponent<StatusTooltipItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(data);
                }
                else
                {
                    Debug.LogWarning("TooltipItemPrefab 上没有找到 StatusTooltipItemUI 组件！请挂载它并关联好 UI 节点。");
                }
            }
        }

        if (hasValidStatus)
        {
            tooltipRoot.SetActive(true);
            // 简单的位置偏移：放在目标位置偏右一点的地方
            // 在实际使用中，如果遇到屏幕边缘，可以根据 Canvas 的 RectTransform 做边缘检测和修正
            tooltipRoot.transform.position = targetPosition + new Vector3(80f, 0, 0); 
        }
    }

    /// <summary>
    /// 隐藏浮窗
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }
}
