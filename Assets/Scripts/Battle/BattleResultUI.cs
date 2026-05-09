using UnityEngine;
using UnityEngine.UI;

public class BattleResultUI : MonoBehaviour
{
    [Header("UI References")]
    public Button continueBtn;

    private void Start()
    {
        continueBtn.onClick.AddListener(OnContinueClicked);
    }

    /// <summary>
    /// 由 GameManager 调用，显示单场战斗胜利弹窗。
    /// </summary>
    public void ShowResult()
    {
        gameObject.SetActive(true);
    }

    private void OnContinueClicked()
    {
        gameObject.SetActive(false);
        // 推进到下一个战斗节点（或3战完成后由 AdvanceToNextNode → EndCurrentLevelGroup 结算）
        GameManager.Instance.AdvanceToNextNode();
    }
}