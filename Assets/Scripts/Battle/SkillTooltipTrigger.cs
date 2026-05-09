using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 挂载在技能/装备/道具条目上的特定触发区域（如状态图标），点击时显示状态浮窗。
/// 再次点击同一触发器、或点击屏幕其他区域均可关闭浮窗。
/// </summary>
public class SkillTooltipTrigger : MonoBehaviour, IPointerClickHandler
{
    private SkillData boundSkillData;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 绑定技能数据，UI 更新时调用。
    /// </summary>
    public void BindSkill(SkillData skill)
    {
        boundSkillData = skill;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundSkillData == null) return;
        if (StatusTooltipManager.Instance == null) return;

        List<StatusType> statuses = boundSkillData.GetRelatedStatuses();
        if (statuses == null || statuses.Count == 0) return;

        // 若浮窗已由本触发器打开，则关闭（切换）；否则打开
        if (StatusTooltipManager.Instance.IsShowingForTrigger(this))
        {
            StatusTooltipManager.Instance.HideTooltip();
        }
        else
        {
            // 取元素上边缘中心作为浮窗锚点
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 topCenter = (corners[1] + corners[2]) / 2f;
            StatusTooltipManager.Instance.ShowTooltip(statuses, topCenter, this);
        }
    }

    private void OnDisable()
    {
        // 组件失效时，若浮窗由本触发器打开，关闭它
        if (StatusTooltipManager.Instance != null)
            StatusTooltipManager.Instance.HideTooltipIfFrom(this);
    }
}
