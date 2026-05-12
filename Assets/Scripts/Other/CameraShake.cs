using System.Collections;
using UnityEngine;

/// <summary>
/// 屏幕震动效果：挂在 Main Camera 上，通过静态方法触发
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Default Settings")]
    public float defaultDuration = 0.15f;
    public float defaultIntensity = 5f;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        Instance = this;
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// 触发一次屏幕震动
    /// </summary>
    /// <param name="duration">震动持续时间（秒）</param>
    /// <param name="intensity">震动强度（像素偏移量）</param>
    public void Shake(float duration = -1f, float intensity = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        if (intensity < 0) intensity = defaultIntensity;

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, intensity));
    }

    private IEnumerator ShakeRoutine(float duration, float intensity)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 随时间线性衰减震动强度
            float currentIntensity = intensity * (1f - elapsed / duration);
            float offsetX = Random.Range(-currentIntensity, currentIntensity);
            float offsetY = Random.Range(-currentIntensity, currentIntensity);
            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
}
