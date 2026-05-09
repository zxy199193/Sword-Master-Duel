using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 通用点击切换组件：点击时显示/隐藏目标节点（替代旧版悬停触发的 UIHoverToggle）。
/// </summary>
public class UIClickToggle : MonoBehaviour, IPointerClickHandler
{
    [Header("点击切换显示的目标对象")]
    public GameObject targetNode;

    private void Awake()
    {
        if (targetNode != null)
            targetNode.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetNode != null)
            targetNode.SetActive(!targetNode.activeSelf);
    }

    private void OnDisable()
    {
        // 组件失效时强制隐藏，防止残留
        if (targetNode != null)
            targetNode.SetActive(false);
    }
}
