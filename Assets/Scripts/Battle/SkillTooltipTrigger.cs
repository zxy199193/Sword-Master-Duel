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
            // 获取UI元素在屏幕上的世界坐标，传给浮窗管理器
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            // 取右边缘中间的位置
            Vector3 rightCenter = (corners[2] + corners[3]) / 2f;
            
            StatusTooltipManager.Instance.ShowTooltip(statuses, rightCenter);
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
