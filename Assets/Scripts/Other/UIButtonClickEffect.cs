using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonClickEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("缩放动画")]
    public float scaleFactor = 0.95f;       // 按下时缩放大小
    public float scaleSpeed = 20f;         // 缩放速度

    [Header("音效设置")]
    public bool enableSound = true;        // 是否播放声音
    [Tooltip("留空则使用 AudioManager 的默认音效")]
    public AudioClip customSound;          // 可选的自定义音效

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * scaleFactor;
        PlayButtonSound();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    private void PlayButtonSound()
    {
        if (!enableSound) return;
        if (AudioManager.Instance == null) return;

        if (customSound != null)
        {
            AudioManager.Instance.PlaySFX(customSound);
        }
        else
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }
}