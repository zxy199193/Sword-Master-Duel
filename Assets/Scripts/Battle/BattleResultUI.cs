using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BattleResultUI : MonoBehaviour
{
    [Header("UI References - Base")]
    public Button continueBtn;

    [Header("UI References - EXP & Level")]
    public Text levelText;
    public Slider expSlider;
    public Image expFill;
    
    [Header("UI References - Rewards")]
    public Text expRewardText;
    public Text goldText;
    public GameObject extraRewardRoot;
    public Image extraRewardIcon;
    public Text extraRewardName;
    public Text extraRewardAmount;

    private void Start()
    {
        if (continueBtn) continueBtn.onClick.AddListener(OnContinueClicked);
    }

    /// <summary>
    /// 大关卡（3场战斗全胜）结算时调用
    /// </summary>
    public void ShowResult(int oldLevel, int oldExp, int gainedExp, int goldReward, LevelExtraRewardEntry extraReward)
    {
        gameObject.SetActive(true);
        if (continueBtn) continueBtn.interactable = false; // 动画播放期间禁用按钮

        // 设置金币与经验文本
        if (goldText) goldText.text = $"+{goldReward}";
        if (expRewardText) expRewardText.text = $"+{gainedExp}";

        // 设置额外奖励
        if (extraRewardRoot)
        {
            if (extraReward != null)
            {
                extraRewardRoot.SetActive(true);
                if (extraRewardIcon) extraRewardIcon.sprite = extraReward.GetIcon();
                if (extraRewardName) extraRewardName.text = extraReward.GetDisplayName();
                if (extraRewardAmount) extraRewardAmount.text = $"x{extraReward.quantity}";
            }
            else
            {
                extraRewardRoot.SetActive(false);
            }
        }

        // 启动经验条动画
        StartCoroutine(AnimateExpBar(oldLevel, oldExp, gainedExp));
    }

    private IEnumerator AnimateExpBar(int startLevel, int startExp, int totalGainedExp)
    {
        int currentLevel = startLevel;
        float currentExpSum = startExp;
        float targetExpSum = startExp + totalGainedExp;
        
        // 初始显示
        UpdateLevelText(currentLevel);
        UpdateExpBar(currentLevel, currentExpSum);

        float fillSpeed = 100f; // 每秒填充多少经验值

        while (currentExpSum < targetExpSum && currentLevel < 12)
        {
            float step = fillSpeed * Time.deltaTime;
            currentExpSum = Mathf.MoveTowards(currentExpSum, targetExpSum, step);

            // 检查升级 (这里用 99.99f 避免任何恶心的浮点误差，不过 MoveTowards 是精确的)
            if (currentExpSum >= 100f)
            {
                currentExpSum -= 100f;
                targetExpSum -= 100f;
                currentLevel++;
                
                // 播放升级音效
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelUpSound();

                UpdateExpBar(currentLevel, 100f); // 瞬间满一下
                UpdateLevelText(currentLevel);
                
                // 播放升级文字的缩放 Bounce 效果
                yield return StartCoroutine(PlayLevelUpBounce());
                
                if (currentLevel >= 12)
                {
                    currentExpSum = 0;
                    targetExpSum = 0;
                }
            }

            UpdateExpBar(currentLevel, currentExpSum);
            yield return null;
        }

        // 动画结束，确保最终数值精准
        if (currentLevel >= 12)
        {
            UpdateLevelText(12);
            UpdateExpBar(12, 0);
        }
        else
        {
            UpdateExpBar(currentLevel, currentExpSum);
        }

        if (continueBtn) continueBtn.interactable = true; // 动画结束，允许点击
    }

    private void UpdateLevelText(int lvl)
    {
        if (levelText)
        {
            levelText.text = lvl >= 12 ? $"{lvl} (Max)" : $"{lvl}";
        }
    }

    private void UpdateExpBar(int lvl, float exp)
    {
        float ratio = lvl >= 12 ? 1f : (exp / 100f);
        if (expSlider) expSlider.value = ratio;
        if (expFill) expFill.fillAmount = ratio;
    }

    private IEnumerator PlayLevelUpBounce()
    {
        if (levelText == null) yield break;
        
        Transform txtTr = levelText.transform;
        Vector3 originalScale = Vector3.one;
        
        // 放大
        float t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float progress = t / 0.15f;
            txtTr.localScale = Vector3.Lerp(originalScale, originalScale * 1.5f, progress);
            yield return null;
        }
        
        // 缩小
        t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float progress = t / 0.15f;
            txtTr.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, progress);
            yield return null;
        }
        
        txtTr.localScale = originalScale;
    }

    private void OnContinueClicked()
    {
        gameObject.SetActive(false);
        // 通知 GameManager 正式进入休息区（结算逻辑已经在 EndCurrentLevelGroup 里算完了）
        GameManager.Instance.EnterRestFromBattleResult();
    }
}