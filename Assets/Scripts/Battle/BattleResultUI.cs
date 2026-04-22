using UnityEngine;
using UnityEngine.UI;

public class BattleResultUI : MonoBehaviour
{
    [Header("UI References")]
    public Text rewardText;
    public Button continueBtn;

    private void Start()
    {
        continueBtn.onClick.AddListener(OnContinueClicked);
    }

    /// <summary>
    /// 由 GameManager 调用，显示结算奖励
    /// </summary>
    public void ShowResult(int goldReward)
    {
        gameObject.SetActive(true);
        if (rewardText != null)
        {
            rewardText.text = $"战斗胜利！\n获得金币: {goldReward}";
        }
    }

    private void OnContinueClicked()
    {
        gameObject.SetActive(false);
        // 通知 GameManager 推进节点 (进下一场战斗或休息室)
        GameManager.Instance.AdvanceToNextNode();
    }
}