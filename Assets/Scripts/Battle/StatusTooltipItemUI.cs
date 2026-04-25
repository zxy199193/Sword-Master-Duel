using UnityEngine;
using UnityEngine.UI;

public class StatusTooltipItemUI : MonoBehaviour
{
    public Image iconImg;
    public Text nameTxt;
    public Text descTxt;

    public void Setup(StatusData data)
    {
        if (data == null) return;

        if (iconImg != null) iconImg.sprite = data.icon;
        if (nameTxt != null) nameTxt.text = data.statusName;
        if (descTxt != null) descTxt.text = data.description;
    }
}
