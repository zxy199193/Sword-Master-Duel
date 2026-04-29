using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float moveSpeed = 80f;   // 向上飘的速度（UI像素/秒）
    [SerializeField] private float destroyTime = 1.5f; // 存活/淡出时间（秒）

    private Text popupText;

    // ==========================================
    // Unity 生命周期
    // ==========================================
    private void Awake()
    {
        popupText = GetComponentInChildren<Text>();

        if (popupText == null)
        {
            Debug.LogWarning($"[DamagePopup] {gameObject.name} 及其子节点下未找到 Text 组件！");
        }
    }

    // ==========================================
    // 公共接口 (Public API)
    // ==========================================

    /// <summary>
    /// 初始化并启动飘字动画
    /// </summary>
    public void Setup(string textContent)
    {
        if (popupText != null)
        {
            popupText.text = textContent;
            StartCoroutine(AnimatePopup());
        }
    }

    // ==========================================
    // 内部协程动画逻辑
    // ==========================================
    private IEnumerator AnimatePopup()
    {
        float timer = 0;
        Color startColor = popupText.color;

        while (timer < destroyTime)
        {
            // 向上匀速移动
            transform.localPosition += Vector3.up * moveSpeed * Time.deltaTime;

            // 透明度渐隐淡出
            float alpha = Mathf.Lerp(1f, 0f, timer / destroyTime);
            popupText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}