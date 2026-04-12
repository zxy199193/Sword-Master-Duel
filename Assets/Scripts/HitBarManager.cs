using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 打击条管理器：负责多段连击判定、滑块运动逻辑、UI渲染以及 AI 自动托管判定
/// </summary>
public class HitBarManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform baseBarRect;
    public RectTransform sliderRect;
    public Button slashButton;
    public Text countdownText;

    [Header("Hit Section Generator")]
    public GameObject sectionPrefab;  // 命中区间色块预制体
    public Transform sectionsRoot;    // 挂载色块的容器节点

    // ==========================================
    // 运行时状态 (Runtime State)
    // ==========================================
    private enum HitBarState { Idle, Moving, Stopping, Finished }
    private HitBarState currentState = HitBarState.Idle;

    private HitBarConfig currentConfig;
    private float currentSliderPos;
    private float currentSpeed;
    private float timeRemaining;
    private int moveDirection = 1;    // 1向右，-1向左

    // 连击与回调状态
    private int targetHitCount;
    private int currentHitCount;
    private List<HitSection?> accumulatedHits = new List<HitSection?>();
    private Action<List<HitSection?>, bool> onHitComplete;

    // AI 托管状态
    private bool isAIPlay = false;
    private float aiTargetPosition;

    // ==========================================
    // Unity 生命周期
    // ==========================================
    private void Awake()
    {
        if (slashButton != null)
        {
            slashButton.onClick.AddListener(OnSlashClicked);
        }
    }

    private void Update()
    {
        if (currentState == HitBarState.Idle || currentState == HitBarState.Finished) return;

        // 1. 全局倒计时逻辑
        timeRemaining -= Time.deltaTime;
        if (countdownText != null) countdownText.text = timeRemaining.ToString("F1") + "s";

        if (timeRemaining <= 0)
        {
            FinishHit(true); // 超时强制结算
            return;
        }

        // 2. AI 托管逻辑 (自动触发点击)
        if (currentState == HitBarState.Moving && isAIPlay)
        {
            ProcessAILogic();
        }

        // 3. 滑块运动逻辑
        if (currentState == HitBarState.Moving || currentState == HitBarState.Stopping)
        {
            currentSliderPos += currentSpeed * moveDirection * Time.deltaTime;

            // 触碰边缘反弹
            if (currentSliderPos >= 100f) { currentSliderPos = 100f; moveDirection = -1; }
            else if (currentSliderPos <= 0f) { currentSliderPos = 0f; moveDirection = 1; }
        }

        // 4. 减速停止逻辑
        if (currentState == HitBarState.Stopping)
        {
            currentSpeed = currentConfig.baseSlowdown > 0 ? currentSpeed - (currentConfig.baseSlowdown * Time.deltaTime) : 0;

            if (currentSpeed <= 0)
            {
                currentSpeed = 0;
                EvaluateResult();
            }
        }

        // 5. 更新表现层
        UpdateSliderUI();
    }

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    /// <summary>
    /// 启动打击条系统
    /// </summary>
    /// <param name="config">打击条配置数据</param>
    /// <param name="hitTimes">连击次数</param>
    /// <param name="onComplete">完成回调 (返回判定结果集与是否超时)</param>
    /// <param name="isAI">是否由 AI 接管</param>
    /// <param name="deviation">AI预判偏移量 (冗余参数暂留)</param>
    public void StartHitBar(HitBarConfig config, int hitTimes, Action<List<HitSection?>, bool> onComplete, bool isAI = false, Vector2 deviation = default)
    {
        currentConfig = config;
        onHitComplete = onComplete;
        isAIPlay = isAI;

        targetHitCount = hitTimes;
        currentHitCount = 0;
        accumulatedHits.Clear();

        CreateSectionsUI();
        ResetSliderForNextHit();

        timeRemaining = config.actionTime;
        gameObject.SetActive(true);
    }

    // ==========================================
    // 内部私有逻辑 (Core Logic)
    // ==========================================

    private void ResetSliderForNextHit()
    {
        currentSliderPos = UnityEngine.Random.Range(0f, 100f);
        moveDirection = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;
        currentSpeed = currentConfig.baseSpeed;
        currentState = HitBarState.Moving;

        if (slashButton != null) slashButton.interactable = true;

        // AI 每次连击重新锁定最高价值区间
        if (isAIPlay && currentConfig.sections != null && currentConfig.sections.Length > 0)
        {
            var bestSection = currentConfig.sections.OrderByDescending(s => s.level).FirstOrDefault();
            aiTargetPosition = bestSection.axisPosition;
        }
    }

    private void ProcessAILogic()
    {
        // 安全防除0处理计算刹车距离
        float stopDistance = currentConfig.baseSlowdown > 0 ? (currentSpeed * currentSpeed) / (2f * currentConfig.baseSlowdown) : 0;
        float predictedStopPos = currentSliderPos + (stopDistance * moveDirection);

        // 预测滑块停止位置，到达目标点时触发自动点击
        bool shouldClick = false;
        if (moveDirection == 1 && currentSliderPos < aiTargetPosition && predictedStopPos >= aiTargetPosition)
        {
            shouldClick = true;
        }
        else if (moveDirection == -1 && currentSliderPos > aiTargetPosition && predictedStopPos <= aiTargetPosition)
        {
            shouldClick = true;
        }

        if (shouldClick)
        {
            OnSlashClicked();
        }
    }

    private void OnSlashClicked()
    {
        if (currentState == HitBarState.Moving)
        {
            currentState = HitBarState.Stopping;
            if (slashButton != null) slashButton.interactable = false;
        }
    }

    private void EvaluateResult()
    {
        HitSection? bestHit = null;
        int highestLevel = -1;

        foreach (var section in currentConfig.sections)
        {
            float minBound = section.axisPosition - (section.width / 2f);
            float maxBound = section.axisPosition + (section.width / 2f);

            if (currentSliderPos >= minBound && currentSliderPos <= maxBound)
            {
                int levelValue = (int)section.level;
                if (levelValue > highestLevel)
                {
                    highestLevel = levelValue;
                    bestHit = section;
                }
            }
        }

        accumulatedHits.Add(bestHit);
        currentHitCount++;

        // 判定是否达到总段数
        if (currentHitCount >= targetHitCount)
        {
            FinishHit(false);
        }
        else
        {
            ResetSliderForNextHit();
        }
    }

    private void FinishHit(bool isTimeout)
    {
        currentState = HitBarState.Finished;
        gameObject.SetActive(false);
        if (slashButton != null) slashButton.interactable = true;

        onHitComplete?.Invoke(accumulatedHits, isTimeout);
    }

    // ==========================================
    // UI 渲染逻辑 (UI Rendering)
    // ==========================================

    private void UpdateSliderUI()
    {
        float width = baseBarRect.rect.width;
        float anchoredX = (currentSliderPos / 100f) * width - (width / 2f);
        sliderRect.anchoredPosition = new Vector2(anchoredX, sliderRect.anchoredPosition.y);
    }

    private void CreateSectionsUI()
    {
        foreach (Transform child in sectionsRoot)
        {
            Destroy(child.gameObject);
        }

        if (currentConfig.sections == null) return;

        float totalWidth = baseBarRect.rect.width;

        // 强制按等级排序渲染，确保高等级色块置于最上层
        var sortedSections = currentConfig.sections.OrderBy(s => s.level).ToList();

        foreach (var section in sortedSections)
        {
            GameObject go = Instantiate(sectionPrefab, sectionsRoot);
            RectTransform rt = go.GetComponent<RectTransform>();
            Image img = go.GetComponent<Image>();

            // 锚点设置：Y轴自适应拉伸
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float elementWidth = (section.width / 100f) * totalWidth;
            float anchoredX = (section.axisPosition / 100f) * totalWidth - (totalWidth / 2f);

            rt.sizeDelta = new Vector2(elementWidth, 0);
            rt.anchoredPosition = new Vector2(anchoredX, 0);

            img.color = GetSectionColor(section.level);
        }
    }

    private Color GetSectionColor(SectionLevel level)
    {
        switch (level)
        {
            case SectionLevel.Level1: return new Color(1f, 0.8f, 0.2f); // 黄
            case SectionLevel.Level2: return new Color(0.4f, 1f, 0.4f); // 绿
            case SectionLevel.Level3: return new Color(0.2f, 0.8f, 1f); // 蓝
            case SectionLevel.Level4: return new Color(1f, 0.4f, 0.4f); // 红
            case SectionLevel.Level5: return new Color(0.8f, 0.2f, 1f); // 紫
            case SectionLevel.Level6: return Color.white;               // 白
            case SectionLevel.Level0:
            case SectionLevel.Level99:
            default: return new Color(0.3f, 0.3f, 0.3f);                // 灰 (无效)
        }
    }
}