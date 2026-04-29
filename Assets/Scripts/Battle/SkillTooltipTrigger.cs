using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 挂载在技能按钮（如 SkillItemUI、ActionPanelButton 等）上，处理鼠标悬停事件
/// </summary>
public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SkillData boundSkillData;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 绑定技能数据，当 UI 更新技能时调用此方法
    /// </summary>
    public void BindSkill(SkillData skill)
    {
        boundSkillData = skill;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundSkillData == null) return;
        if (StatusTooltipManager.Instance == null) return;

        List<StatusType> statuses = boundSkillData.GetRelatedStatuses();
        if (statuses != null && statuses.Count > 0)
        {
            // 获取 UI 元素的四角世界坐标（0=左下, 1=左上, 2=右上, 3=右下）
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // 取上边缘中心点（左上角 + 右上角的中点），这样向上偏移后浮窗在按钮正上方
            Vector3 topCenter = (corners[1] + corners[2]) / 2f;

            StatusTooltipManager.Instance.ShowTooltip(statuses, topCenter);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (StatusTooltipManager.Instance != null)
        {
            StatusTooltipManager.Instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        if (StatusTooltipManager.Instance != null)
        {
            StatusTooltipManager.Instance.HideTooltip();
        }
    }
}
