using UnityEngine;
using UnityEngine.UI;
using System;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI 节点 - 展示")]
    public Image iconImg;
    public Text nameText;
    public Text descText;
    public Text priceText;

    [Header("UI 节点 - 操作")]
    public Button actionBtn;
    public Text actionBtnText;

    // ==========================================
    // Public Methods
    // ==========================================

    public void Setup(Sprite icon, string name, string desc, int price, string btnText, bool canAfford, Action onClick)
    {
        if (iconImg) iconImg.sprite = icon;
        if (nameText) nameText.text = name;
        if (descText) descText.text = desc;

        if (priceText)
        {
            priceText.text = price.ToString();
            priceText.color = canAfford ? Color.white : Color.red;
        }

        if (actionBtnText) actionBtnText.text = btnText;

        if (actionBtn)
        {
            actionBtn.onClick.RemoveAllListeners();
            actionBtn.onClick.AddListener(() => onClick?.Invoke());
            actionBtn.interactable = canAfford;
        }
    }
}