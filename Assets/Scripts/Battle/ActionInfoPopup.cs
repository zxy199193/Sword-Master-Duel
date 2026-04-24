using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 行动信息气泡：在角色挂点处显示技能行动文字。
/// 展示 1 秒后在 0.5 秒内原地透明度渐变淡出，随后自动销毁。
/// </summary>
public class ActionInfoPopup : MonoBehaviour
{
    [SerializeField] private Text popupText;

    /// <summary>
    /// 设置显示文字并启动动画（支持 Rich Text 颜色标签）
    /// </summary>
    public void Setup(string textContent)
    {
        if (popupText != null) popupText.text = textContent;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        // 展示阶段：静止显示 1 秒
        yield return new WaitForSeconds(1f);

        // 淡出阶段：0.5 秒内透明度渐变至 0
        if (popupText != null)
        {
            float timer = 0f;
            Color startColor = popupText.color;
            while (timer < 0.5f)
            {
                float alpha = Mathf.Lerp(1f, 0f, timer / 0.5f);
                popupText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
