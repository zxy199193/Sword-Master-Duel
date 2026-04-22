using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class HitBarManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject backgroundMask;
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
    private int aiTargetBounces = 0;
    private int aiCurrentBounces = 0;

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
    private BattleEntity currentCaster;

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

        // ==========================================
        // 【核心修改】：每一帧实时刷新速度，让“眩晕柏林噪声”真正生效！
        // 必须只在 Moving 状态刷新，防止干扰 Stopping 阶段的物理刹车计算
        // ==========================================
        if (currentState == HitBarState.Moving)
        {
            currentSpeed = currentCaster != null ? currentCaster.GetFinalHitBarSpeed(GlobalBattleRules.globalHitBarBaseSpeed) : GlobalBattleRules.globalHitBarBaseSpeed;
        }

        if (currentState == HitBarState.Moving || currentState == HitBarState.Stopping)
        {
            currentSliderPos += currentSpeed * moveDirection * Time.deltaTime;

            if (currentSliderPos >= 100f)
            {
                currentSliderPos = 100f;
                moveDirection = -1;
                if (isAIPlay) aiCurrentBounces++;
            }
            else if (currentSliderPos <= 0f)
            {
                currentSliderPos = 0f;
                moveDirection = 1;
                if (isAIPlay) aiCurrentBounces++;
            }
        }

        if (currentState == HitBarState.Stopping)
        {
            float finalSlowdown = currentCaster != null ? currentCaster.GetFinalHitBarSlowdown(GlobalBattleRules.globalHitBarBaseSlowdown) : GlobalBattleRules.globalHitBarBaseSlowdown;

            currentSpeed = finalSlowdown > 0 ? currentSpeed - (finalSlowdown * Time.deltaTime) : 0;

            if (currentSpeed <= 0)
            {
                currentSpeed = 0;
                EvaluateResult();
            }
        }

        UpdateSliderUI();
    }

    public void StartHitBar(HitBarConfig config, int hitTimes, Action<List<HitSection?>, bool> onComplete,
                                    BattleEntity caster, SkillSlot skillSlot,
                                    bool isAI = false)
    {
        currentConfig = config;

        if (caster != null && caster.activeStatuses.ContainsKey(StatusType.Obscured))
        {
            var sectionList = currentConfig.sections != null ? currentConfig.sections.ToList() : new List<HitSection>();
            sectionList.Add(new HitSection
            {
                level = SectionLevel.Level99, // Level99 是我们的雷区专用判定
                axisPosition = UnityEngine.Random.Range(15f, 85f),
                width = 30f
            });
            currentConfig.sections = sectionList.ToArray();
        }

        onHitComplete = onComplete;
        isAIPlay = isAI;
        currentCaster = caster;

        targetHitCount = hitTimes;
        currentHitCount = 0;
        accumulatedHits.Clear();

        if (backgroundMask != null) backgroundMask.SetActive(true);
        if (titleText != null && caster != null) titleText.text = $"{caster.roleData.roleName} 的攻击";

        if (skillInfoUI != null && skillSlot != null && caster != null)
        {
            skillInfoUI.Init(skillSlot, caster, null);
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

        timeRemaining = currentCaster != null ? currentCaster.GetFinalActionTime(config.actionTime) : config.actionTime;
        gameObject.SetActive(true);
    }

    private void ResetSliderForNextHit()
    {
        currentSliderPos = UnityEngine.Random.Range(0f, 100f);
        moveDirection = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;

        currentSpeed = currentCaster != null ? currentCaster.GetFinalHitBarSpeed(GlobalBattleRules.globalHitBarBaseSpeed) : GlobalBattleRules.globalHitBarBaseSpeed;

        currentState = HitBarState.Moving;

        if (slashButton != null) slashButton.interactable = true;

        if (isAIPlay && currentConfig.sections != null && currentConfig.sections.Length > 0)
        {
            var bestSection = currentConfig.sections.OrderByDescending(s => s.level).FirstOrDefault();
            aiPerfectTarget = bestSection.axisPosition;

            float tolerance = 0.2f;
            if (currentCaster != null && currentCaster.roleData != null)
            {
                tolerance = currentCaster.roleData.aiReactionTolerance;
            }

            aiReactionTimeError = UnityEngine.Random.Range(-tolerance, tolerance);
            aiCurrentBounces = 0;
            aiTargetBounces = UnityEngine.Random.Range(1, 4);
        }
    }

    private void ProcessAILogic()
    {
        if (aiCurrentBounces < aiTargetBounces) return;

        float finalSlowdown = currentCaster != null ? currentCaster.GetFinalHitBarSlowdown(GlobalBattleRules.globalHitBarBaseSlowdown) : GlobalBattleRules.globalHitBarBaseSlowdown;

        float stopDistance = finalSlowdown > 0 ? (currentSpeed * currentSpeed) / (2f * finalSlowdown) : 0;
        float idealClickPos = aiPerfectTarget - (stopDistance * moveDirection) + (currentSpeed * aiReactionTimeError * moveDirection);

        if (idealClickPos < 0f || idealClickPos > 100f)
        {
            idealClickPos = aiPerfectTarget - (stopDistance * moveDirection) - (currentSpeed * aiReactionTimeError * moveDirection);
            idealClickPos = Mathf.Clamp(idealClickPos, 0f, 100f);
        }

        bool shouldClick = false;

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
        bool isObscuredHit = false; // 是否踩雷标志

        foreach (var section in currentConfig.sections)
        {
            float minBound = section.axisPosition - (section.width / 2f);
            float maxBound = section.axisPosition + (section.width / 2f);

            if (currentSliderPos >= minBound && currentSliderPos <= maxBound)
            {
                // 如果落入了沙子区，打上踩雷标记！
                if (section.level == SectionLevel.Level99)
                {
                    isObscuredHit = true;
                }
                else
                {
                    int levelValue = (int)section.level;
                    if (levelValue > highestLevel)
                    {
                        highestLevel = levelValue;
                        bestHit = section;
                    }
                }
            }
        }

        // 遮蔽区拥有绝对的“一票否决权”，就算你同时踩到了完美的 Level6 和沙子区，也会被判定为 Miss！
        if (isObscuredHit)
        {
            bestHit = null;
            Debug.Log("[HitBarManager] 滑块落入了沙子遮蔽区，强制判为 Miss！");
        }

        accumulatedHits.Add(bestHit);
        currentHitCount++;

        if (currentHitCount >= targetHitCount) FinishHit(false);
        else ResetSliderForNextHit();
    }

    private void FinishHit(bool isTimeout)
    {
        currentState = HitBarState.Finished;

        if (backgroundMask != null) backgroundMask.SetActive(false);
        gameObject.SetActive(false);

        if (slashButton != null) slashButton.interactable = true;

        onHitComplete?.Invoke(accumulatedHits, isTimeout);
    }

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