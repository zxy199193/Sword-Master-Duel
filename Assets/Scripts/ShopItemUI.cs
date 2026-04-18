using UnityEngine;
using UnityEngine.UI;
using System;

public class ShopItemUI : MonoBehaviour
{
    [Header("展示节点")]
    public Image iconImg;
    public Text nameText;
    public Text descText;
    public Text priceText;

    [Header("操作节点")]
    public Button actionBtn;
    public Text actionBtnText;

    /// <summary>
    /// 初始化商品项的数据
    /// </summary>
    public void Setup(Sprite icon, string name, string desc, int price, string btnText, bool canAfford, Action onClick)
    {
        if (iconImg) iconImg.sprite = icon;
        if (nameText) nameText.text = name;
        if (descText) descText.text = desc;

        if (priceText)
        {
            priceText.text = price.ToString();
            // 买不起时，价格变红提示
            priceText.color = canAfford ? Color.white : Color.red;
        }

        if (actionBtnText) actionBtnText.text = btnText;

        if (actionBtn)
        {
            actionBtn.onClick.RemoveAllListeners();
            actionBtn.onClick.AddListener(() => onClick?.Invoke());
            // 买不起时，按钮变灰不可点
            actionBtn.interactable = canAfford;
        }
    }
}