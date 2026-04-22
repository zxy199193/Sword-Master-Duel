using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 通用的 UI 悬停切换组件：鼠标移入显示目标节点，移出隐藏
/// </summary>
public class UIHoverToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("悬停显示的目标对象")]
    public GameObject targetNode;

    private void Awake()
    {
        // 初始状态确保隐藏
        if (targetNode != null)
        {
            targetNode.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetNode != null)
        {
            targetNode.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetNode != null)
        {
            targetNode.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 脚本失效或对象隐藏时，强制关闭提示，防止残留
        if (targetNode != null)
        {
            targetNode.SetActive(false);
        }
    }
}
