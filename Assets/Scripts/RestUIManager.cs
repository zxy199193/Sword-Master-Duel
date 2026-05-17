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
    public Text daysText;

    [Header("UI References - Shop/Dojo Systems")]
    public ShopListUI shopListUI;

    [Header("UI References - Role System")]
    public Button openRolePanelBtn;
    public RoleUIManager roleUIManager;

    [Header("UI References - Main Menus")]
    public Button homeMenuBtn;
    public Button dojoMenuBtn;
    public Button shopMenuBtn;
    public Button tavernMenuBtn;
    public Button mountainMenuBtn;
    public Button boardMenuBtn;

    [Header("UI References - Panels")]
    public GameObject homePanel;
    public GameObject dojoPanel;
    public GameObject shopPanel;
    public GameObject tavernPanel;
    public GameObject mountainPanel;
    public GameObject boardPanel;
    public Button bgMaskBtn;

    [Header("UI References - Continue Confirm")]
    public GameObject continueConfirmPanel;
    public Text continueConfirmText;
    public Button confirmContinueBtn;
    public Button cancelContinueBtn;

    [Header("UI References - Home Actions")]
    public Button restBtn;
    public Button workoutBtn;

    [Header("UI References - Dojo Actions")]
    public Button learnSkillBtn;
    public Button upgradeSkillBtn;
    public Button masterSkillBtn;

    [Header("UI References - Shop Actions")]
    public Button buyWeaponBtn;
    public Button buyArmorBtn;
    public Button buyAccessoryBtn;
    public Button buyItemBtn;

    [Header("UI References - Tavern & Board")]
    public Button drinkBtn;
    public Button taskEasyBtn;
    public Button taskMediumBtn;
    public Button taskHardBtn;
    public Button taskExtremeBtn;

    [Header("UI References - Mountain Actions")]
    public Button waterfallBtn;

    [Header("UI References - Flow Control")]
    public Button continueBtn;
    public RestTransitionUI transitionUI;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Start()
    {
        RefreshPlayerStatusUI();

        if (openRolePanelBtn) openRolePanelBtn.onClick.AddListener(OnOpenRolePanelClicked);

        // Bind main menus
        if (homeMenuBtn) homeMenuBtn.onClick.AddListener(() => OpenPanel(homePanel));
        if (dojoMenuBtn) dojoMenuBtn.onClick.AddListener(() => OpenPanel(dojoPanel));
        if (shopMenuBtn) shopMenuBtn.onClick.AddListener(() => OpenPanel(shopPanel));
        if (tavernMenuBtn) tavernMenuBtn.onClick.AddListener(() => OpenPanel(tavernPanel));
        if (mountainMenuBtn) mountainMenuBtn.onClick.AddListener(() => OpenPanel(mountainPanel));
        if (boardMenuBtn) boardMenuBtn.onClick.AddListener(() => OpenPanel(boardPanel));

        // Bind actions
        if (restBtn) restBtn.onClick.AddListener(OnRestClicked);
        if (workoutBtn) workoutBtn.onClick.AddListener(OnWorkoutClicked);
        if (waterfallBtn) waterfallBtn.onClick.AddListener(OnWaterfallClicked);
        if (drinkBtn) drinkBtn.onClick.AddListener(OnDrinkClicked);
        
        if (taskEasyBtn) taskEasyBtn.onClick.AddListener(() => OnTaskDifficultyClicked(TaskDifficulty.Easy));
        if (taskMediumBtn) taskMediumBtn.onClick.AddListener(() => OnTaskDifficultyClicked(TaskDifficulty.Medium));
        if (taskHardBtn) taskHardBtn.onClick.AddListener(() => OnTaskDifficultyClicked(TaskDifficulty.Hard));
        if (taskExtremeBtn) taskExtremeBtn.onClick.AddListener(() => OnTaskDifficultyClicked(TaskDifficulty.Extreme));

        if (bgMaskBtn) bgMaskBtn.onClick.AddListener(CloseSubPanel);

        if (shopListUI) shopListUI.Init(currentShopConfig, this);

        if (learnSkillBtn) learnSkillBtn.onClick.AddListener(() => shopListUI.OpenLearnSkill());
        if (upgradeSkillBtn) upgradeSkillBtn.onClick.AddListener(() => shopListUI.OpenUpgradeSkill());
        if (masterSkillBtn) masterSkillBtn.onClick.AddListener(() => shopListUI.OpenMasterSkill());


        // 原有的分类按钮也全部指向统一商店的不同分类
        if (buyWeaponBtn) buyWeaponBtn.onClick.AddListener(() => shopListUI.OpenShop(ShopCategory.Weapon));
        if (buyArmorBtn) buyArmorBtn.onClick.AddListener(() => shopListUI.OpenShop(ShopCategory.Armor));
        if (buyAccessoryBtn) buyAccessoryBtn.onClick.AddListener(() => shopListUI.OpenShop(ShopCategory.Accessory));
        if (buyItemBtn) buyItemBtn.onClick.AddListener(() => shopListUI.OpenShop(ShopCategory.Item));

        if (continueBtn) continueBtn.onClick.AddListener(OnContinueClicked);
        if (confirmContinueBtn) confirmContinueBtn.onClick.AddListener(OnConfirmContinue);
        if (cancelContinueBtn) cancelContinueBtn.onClick.AddListener(OnCancelContinue);
        
        if (roleUIManager) roleUIManager.OnCloseCallback += RefreshPlayerStatusUI;

        // Hide panels by default
        OpenPanel(null);
        if (continueConfirmPanel) continueConfirmPanel.SetActive(false);
    }

    // ==========================================
    // Public Methods
    // ==========================================

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        RefreshPlayerStatusUI();
        OpenPanel(null);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    public void RefreshPlayerStatusUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerProfile == null) return;
        
        var profile = GameManager.Instance.playerProfile;

        if (hpText) hpText.text = $"{profile.currentHp}/{profile.GetFinalMaxLife()}";
        if (goldText) goldText.text = $"{profile.totalGold}";
        if (attrPointsText) attrPointsText.text = $"{profile.unallocatedPoints}";
        if (daysText) daysText.text = $"第{profile.currentRestDays}/{profile.maxRestDays}天";
    }

    // ==========================================
    // Private Methods - Event Handlers
    // ==========================================

    public void CloseSubPanel()
    {
        OpenPanel(null);
    }

    private void OpenPanel(GameObject targetPanel)
    {
        if (bgMaskBtn) bgMaskBtn.gameObject.SetActive(targetPanel != null);

        if (homePanel) homePanel.SetActive(homePanel == targetPanel);
        if (dojoPanel) dojoPanel.SetActive(dojoPanel == targetPanel);
        if (shopPanel) shopPanel.SetActive(shopPanel == targetPanel);
        if (tavernPanel) tavernPanel.SetActive(tavernPanel == targetPanel);
        if (mountainPanel) mountainPanel.SetActive(mountainPanel == targetPanel);
        if (boardPanel) boardPanel.SetActive(boardPanel == targetPanel);
    }

    private void OnContinueClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.currentRestDays > 0)
        {
            if (continueConfirmPanel) 
            {
                continueConfirmPanel.SetActive(true);
                if (continueConfirmText) continueConfirmText.text = $"距离决斗还有{profile.currentRestDays}天，是否【休息】到决斗？";
            }
            else ProceedToNextLevel();
        }
        else
        {
            ProceedToNextLevel();
        }
    }

    private void OnConfirmContinue()
    {
        var profile = GameManager.Instance.playerProfile;
        int remainingDays = profile.currentRestDays;
        
        if (remainingDays > 0)
        {
            int healAmountPerDay = Mathf.FloorToInt(profile.baseMaxLife * 0.4f);
            int totalHeal = healAmountPerDay * remainingDays;
            
            profile.currentRestDays = 0;
            profile.currentHp = Mathf.Min(profile.currentHp + totalHeal, profile.GetFinalMaxLife());
            
            Debug.Log($"一键休息：消耗了{remainingDays}天，恢复了{totalHeal}点生命。");
        }

        if (continueConfirmPanel) continueConfirmPanel.SetActive(false);
        ProceedToNextLevel();
    }

    private void OnCancelContinue()
    {
        if (continueConfirmPanel) continueConfirmPanel.SetActive(false);
    }

    private void ProceedToNextLevel()
    {
        ClosePanel();
        if (GameManager.Instance == null) return;

        // 若玩家是从关卡选择界面临时返回休息场景的，回到关卡界面而不推进关卡
        if (GameManager.Instance.isReturnedFromLevelSelect)
            GameManager.Instance.ReturnToLevelUIFromRest();
        else
            GameManager.Instance.AdvanceToNextMainLevel();
    }

    public void OnOpenRolePanelClicked()
    {
        CloseSubPanel();
        if (continueConfirmPanel) continueConfirmPanel.SetActive(false);
        if (shopListUI) shopListUI.CloseList();

        if (roleUIManager != null) roleUIManager.ShowPanel();
        else Debug.LogWarning("未绑定 RoleUIManager，无法打开角色面板！");
    }

    // ==========================================
    // Private Methods - Actions
    // ==========================================

    private void OnRestClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.currentRestDays >= 1)
        {
            int healAmount = Mathf.FloorToInt(profile.baseMaxLife * 0.4f);
            string desc = $"恢复了 {healAmount} 点生命值";

            if (transitionUI != null)
            {
                transitionUI.ShowTransition(desc, () =>
                {
                    profile.currentRestDays -= 1;
                    profile.currentHp = Mathf.Min(profile.currentHp + healAmount, profile.GetFinalMaxLife());
                    Debug.Log($"家里休息：消耗1天，恢复40%基础生命值 ({healAmount}点)。");
                    RefreshPlayerStatusUI();
                });
            }
            else
            {
                profile.currentRestDays -= 1;
                profile.currentHp = Mathf.Min(profile.currentHp + healAmount, profile.GetFinalMaxLife());
                RefreshPlayerStatusUI();
            }
        }
        else Debug.Log("天数不足，无法休息！");
    }

    private void OnWorkoutClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.currentRestDays >= 2)
        {
            string desc = "获得了 1 点自由属性点";
            if (transitionUI != null)
            {
                transitionUI.ShowTransition(desc, () =>
                {
                    profile.currentRestDays -= 2;
                    profile.unallocatedPoints += 1;
                    Debug.Log("家里锻炼：消耗2天，获得1点自由属性点。");
                    RefreshPlayerStatusUI();
                });
            }
            else
            {
                profile.currentRestDays -= 2;
                profile.unallocatedPoints += 1;
                RefreshPlayerStatusUI();
            }
        }
        else Debug.Log("天数不足，无法锻炼！");
    }

    private void OnDrinkClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.currentRestDays >= 1 && profile.totalGold >= 20)
        {
            profile.currentRestDays -= 1;
            profile.ConsumeGold(20);
            Debug.Log("酒馆饮酒：消耗1天，花费20金币。效果待定 TODO");
            RefreshPlayerStatusUI();
        }
        else Debug.Log("天数或金币不足，无法饮酒！");
    }

    private void OnTaskDifficultyClicked(TaskDifficulty difficulty)
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.currentRestDays < 1)
        {
            Debug.Log("天数不足，无法接任务！");
            return;
        }

        string diffName = "";
        switch (difficulty)
        {
            case TaskDifficulty.Easy: diffName = "简单"; break;
            case TaskDifficulty.Medium: diffName = "中等"; break;
            case TaskDifficulty.Hard: diffName = "困难"; break;
            case TaskDifficulty.Extreme: diffName = "极难"; break;
        }

        if (transitionUI != null)
        {
            transitionUI.ShowTransition($"准备进行【{diffName}】任务...", () =>
            {
                GameManager.Instance.StartTaskBattle(difficulty);
            });
        }
        else
        {
            GameManager.Instance.StartTaskBattle(difficulty);
        }
    }

    private void OnWaterfallClicked()
    {
        var profile = GameManager.Instance.playerProfile;
        if (profile.currentRestDays < 1)
        {
            Debug.Log("天数不足，无法瀑布冥想！");
            return;
        }

        if (profile.currentHp > 5)
        {
            if (transitionUI != null)
            {
                // 预先随机结果，以便在遮罩显示时告知玩家
                string resultDesc = "";
                System.Action effectCallback = null;

                int roll = Random.Range(0, 100);
                if (roll < 30)
                {
                    resultDesc = "瀑布冥想：什么也没发生...";
                    effectCallback = () => {
                        profile.currentRestDays -= 1;
                        profile.ConsumeHpSafely(5);
                    };
                }
                else if (roll < 70) // 40%
                {
                    int attr = Random.Range(0, 4);
                    string attrName = "";
                    System.Action attrAction = null;
                    switch (attr)
                    {
                        case 0: attrName = "力量"; attrAction = () => profile.baseStrength += 1; break;
                        case 1: attrName = "活力"; attrAction = () => profile.vitality += 1; break;
                        case 2: attrName = "耐力"; attrAction = () => profile.endurance += 1; break;
                        case 3: attrName = "精神"; attrAction = () => profile.baseMentality += 1; break;
                    }
                    resultDesc = $"瀑布冥想：{attrName} +1";
                    effectCallback = () => {
                        profile.currentRestDays -= 1;
                        profile.ConsumeHpSafely(5);
                        attrAction?.Invoke();
                    };
                }
                else if (roll < 90) // 20%
                {
                    var lv1Skills = GetUpgradableSkills(profile, 1);
                    if (lv1Skills.Count > 0)
                    {
                        var target = lv1Skills[Random.Range(0, lv1Skills.Count)];
                        resultDesc = $"瀑布冥想：[{target.skillData.skillName}] 升到了 Lv.2";
                        effectCallback = () => {
                            profile.currentRestDays -= 1;
                            profile.ConsumeHpSafely(5);
                            target.level++;
                        };
                    }
                    else
                    {
                        resultDesc = "瀑布冥想：什么也没发生...";
                        effectCallback = () => {
                            profile.currentRestDays -= 1;
                            profile.ConsumeHpSafely(5);
                        };
                    }
                }
                else // 10%
                {
                    var lv2Skills = GetUpgradableSkills(profile, 2);
                    if (lv2Skills.Count > 0)
                    {
                        var target = lv2Skills[Random.Range(0, lv2Skills.Count)];
                        resultDesc = $"瀑布冥想：[{target.skillData.skillName}] 升到了 Lv.3";
                        effectCallback = () => {
                            profile.currentRestDays -= 1;
                            profile.ConsumeHpSafely(5);
                            target.level++;
                        };
                    }
                    else
                    {
                        resultDesc = "瀑布冥想：什么也没发生...";
                        effectCallback = () => {
                            profile.currentRestDays -= 1;
                            profile.ConsumeHpSafely(5);
                        };
                    }
                }

                transitionUI.ShowTransition(resultDesc, () =>
                {
                    effectCallback?.Invoke();
                    RefreshPlayerStatusUI();
                });
            }
            else
            {
                // 无过渡逻辑（原逻辑）
                profile.currentRestDays -= 1;
                profile.ConsumeHpSafely(5);
                
                int roll = Random.Range(0, 100);
                if (roll < 30) Debug.Log("瀑布冥想：消耗1天、5生命，什么也没发生...");
                else if (roll < 70) // 40%
                {
                    int attr = Random.Range(0, 4);
                    switch (attr)
                    {
                        case 0: profile.baseStrength += 1; break;
                        case 1: profile.vitality += 1; break;
                        case 2: profile.endurance += 1; break;
                        case 3: profile.baseMentality += 1; break;
                    }
                }
                else if (roll < 90) // 20%
                {
                    var lv1Skills = GetUpgradableSkills(profile, 1);
                    if (lv1Skills.Count > 0) lv1Skills[Random.Range(0, lv1Skills.Count)].level++;
                }
                else // 10%
                {
                    var lv2Skills = GetUpgradableSkills(profile, 2);
                    if (lv2Skills.Count > 0) lv2Skills[Random.Range(0, lv2Skills.Count)].level++;
                }
                RefreshPlayerStatusUI();
            }
        }
        else Debug.Log("生命体征过低，无法承受瀑布的冲击！");
    }

    private System.Collections.Generic.List<SkillSlot> GetUpgradableSkills(PlayerProfile profile, int levelReq)
    {
        var list = new System.Collections.Generic.List<SkillSlot>();
        if (profile.equippedAttackSkills != null) list.AddRange(profile.equippedAttackSkills);
        if (profile.equippedDefendSkills != null) list.AddRange(profile.equippedDefendSkills);
        if (profile.equippedSpecialSkills != null) list.AddRange(profile.equippedSpecialSkills);
        if (profile.storageSkillsAndItems != null) list.AddRange(profile.storageSkillsAndItems);

        list.RemoveAll(s => s == null || s.skillData == null || s.skillData.skillType == SkillType.Item || s.level != levelReq);
        return list;
    }
}