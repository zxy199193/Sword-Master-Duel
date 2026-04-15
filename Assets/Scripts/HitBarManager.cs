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
    public GameObject backgroundMask;  // 【新增】：黑色半透明遮罩
    public RectTransform baseBarRect;
    public RectTransform sliderRect;
    public Button slashButton;
    public Text countdownText;

    public Text titleText;
    public SkillItemUI skillInfoUI;

    [Header("Hit Section Generator")]
    public GameObject sectionPrefab;
    public Transform sectionsRoot;

    private float aiPerfectTarget;
    private float aiReactionTimeError;
    private int aiTargetBounces = 0;  // 决定这次要看几次
    private int aiCurrentBounces = 0; // 当前已经看了几次

    // ==========================================
    // 运行时状态 (Runtime State)
    // ==========================================
    private enum HitBarState { Idle, Moving, Stopping, Finished }
    private HitBarState currentState = HitBarState.Idle;

    private HitBarConfig currentConfig;
    private float currentSliderPos;
    private float currentSpeed;
    private float timeRemaining;
    private int moveDirection = 1;

    private int targetHitCount;
    private int currentHitCount;
    private List<HitSection?> accumulatedHits = new List<HitSection?>();
    private Action<List<HitSection?>, bool> onHitComplete;

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

        timeRemaining -= Time.deltaTime;
        if (countdownText != null) countdownText.text = timeRemaining.ToString("F1") + "s";

        if (timeRemaining <= 0)
        {
            FinishHit(true);
            return;
        }

        if (currentState == HitBarState.Moving && isAIPlay)
        {
            ProcessAILogic();
        }

        if (currentState == HitBarState.Moving || currentState == HitBarState.Stopping)
        {
            currentSliderPos += currentSpeed * moveDirection * Time.deltaTime;

            if (currentSliderPos >= 100f)
            {
                currentSliderPos = 100f;
                moveDirection = -1;
                if (isAIPlay) aiCurrentBounces++; // 【新增】：撞到右边墙了，计数+1
            }
            else if (currentSliderPos <= 0f)
            {
                currentSliderPos = 0f;
                moveDirection = 1;
                if (isAIPlay) aiCurrentBounces++; // 【新增】：撞到左边墙了，计数+1
            }
        }

        if (currentState == HitBarState.Stopping)
        {
            currentSpeed = currentConfig.baseSlowdown > 0 ? currentSpeed - (currentConfig.baseSlowdown * Time.deltaTime) : 0;

            if (currentSpeed <= 0)
            {
                currentSpeed = 0;
                EvaluateResult();
            }
        }

        UpdateSliderUI();
    }

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    public void StartHitBar(HitBarConfig config, int hitTimes, Action<List<HitSection?>, bool> onComplete,
                            BattleEntity caster, SkillData skill,
                            bool isAI = false, Vector2 deviation = default)
    {
        currentConfig = config;
        onHitComplete = onComplete;
        isAIPlay = isAI;

        targetHitCount = hitTimes;
        currentHitCount = 0;
        accumulatedHits.Clear();

        // ------------------------------------------
        // 【新增】：显示半透明遮罩
        // ------------------------------------------
        if (backgroundMask != null) backgroundMask.SetActive(true);

        if (titleText != null && caster != null)
        {
            titleText.text = $"{caster.roleData.roleName} 的攻击";
        }

        if (skillInfoUI != null && skill != null && caster != null)
        {
            skillInfoUI.Init(skill, caster, null);
            if (skillInfoUI.miniHitBarRoot != null)
            {
                skillInfoUI.miniHitBarRoot.SetActive(false);
            }
        }

        if (slashButton != null)
        {
            slashButton.gameObject.SetActive(!isAIPlay);
        }

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

        if (isAIPlay && currentConfig.sections != null && currentConfig.sections.Length > 0)
        {
            var bestSection = currentConfig.sections.OrderByDescending(s => s.level).FirstOrDefault();
            aiPerfectTarget = bestSection.axisPosition;

            float aiDifficultyTolerance = 0.2f;
            aiReactionTimeError = UnityEngine.Random.Range(-aiDifficultyTolerance, aiDifficultyTolerance);

            // 【新增】：重置计数器，并随机决定观察 1~3 次（撞墙折返次数）
            aiCurrentBounces = 0;
            aiTargetBounces = UnityEngine.Random.Range(1, 4); // 1, 2, 或者 3

            Debug.Log($"<color=#FF8C00>[AI 决策] 完美点:{aiPerfectTarget}, 误差:{aiReactionTimeError:F3}秒, 打算观察 {aiTargetBounces} 次再出手</color>");
        }
    }

    private void ProcessAILogic()
    {
        // 还没看够折返次数，按兵不动
        if (aiCurrentBounces < aiTargetBounces) return;

        // 1. 算出物理刹车距离
        float stopDistance = currentConfig.baseSlowdown > 0 ? (currentSpeed * currentSpeed) / (2f * currentConfig.baseSlowdown) : 0;

        // 2. 计算理想按键位置 (完美中心点 - 刹车提前量 + 误差偏移)
        float idealClickPos = aiPerfectTarget - (stopDistance * moveDirection) + (currentSpeed * aiReactionTimeError * moveDirection);

        // 3. 【核心修复】：物理学防作弊检查！
        // 如果想按的位置在墙外（比如 -45），说明在这个方向上根本不可能“提早按”
        if (idealClickPos < 0f || idealClickPos > 100f)
        {
            // 既然不能提早按，那就强行变成“晚按”（反转误差符号）！
            // 绝对不能让它卡在边缘 0 处按，否则又会刚好滑到正中心！
            idealClickPos = aiPerfectTarget - (stopDistance * moveDirection) - (currentSpeed * aiReactionTimeError * moveDirection);

            // 钳制在屏幕内，防止二次越界
            idealClickPos = Mathf.Clamp(idealClickPos, 0f, 100f);
        }

        bool shouldClick = false;

        // 4. 严格判定：只有当滑块真正“跨过”理想按键点时才触发
        if (moveDirection == 1 && currentSliderPos >= idealClickPos) shouldClick = true;
        if (moveDirection == -1 && currentSliderPos <= idealClickPos) shouldClick = true;

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

        // ------------------------------------------
        // 【新增】：判定结束时，隐藏遮罩和自身
        // ------------------------------------------
        if (backgroundMask != null) backgroundMask.SetActive(false);
        gameObject.SetActive(false);

        if (slashButton != null) slashButton.interactable = true;

        onHitComplete?.Invoke(accumulatedHits, isTimeout);
    }

    // ==========================================
    // UI 渲染逻辑 (UI Rendering)
    // ==========================================

    private void UpdateSliderUI()
    {
        if (sliderRect == null || baseBarRect == null) return;

        sliderRect.anchorMin = new Vector2(0.5f, sliderRect.anchorMin.y);
        sliderRect.anchorMax = new Vector2(0.5f, sliderRect.anchorMax.y);

        float width = baseBarRect.rect.width;
        float anchoredX = (currentSliderPos / 100f) * width - (width / 2f);

        anchoredX = Mathf.Clamp(anchoredX, -width / 2f, width / 2f);

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
        var sortedSections = currentConfig.sections.OrderBy(s => s.level).ToList();

        foreach (var section in sortedSections)
        {
            GameObject go = Instantiate(sectionPrefab, sectionsRoot);
            RectTransform rt = go.GetComponent<RectTransform>();
            Image img = go.GetComponent<Image>();

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
            case SectionLevel.Level1: return new Color(1f, 0.8f, 0.2f);
            case SectionLevel.Level2: return new Color(0.4f, 1f, 0.4f);
            case SectionLevel.Level3: return new Color(0.2f, 0.8f, 1f);
            case SectionLevel.Level4: return new Color(1f, 0.4f, 0.4f);
            case SectionLevel.Level5: return new Color(0.8f, 0.2f, 1f);
            case SectionLevel.Level6: return Color.white;
            case SectionLevel.Level0:
            case SectionLevel.Level99:
            default: return new Color(0.3f, 0.3f, 0.3f);
        }
    }
}