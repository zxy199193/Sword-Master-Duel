using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RestTransitionUI : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup maskCanvasGroup;
    public Text descriptionText;

    [Header("Transition Settings")]
    public float fadeInDuration = 0.6f;
    public float displayDuration = 2.0f;
    public float fadeOutDuration = 0.6f;

    private bool isPlaying = false;

    private void Awake()
    {
        if (maskCanvasGroup != null)
        {
            maskCanvasGroup.alpha = 0f;
            maskCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 播放过渡动画
    /// </summary>
    /// <param name="text">显示的文本描述</param>
    /// <param name="onMaskFull">遮罩完全显示时的回调（通常用于执行数据变更）</param>
    /// <param name="onComplete">整个动画结束后的回调</param>
    public void ShowTransition(string text, System.Action onMaskFull = null, System.Action onComplete = null)
    {
        if (isPlaying) return;

        // 确保脚本所在物体是激活的，否则协程无法启动
        gameObject.SetActive(true);
        StartCoroutine(TransitionRoutine(text, onMaskFull, onComplete));
    }

    /// <summary>
    /// 强制清除遮罩状态，防止因物体禁用导致的卡死
    /// </summary>
    public void ForceClear()
    {
        StopAllCoroutines();
        if (maskCanvasGroup != null)
        {
            maskCanvasGroup.alpha = 0f;
            maskCanvasGroup.blocksRaycasts = false;
        }
        if (descriptionText != null) descriptionText.text = "";
        isPlaying = false;
    }

    private IEnumerator TransitionRoutine(string text, System.Action onMaskFull, System.Action onComplete)
    {
        isPlaying = true;
        if (descriptionText != null) descriptionText.text = "";
        
        if (maskCanvasGroup != null)
        {
            maskCanvasGroup.blocksRaycasts = true;
            maskCanvasGroup.alpha = 0f;

            // 1. 遮罩淡入
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                maskCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            maskCanvasGroup.alpha = 1f;
        }
        else
        {
            yield return new WaitForSeconds(fadeInDuration);
        }

        // 遮罩完全显现，执行逻辑回调
        onMaskFull?.Invoke();

        // 2. 显示文本
        if (descriptionText != null) 
        {
            descriptionText.text = text;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayRestActionSound();
            }
        }
        yield return new WaitForSeconds(displayDuration);

        // 3. 隐藏文本，遮罩淡出
        if (descriptionText != null) descriptionText.text = "";
        if (maskCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                maskCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeOutDuration));
                yield return null;
            }
            maskCanvasGroup.alpha = 0f;
            maskCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            yield return new WaitForSeconds(fadeOutDuration);
        }

        isPlaying = false;
        onComplete?.Invoke();
    }
}
