using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载在关卡额外奖励预制体上，由 LevelUIManager 在生成 AB 组奖励时调用 Setup() 填充数据。
/// 预制体需包含：图标(Image)、名称(Text)、数量(Text) 三个 UI 组件。
/// </summary>
public class LevelRewardItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image  iconImage;
    public Text   nameText;
    public Text   quantityText;

    public void Setup(LevelExtraRewardEntry entry, bool isGroupA = true, System.Action<LevelExtraRewardEntry, bool> onClick = null)
    {
        if (entry == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        Button btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick(entry, isGroupA));
        }

        if (iconImage    != null) iconImage.sprite = entry.GetIcon();
        if (nameText     != null) nameText.text    = entry.GetDisplayName();
        if (quantityText != null)
        {
            // 装备数量固定1就不显示×1；道具如果数量>1才显示
            quantityText.text = entry.quantity > 1 ? $"×{entry.quantity}" : "";
        }
    }
}
