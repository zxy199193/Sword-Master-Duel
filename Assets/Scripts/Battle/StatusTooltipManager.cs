using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 全局状态浮窗管理器（单例）。
/// 手机端：点击触发区域打开，点击屏幕任意位置关闭（不拦截其他按钮，按钮照常响应）。
/// </summary>
public class StatusTooltipManager : MonoBehaviour
{
    public static StatusTooltipManager Instance { get; private set; }

    [Header("Data References")]
    public StatusDatabase statusDatabase;

    [Header("UI References")]
    public GameObject tooltipRoot;       // 整体浮窗节点
    public GameObject tooltipItemPrefab; // 单条状态预制体

    // ── 内部状态 ──────────────────────────────────────
    private SkillTooltipTrigger currentTrigger;  // 当前打开浮窗的触发器
    private bool canHideThisFrame = false;        // 防止打开瞬间被同帧关闭

    // 当前已打开内嵌浮窗的 StatusIconUI（同屏同时只允许1个）
    private StatusIconUI openStatusIcon;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

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

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // ── 关闭 SkillTooltipTrigger 打开的浮窗 ──
        if (tooltipRoot != null && tooltipRoot.activeSelf && canHideThisFrame)
        {
            if (!IsPointerOverTooltip())
                HideTooltip();
        }

        // ── 关闭 StatusIconUI 内嵌浮窗 ──
        if (openStatusIcon != null)
        {
            // 将屏幕点击位置进行射线检测：浮窗内点击则保留，外部则关闭
            if (!IsPointerOverObject(openStatusIcon.tooltipNode))
                CloseAllStatusIconTooltips();
        }
    }

    // ==========================================
    // Public API
    // ==========================================

    /// <summary>
    /// 显示包含指定状态列表的浮窗，锚定到 targetPosition。
    /// trigger 用于判断是否由同一触发器再次点击（切换逻辑）。
    /// </summary>
    public void ShowTooltip(List<StatusType> statuses, Vector3 targetPosition,
                            SkillTooltipTrigger trigger = null)
    {
        if (statuses == null || statuses.Count == 0) return;
        if (statusDatabase == null || tooltipRoot == null || tooltipItemPrefab == null) return;

        currentTrigger = trigger;
        canHideThisFrame = false;

        // 清空旧条目
        foreach (Transform child in tooltipRoot.transform)
            Destroy(child.gameObject);

        bool hasValidStatus = false;
        foreach (var type in statuses)
        {
            StatusData data = statusDatabase.GetStatus(type);
            if (data == null) continue;

            hasValidStatus = true;
            GameObject itemObj = Instantiate(tooltipItemPrefab, tooltipRoot.transform);
            StatusTooltipItemUI itemUI = itemObj.GetComponent<StatusTooltipItemUI>();
            if (itemUI != null)
                itemUI.Setup(data);
            else
                Debug.LogWarning("[StatusTooltipManager] TooltipItemPrefab 缺少 StatusTooltipItemUI 组件！");
        }

        if (hasValidStatus)
        {
            tooltipRoot.SetActive(true);
            tooltipRoot.transform.position = targetPosition + new Vector3(0, 150f, 0);
            // 等下一帧再允许关闭，避免打开的那次点击立即将浮窗关掉
            StartCoroutine(EnableHideNextFrame());
        }
    }

    /// <summary>
    /// 关闭浮窗。
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        currentTrigger = null;
        canHideThisFrame = false;
    }

    /// <summary>
    /// 浮窗当前是否由指定触发器打开。
    /// </summary>
    public bool IsShowingForTrigger(SkillTooltipTrigger trigger)
    {
        return tooltipRoot != null && tooltipRoot.activeSelf && currentTrigger == trigger;
    }

    /// <summary>
    /// 若浮窗由指定触发器打开，则关闭它（触发器 OnDisable 时调用）。
    /// </summary>
    public void HideTooltipIfFrom(SkillTooltipTrigger trigger)
    {
        if (currentTrigger == trigger)
            HideTooltip();
    }

    // ==========================================
    // StatusIconUI 内嵌浮窗管理
    // ==========================================

    /// <summary>
    /// StatusIconUI 点击打开内嵌浮窗时调用，让管理器知道当前有哪个浮窗打开。
    /// </summary>
    public void RegisterOpenStatusIcon(StatusIconUI icon)
    {
        openStatusIcon = icon;
    }

    /// <summary>
    /// StatusIconUI OnDisable 时调用，取消注册。
    /// </summary>
    public void UnregisterStatusIcon(StatusIconUI icon)
    {
        if (openStatusIcon == icon)
            openStatusIcon = null;
    }

    /// <summary>
    /// 关闭当前所有已打开的 StatusIconUI 浮窗。
    /// </summary>
    public void CloseAllStatusIconTooltips()
    {
        if (openStatusIcon != null)
        {
            openStatusIcon.ForceCloseTooltip();
            openStatusIcon = null;
        }
    }

    // ==========================================
    // Private Helpers
    // ==========================================

    private IEnumerator EnableHideNextFrame()
    {
        yield return null;
        canHideThisFrame = true;
    }

    /// <summary>
    /// 检测当前点击位置是否在浮窗节点内（用于防止误关）。
    /// </summary>
    private bool IsPointerOverTooltip()
    {
        return IsPointerOverObject(tooltipRoot);
    }

    /// <summary>
    /// 检测当前点击位置是否在指定 GameObject 内部。
    /// </summary>
    private bool IsPointerOverObject(GameObject target)
    {
        if (target == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            Transform t = result.gameObject.transform;
            if (t == target.transform || t.IsChildOf(target.transform))
                return true;
        }
        return false;
    }
}
