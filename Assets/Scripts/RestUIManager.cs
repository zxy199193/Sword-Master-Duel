using UnityEngine;
using UnityEngine.UI;

public class RestUIManager : MonoBehaviour
{
    [Header("Shop Config")]
    public ShopConfig currentShopConfig;

    [Header("UI References - Player Status")]
    public Text hpText;
    public Text goldText;
    public Text attrPointsText;

    [Header("UI References - Shop/Dojo Systems")]
    public ShopListUI shopListUI;

    [Header("UI References - Role System")]
    public Button openRolePanelBtn;
    public RoleUIManager roleUIManager;

    [Header("UI References - Rest Actions")]
    public Button sleepBtn;
    public Button massageBtn;
    public Button workoutBtn;
    public Button waterfallBtn;
    public Button gachaBtn;

    [Header("UI References - Dojo Actions")]
    public Button learnSkillBtn;
    public Button upgradeSkillBtn;
    public Button masterSkillBtn;

    [Header("UI References - Shop Actions")]
    public Button buyEquipBtn;
    public Button buyItemBtn;

    [Header("UI References - Flow Control")]
    public Button continueBtn;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Start()
    {
        RefreshPlayerStatusUI();

        if (openRolePanelBtn) openRolePanelBtn.onClick.AddListener(OnOpenRolePanelClicked);

        if (sleepBtn) sleepBtn.onClick.AddListener(OnSleepClicked);
        if (massageBtn) massageBtn.onClick.AddListener(OnMassageClicked);
        if (workoutBtn) workoutBtn.onClick.AddListener(OnWorkoutClicked);
        if (waterfallBtn) waterfallBtn.onClick.AddListener(OnWaterfallClicked);
        if (gachaBtn) gachaBtn.onClick.AddListener(OnGachaClicked);

        if (shopListUI) shopListUI.Init(currentShopConfig, this);

        if (learnSkillBtn) learnSkillBtn.onClick.AddListener(() => shopListUI.OpenLearnSkill());
        if (upgradeSkillBtn) upgradeSkillBtn.onClick.AddListener(() => shopListUI.OpenUpgradeSkill());
        if (masterSkillBtn) masterSkillBtn.onClick.AddListener(() => shopListUI.OpenMasterSkill());

        if (buyEquipBtn) buyEquipBtn.onClick.AddListener(() => shopListUI.OpenBuyEquipment());
        if (buyItemBtn) buyItemBtn.onClick.AddListener(() => shopListUI.OpenBuyItem());

        if (continueBtn) continueBtn.onClick.AddListener(OnContinueClicked);
    }

    // ==========================================
    // Public Methods
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
    // Private Methods - Event Handlers
    // ==========================================

    private void OnContinueClicked()
    {
        ClosePanel();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceToNextMainLevel();
        }
    }

    private void OnOpenRolePanelClicked()
    {
        if (roleUIManager != null)
        {
            roleUIManager.ShowPanel();
        }
        else
        {
            Debug.LogWarning("未绑定 RoleUIManager，无法打开角色面板！");
        }
    }

    // ==========================================
    // Private Methods - Rest Actions
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
                case 1: profile.vitality += gain; Debug.Log($"瀑布冥想：损失6生命，顿悟！活力 +{gain}"); break;
                case 2: profile.endurance += gain; Debug.Log($"瀑布冥想：损失6生命，顿悟！耐力 +{gain}"); break;
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
                
                // 修复：直接实现添加道具或叠加数量的逻辑
                bool found = false;
                if (profile.equippedItems != null)
                {
                    foreach (var slot in profile.equippedItems)
                    {
                        if (slot != null && slot.skillData == randomItem) { slot.quantity++; found = true; break; }
                    }
                }
                if (!found && profile.storageSkillsAndItems != null)
                {
                    foreach (var slot in profile.storageSkillsAndItems)
                    {
                        if (slot != null && slot.skillData == randomItem) { slot.quantity++; found = true; break; }
                    }
                }
                if (!found)
                {
                    profile.storageSkillsAndItems.Add(new SkillSlot { skillData = randomItem, level = 1, quantity = 1 });
                }
                Debug.Log($"摇奖结果：获得道具 [{randomItem.skillName}]");
            }
            else 
            {
                profile.totalGold += 10;
                Debug.Log("摇奖结果：道具池为空，退还10金币");
            }
        }
        else if (roll < 90)
        {
            if (currentShopConfig != null && currentShopConfig.availableSkills.Count > 0)
            {
                var randomSkill = currentShopConfig.availableSkills[Random.Range(0, currentShopConfig.availableSkills.Count)];
                
                // 修复：检查是否已有该招式，如果没有才添加
                bool alreadyHas = false;
                var allSlots = new System.Collections.Generic.List<SkillSlot>();
                if (profile.equippedAttackSkills != null) allSlots.AddRange(profile.equippedAttackSkills);
                if (profile.equippedDefendSkills != null) allSlots.AddRange(profile.equippedDefendSkills);
                if (profile.equippedSpecialSkills != null) allSlots.AddRange(profile.equippedSpecialSkills);
                if (profile.storageSkillsAndItems != null) allSlots.AddRange(profile.storageSkillsAndItems);

                foreach (var slot in allSlots)
                {
                    if (slot != null && slot.skillData == randomSkill) { alreadyHas = true; break; }
                }

                if (!alreadyHas)
                {
                    profile.storageSkillsAndItems.Add(new SkillSlot { skillData = randomSkill, level = 1, quantity = 1 });
                    Debug.Log($"摇奖结果：获得招式秘籍 [{randomSkill.skillName}]");
                }
                else
                {
                    // 如果已经有了，转换成 20 金币
                    profile.totalGold += 20;
                    Debug.Log($"摇奖结果：获得了已有的招式秘籍 [{randomSkill.skillName}]，自动转化为 20 金币");
                }
            }
            else 
            {
                profile.totalGold += 10;
                Debug.Log("摇奖结果：招式池为空，退还10金币");
            }
        }
        else
        {
            // 修复：随机找一个等级小于3的已装备招式提升1级
            var upgradeCandidates = new System.Collections.Generic.List<SkillSlot>();
            if (profile.equippedAttackSkills != null) upgradeCandidates.AddRange(profile.equippedAttackSkills);
            if (profile.equippedDefendSkills != null) upgradeCandidates.AddRange(profile.equippedDefendSkills);
            if (profile.equippedSpecialSkills != null) upgradeCandidates.AddRange(profile.equippedSpecialSkills);

            upgradeCandidates.RemoveAll(s => s == null || s.skillData == null || s.level >= 3 || s.skillData.skillType == SkillType.Item);

            if (upgradeCandidates.Count > 0)
            {
                var targetSlot = upgradeCandidates[Random.Range(0, upgradeCandidates.Count)];
                targetSlot.level++;
                Debug.Log($"摇奖结果：神秘力量涌入，你装备的招式 [{targetSlot.skillData.skillName}] 提升到了 Lv.{targetSlot.level}！");
            }
            else
            {
                profile.totalGold += 10;
                Debug.Log("摇奖结果：你的装备中没有可以升级的招式，神秘力量散去，退还10金币。");
            }
        }

        RefreshPlayerStatusUI();
    }
}