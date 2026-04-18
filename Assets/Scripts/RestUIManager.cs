using UnityEngine;
using UnityEngine.UI;

public class RestUIManager : MonoBehaviour
{
    [Header("商店货源配置")]
    public ShopConfig currentShopConfig;

    [Header("UI 引用 - 玩家状态展示")]
    public Text hpText;
    public Text goldText;
    public Text attrPointsText;

    [Header("UI 引用 - 商店/道场弹窗系统")]
    public ShopListUI shopListUI;

    [Header("UI 引用 - 角色系统 (新增)")]
    public Button openRolePanelBtn;      // 打开角色面板的按钮
    public RoleUIManager roleUIManager;  // 场景中的角色面板管理器

    [Header("UI 引用 - 直接功能按钮")]
    public Button sleepBtn;
    public Button massageBtn;
    public Button workoutBtn;
    public Button waterfallBtn;
    public Button gachaBtn;

    [Header("UI 引用 - 道场功能按钮")]
    public Button learnSkillBtn;
    public Button upgradeSkillBtn;
    public Button masterSkillBtn;

    [Header("UI 引用 - 商店功能按钮")]
    public Button buyEquipBtn;
    public Button buyItemBtn;

    private void Start()
    {
        RefreshPlayerStatusUI();

        // 绑定打开角色面板按钮
        if (openRolePanelBtn) openRolePanelBtn.onClick.AddListener(OnOpenRolePanelClicked);

        // 直接功能
        if (sleepBtn) sleepBtn.onClick.AddListener(OnSleepClicked);
        if (massageBtn) massageBtn.onClick.AddListener(OnMassageClicked);
        if (workoutBtn) workoutBtn.onClick.AddListener(OnWorkoutClicked);
        if (waterfallBtn) waterfallBtn.onClick.AddListener(OnWaterfallClicked);
        if (gachaBtn) gachaBtn.onClick.AddListener(OnGachaClicked);

        // 初始化商店大面板
        if (shopListUI) shopListUI.Init(currentShopConfig, this);

        // 绑定道场和商店按钮
        if (learnSkillBtn) learnSkillBtn.onClick.AddListener(() => shopListUI.OpenLearnSkill());
        if (upgradeSkillBtn) upgradeSkillBtn.onClick.AddListener(() => shopListUI.OpenUpgradeSkill());
        if (masterSkillBtn) masterSkillBtn.onClick.AddListener(() => shopListUI.OpenMasterSkill());

        if (buyEquipBtn) buyEquipBtn.onClick.AddListener(() => shopListUI.OpenBuyEquipment());
        if (buyItemBtn) buyItemBtn.onClick.AddListener(() => shopListUI.OpenBuyItem());
    }

    // ==========================================
    // 基础 UI 开关与刷新
    // ==========================================
    public void ShowPanel()
    {
        gameObject.SetActive(true);
        RefreshPlayerStatusUI();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    public void RefreshPlayerStatusUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerProfile == null) return;
        var profile = GameManager.Instance.playerProfile;

        if (hpText) hpText.text = $"生命: {profile.currentHp} / {profile.GetFinalMaxLife()}";
        if (goldText) goldText.text = $"金币: {profile.totalGold}";
        if (attrPointsText) attrPointsText.text = $"未分配点数: {profile.unallocatedPoints}";
    }

    // ==========================================
    // 打开角色面板逻辑 (新增)
    // ==========================================
    private void OnOpenRolePanelClicked()
    {
        if (roleUIManager != null)
        {
            roleUIManager.ShowPanel();
        }
        else
        {
            Debug.LogWarning("未绑定 RoleUIManager，无法打开角色面板！请在 Inspector 中拖入该组件。");
        }
    }

    // ==========================================
    // 休息、修炼与摇奖逻辑 (保持不变)
    // ==========================================
    private void OnSleepClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.ConsumeGold(10))
        {
            profile.currentHp = Mathf.Min(profile.currentHp + 5, profile.GetFinalMaxLife());
            Debug.Log("睡觉休息：花费10金币，恢复5点生命。");
            RefreshPlayerStatusUI();
        }
        else Debug.Log("金币不足，无法睡觉！");
    }

    private void OnMassageClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.ConsumeGold(35))
        {
            profile.currentHp = Mathf.Min(profile.currentHp + 20, profile.GetFinalMaxLife());
            profile.hasMassageBuff = true;
            Debug.Log("舒筋活血：花费35金币，恢复20点生命，并获得[精力充沛]Buff。");
            RefreshPlayerStatusUI();
        }
        else Debug.Log("金币不足，无法按摩！");
    }

    private void OnWorkoutClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.ConsumeGold(25))
        {
            profile.unallocatedPoints += 1;
            Debug.Log("挥汗如雨：花费25金币，获得1点自由属性点。");
            RefreshPlayerStatusUI();
        }
        else Debug.Log("金币不足，无法锻炼！");
    }

    private void OnWaterfallClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.currentHp > 6)
        {
            profile.ConsumeHpSafely(6);
            int gain = Random.Range(1, 3);
            int attr = Random.Range(0, 4);

            switch (attr)
            {
                case 0: profile.baseStrength += gain; Debug.Log($"瀑布冥想：损失6生命，顿悟！力量 +{gain}"); break;
                case 1: profile.baseMaxLife += gain * 2; Debug.Log($"瀑布冥想：损失6生命，顿悟！基础生命上限 +{gain * 2}"); break;
                case 2: profile.baseMaxStamina += gain; Debug.Log($"瀑布冥想：损失6生命，顿悟！基础体力上限 +{gain}"); break;
                case 3: profile.baseMentality += gain; Debug.Log($"瀑布冥想：损失6生命，顿悟！精神 +{gain}"); break;
            }
            RefreshPlayerStatusUI();
        }
        else Debug.Log("生命体征过低，无法承受瀑布的冲击！");
    }

    private void OnGachaClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (!profile.ConsumeGold(10))
        {
            Debug.Log("金币不足，无法摇奖！");
            return;
        }

        int roll = Random.Range(0, 100);

        if (roll < 40)
        {
            profile.currentHp = Mathf.Min(profile.currentHp + 3, profile.GetFinalMaxLife());
            Debug.Log("摇奖结果：获得微小恢复，生命 +3");
        }
        else if (roll < 60)
        {
            profile.unallocatedPoints += 1;
            Debug.Log("摇奖结果：灵光一闪，未分配属性点 +1");
        }
        else if (roll < 80)
        {
            if (currentShopConfig != null && currentShopConfig.availableItems.Count > 0)
            {
                var randomItem = currentShopConfig.availableItems[Random.Range(0, currentShopConfig.availableItems.Count)];
                if (shopListUI != null) shopListUI.Invoke("AddOrStackItem", 0);
                Debug.Log($"摇奖结果：获得道具 [{randomItem.skillName}]");
            }
            else Debug.Log("摇奖结果：道具池为空，退还10金币");
        }
        else if (roll < 90)
        {
            if (currentShopConfig != null && currentShopConfig.availableSkills.Count > 0)
            {
                var randomSkill = currentShopConfig.availableSkills[Random.Range(0, currentShopConfig.availableSkills.Count)];
                Debug.Log($"摇奖结果：获得招式秘籍 [{randomSkill.skillName}]");
            }
            else Debug.Log("摇奖结果：招式池为空，退还10金币");
        }
        else
        {
            Debug.Log("摇奖结果：神秘力量涌入，某个已装备的招式提升了1级！");
        }

        RefreshPlayerStatusUI();
    }
}