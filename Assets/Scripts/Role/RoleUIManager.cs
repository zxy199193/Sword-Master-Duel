using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoleUIManager : MonoBehaviour
{
    [Header("UI 引用 - 基础信息")]
    public Text roleNameText;
    public Text levelText;
    public Text expText;
    public Slider expSlider;
    public Image expFill;
    public Text goldText;
    public System.Action OnCloseCallback;

    [Header("UI 引用 - 追加基础信息")]
    public Text hitBarSpeedText;
    public Text hitBarSlowdownText;
    public Text weaponAtkFactorText;
    public Text armorExtraLifeText;

    [Header("UI 引用 - 属性面板")]
    public Text lifeText;
    public Text staminaText;
    public Text vitalityText;
    public Text enduranceText;
    public Text strengthText;
    public Text mentalityText;
    public Text staminaRecoverText;
    public Text hpRecoverText;

    [Header("UI 引用 - 加点按钮")]
    public Text unallocatedPointsText;
    public Button addVitalityBtn;
    public Button addEnduranceBtn;
    public Button addStrengthBtn;
    public Button addMentalityBtn;

    [Header("UI 引用 - 装备系统 (组件化)")]
    public EquipSlotUI weaponSlot;
    public EquipSlotUI armorSlot;
    public EquipSlotUI[] accSlots;

    [Header("UI 引用 - 道具系统 (组件化)")]
    public ItemSlotUI[] itemSlots;
    public int maxItemSlots = 4;
    public ItemListUI itemListUI;

    [Header("UI 引用 - 招式系统 (组件化)")]
    public SkillSlotUI[] skillSlots;
    public int maxSkillSlots = 4;
    public Button attackTabBtn;
    public Button defendTabBtn;
    public Button specialTabBtn;
    public RoleSkillListUI skillListUI;
    private int currentSkillTab = 0;

    [Header("UI 引用 - 弹出面板")]
    public EquipListUI equipListUI;

    [Header("UI 引用 - 负重系统")]
    public Text loadWeightText;

    [Header("UI 引用 - 属性说明弹窗")]
    [Tooltip("点击后弹出属性说明弹窗的按钮")]
    public Button attrInfoBtn;
    [Tooltip("属性说明弹窗根节点")]
    public GameObject attrInfoPanel;
    [Tooltip("属性说明弹窗内的关闭按钮")]
    public Button attrInfoCloseBtn;

    [Header("UI 引用 - 负重说明弹窗")]
    [Tooltip("点击后弹出负重说明弹窗的按钮")]
    public Button loadInfoBtn;
    [Tooltip("负重说明弹窗根节点")]
    public GameObject loadInfoPanel;
    [Tooltip("负重说明弹窗内的关闭按钮")]
    public Button loadInfoCloseBtn;

    public Button closeBtn;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Start()
    {
        InitializeButtons();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerProfile != null) 
        {
            RefreshAllUI();
        }
    }

    private void OnDisable()
    {
        // 面板隐藏时同步关闭所有子弹窗，防止残留
        if (attrInfoPanel != null) attrInfoPanel.SetActive(false);
        if (loadInfoPanel != null) loadInfoPanel.SetActive(false);
    }

    // ==========================================
    // Initialization
    // ==========================================

    private void InitializeButtons()
    {
        if (addVitalityBtn) addVitalityBtn.onClick.AddListener(() => AllocatePoint(AttributeType.Vitality));
        if (addEnduranceBtn) addEnduranceBtn.onClick.AddListener(() => AllocatePoint(AttributeType.Endurance));
        if (addStrengthBtn) addStrengthBtn.onClick.AddListener(() => AllocatePoint(AttributeType.Strength));
        if (addMentalityBtn) addMentalityBtn.onClick.AddListener(() => AllocatePoint(AttributeType.Mentality));

        // 属性说明弹窗
        if (attrInfoBtn)      attrInfoBtn.onClick.AddListener(() => attrInfoPanel?.SetActive(true));
        if (attrInfoCloseBtn) attrInfoCloseBtn.onClick.AddListener(() => attrInfoPanel?.SetActive(false));

        // 负重说明弹窗
        if (loadInfoBtn)      loadInfoBtn.onClick.AddListener(() => loadInfoPanel?.SetActive(true));
        if (loadInfoCloseBtn) loadInfoCloseBtn.onClick.AddListener(() => loadInfoPanel?.SetActive(false));

        if (weaponSlot != null && weaponSlot.slotBtn != null) weaponSlot.slotBtn.onClick.AddListener(OnWeaponSlotClicked);
        if (armorSlot != null && armorSlot.slotBtn != null) armorSlot.slotBtn.onClick.AddListener(OnArmorSlotClicked);

        if (accSlots != null)
        {
            for (int i = 0; i < accSlots.Length; i++)
            {
                int slotIndex = i;
                if (accSlots[i] != null && accSlots[i].slotBtn != null) 
                    accSlots[i].slotBtn.onClick.AddListener(() => OnAccSlotClicked(slotIndex));
            }
        }

        if (itemSlots != null)
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                int slotIndex = i;
                if (itemSlots[i] != null && itemSlots[i].slotBtn != null) 
                    itemSlots[i].slotBtn.onClick.AddListener(() => OnItemSlotClicked(slotIndex));
            }
        }

        if (attackTabBtn) attackTabBtn.onClick.AddListener(() => SwitchSkillTab(0));
        if (defendTabBtn) defendTabBtn.onClick.AddListener(() => SwitchSkillTab(1));
        if (specialTabBtn) specialTabBtn.onClick.AddListener(() => SwitchSkillTab(2));

        if (skillSlots != null)
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                int index = i;
                if (skillSlots[i] != null && skillSlots[i].slotBtn != null) 
                    skillSlots[i].slotBtn.onClick.AddListener(() => OnSkillSlotClicked(index));
            }
        }

        if (closeBtn) closeBtn.onClick.AddListener(ClosePanel);
    }

    // ==========================================
    // Public Methods
    // ==========================================

    public void ShowPanel() 
    { 
        gameObject.SetActive(true); 
        RefreshAllUI(); 
        ForceRefreshLayout();
    }

    public void ClosePanel() 
    { 
        gameObject.SetActive(false); 
        OnCloseCallback?.Invoke();
    }

    // ==========================================
    // UI Refresh Logic
    // ==========================================

    private void RefreshAllUI()
    {
        if (GameManager.Instance == null) return;
        PlayerProfile profile = GameManager.Instance.playerProfile;

        // Basic Info
        if (roleNameText) roleNameText.text = profile.playerRoleAsset.roleName;
        if (levelText) 
        {
            if (profile.level >= 12) levelText.text = $"{profile.level} (Max)";
            else levelText.text = $"{profile.level}";
        }
        if (expText)
        {
            if (profile.level >= 12) expText.text = "MAX";
            else expText.text = $"{profile.currentExp}/100";
        }
        
        float expRatio = profile.level >= 12 ? 1f : (profile.currentExp / 100f);
        if (expSlider) expSlider.value = expRatio;
        if (expFill) expFill.fillAmount = expRatio;

        if (goldText) goldText.text = profile.totalGold.ToString();

        // Attributes
        if (lifeText) lifeText.text = $"{profile.currentHp}/{profile.GetFinalMaxLife()}";
        if (staminaText) staminaText.text = $"{profile.currentStamina}/{profile.GetFinalMaxStamina()}";
        
        if (vitalityText)
        {
            int finalVit = profile.GetFinalVitality();
            int bonusVit = finalVit - profile.vitality;
            vitalityText.text = bonusVit > 0 ? $"{finalVit} (+{bonusVit})" : finalVit.ToString();
        }
        if (enduranceText)
        {
            int finalEnd = profile.GetFinalEndurance();
            int bonusEnd = finalEnd - profile.endurance;
            enduranceText.text = bonusEnd > 0 ? $"{finalEnd} (+{bonusEnd})" : finalEnd.ToString();
        }
        if (strengthText)
        {
            int finalStr = profile.GetFinalStrength();
            int bonusStr = finalStr - profile.baseStrength;
            strengthText.text = bonusStr > 0 ? $"{finalStr} (+{bonusStr})" : finalStr.ToString();
        }
        if (mentalityText)
        {
            int finalMen = profile.GetFinalMentality();
            int bonusMen = finalMen - profile.baseMentality;
            mentalityText.text = bonusMen > 0 ? $"{finalMen} (+{bonusMen})" : finalMen.ToString();
        }

        if (staminaRecoverText) staminaRecoverText.text = $"{profile.GetStaminaRecoverPerTurn()} 点/回合";
        if (hpRecoverText) hpRecoverText.text = $"{profile.GetHpRecoverPerTurn()} 点/回合";

        // 追加基础信息
        if (hitBarSpeedText) hitBarSpeedText.text = profile.GetFinalHitBarSpeed().ToString("F0");
        if (hitBarSlowdownText) hitBarSlowdownText.text = profile.GetFinalHitBarSlowdown().ToString("F0") + "/秒";
        if (weaponAtkFactorText) weaponAtkFactorText.text = profile.GetWeaponAtkFactor().ToString("F1");
        if (armorExtraLifeText) armorExtraLifeText.text = profile.GetArmorExtraLife().ToString();

        // Allocate Points
        if (unallocatedPointsText) unallocatedPointsText.text = $"{profile.unallocatedPoints}";
        bool canAllocate = profile.unallocatedPoints > 0;
        if (addVitalityBtn) addVitalityBtn.gameObject.SetActive(canAllocate);
        if (addEnduranceBtn) addEnduranceBtn.gameObject.SetActive(canAllocate);
        if (addStrengthBtn) addStrengthBtn.gameObject.SetActive(canAllocate);
        if (addMentalityBtn) addMentalityBtn.gameObject.SetActive(canAllocate);

        // Equipment Slots
        if (weaponSlot != null) weaponSlot.UpdateUI(profile.equippedWeapon);
        if (armorSlot != null) armorSlot.UpdateUI(profile.equippedArmor);

        if (accSlots != null)
        {
            for (int i = 0; i < accSlots.Length; i++)
            {
                if (accSlots[i] == null) continue;
                EquipmentData accData = null;
                if (i < profile.equippedAccessories.Count) accData = profile.equippedAccessories[i];
                accSlots[i].UpdateUI(accData);
            }
        }

        // Item Slots
        if (itemSlots != null)
        {
            int maxCap = profile.GetMaxItemCapacity();
            for (int i = 0; i < itemSlots.Length; i++)
            {
                if (i >= maxItemSlots) 
                { 
                    itemSlots[i].gameObject.SetActive(false); 
                    continue; 
                }
                itemSlots[i].gameObject.SetActive(true);
                SkillSlot itemData = null;
                int totalQuantity = 0;
                if (i < profile.equippedItems.Count) 
                {
                    itemData = profile.equippedItems[i];
                    if (itemData != null && itemData.skillData != null)
                    {
                        foreach (var s in profile.equippedItems) { if (s != null && s.skillData == itemData.skillData) totalQuantity += s.quantity; }
                        foreach (var s in profile.storageSkillsAndItems) { if (s != null && s.skillData == itemData.skillData) totalQuantity += s.quantity; }
                    }
                }
                itemSlots[i].UpdateUI(itemData, maxCap, totalQuantity);
            }
        }

        RefreshLoadWeightUI(profile);
        RefreshSkillSlots();

        ForceRefreshLayout();
    }

    private void RefreshLoadWeightUI(PlayerProfile profile)
    {
        if (loadWeightText == null) return;

        int currentLoad = profile.GetCurrentLoadWeight();
        int maxLoad = profile.GetMaxLoad();

        float ratio = maxLoad > 0 ? (float)currentLoad / maxLoad : 0f;
        string stateStr = "";

        if (ratio < 0.3f) stateStr = "轻";
        else if (ratio <= 1.0f) stateStr = "适中";
        else if (ratio <= 1.5f) stateStr = "超重";
        else stateStr = "极重";

        loadWeightText.text = $"{currentLoad}/{maxLoad}（{stateStr}）";
    }

    private void AllocatePoint(AttributeType attrType)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        if (profile.unallocatedPoints <= 0) return;
        profile.unallocatedPoints--;

        switch (attrType)
        {
            case AttributeType.Vitality:
                profile.vitality += 1;
                profile.currentHp += 6;
                break;
            case AttributeType.Endurance:
                int oldMaxStam = profile.GetFinalMaxStamina();
                profile.endurance += 1;
                int newMaxStam = profile.GetFinalMaxStamina();
                profile.currentStamina += (newMaxStam - oldMaxStam);
                break;
            case AttributeType.Strength:
                profile.baseStrength += 1;
                break;
            case AttributeType.Mentality:
                profile.baseMentality += 1;
                break;
        }

        if (profile.currentHp > profile.GetFinalMaxLife()) profile.currentHp = profile.GetFinalMaxLife();
        if (profile.currentStamina > profile.GetFinalMaxStamina()) profile.currentStamina = profile.GetFinalMaxStamina();

        RefreshAllUI();
    }

    // ==========================================
    // Equipment Management
    // ==========================================

    private void OnWeaponSlotClicked()
    {
        if (equipListUI == null) return;
        equipListUI.OpenList(EquipmentType.Weapon, GameManager.Instance.playerProfile.equippedWeapon,
            (newWeapon) => EquipEquipment(EquipmentType.Weapon, newWeapon), null);
    }

    private void OnArmorSlotClicked()
    {
        if (equipListUI == null) return;
        equipListUI.OpenList(EquipmentType.Armor, GameManager.Instance.playerProfile.equippedArmor,
            (newArmor) => EquipEquipment(EquipmentType.Armor, newArmor), () => UnequipEquipment(EquipmentType.Armor));
    }

    private void EquipEquipment(EquipmentType type, EquipmentData newItem)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        UnequipEquipment(type);
        if (type == EquipmentType.Weapon) profile.equippedWeapon = newItem;
        else if (type == EquipmentType.Armor) profile.equippedArmor = newItem;
        profile.storageEquipments.Remove(newItem);
        RefreshAllUI();
    }

    private void UnequipEquipment(EquipmentType type)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        EquipmentData currentEquip = null;
        if (type == EquipmentType.Weapon) { currentEquip = profile.equippedWeapon; profile.equippedWeapon = null; }
        else if (type == EquipmentType.Armor) { currentEquip = profile.equippedArmor; profile.equippedArmor = null; }
        if (currentEquip != null) profile.storageEquipments.Add(currentEquip);
        RefreshAllUI();
    }

    private void OnAccSlotClicked(int index)
    {
        if (equipListUI == null) return;
        PlayerProfile profile = GameManager.Instance.playerProfile;
        EquipmentData currentAcc = null;
        if (index < profile.equippedAccessories.Count) currentAcc = profile.equippedAccessories[index];
        equipListUI.OpenList(EquipmentType.Accessory, currentAcc,
            (newAcc) => EquipAccessory(index, newAcc), () => UnequipAccessory(index));
    }

    private void EquipAccessory(int index, EquipmentData newAcc)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        UnequipAccessory(index);
        while (profile.equippedAccessories.Count <= index) profile.equippedAccessories.Add(null);
        profile.equippedAccessories[index] = newAcc;
        profile.storageEquipments.Remove(newAcc);
        RefreshAllUI();
    }

    private void UnequipAccessory(int index)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        if (index < profile.equippedAccessories.Count)
        {
            EquipmentData currentAcc = profile.equippedAccessories[index];
            if (currentAcc != null) { profile.storageEquipments.Add(currentAcc); profile.equippedAccessories[index] = null; }
        }
        RefreshAllUI();
    }

    // ==========================================
    // Item Management
    // ==========================================

    private void OnItemSlotClicked(int index)
    {
        if (itemListUI == null) return;
        PlayerProfile profile = GameManager.Instance.playerProfile;
        SkillSlot currentItem = null;
        if (index < profile.equippedItems.Count) currentItem = profile.equippedItems[index];

        itemListUI.OpenList(currentItem, (newItem) => EquipBagItem(index, newItem), () => UnequipBagItem(index));
    }

    private void EquipBagItem(int index, SkillSlot newItem)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        UnequipBagItem(index);
        while (profile.equippedItems.Count <= index) profile.equippedItems.Add(null);
        profile.equippedItems[index] = newItem;
        profile.storageSkillsAndItems.Remove(newItem);
        RefreshAllUI();
    }

    private void UnequipBagItem(int index)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        if (index < profile.equippedItems.Count)
        {
            SkillSlot currentItem = profile.equippedItems[index];
            if (currentItem != null) { profile.storageSkillsAndItems.Add(currentItem); profile.equippedItems[index] = null; }
        }
        RefreshAllUI();
    }

    // ==========================================
    // Skill Management
    // ==========================================

    private void SwitchSkillTab(int tabIndex)
    {
        currentSkillTab = tabIndex;
        RefreshSkillSlots();
        ForceRefreshLayout();
    }

    private void RefreshSkillSlots()
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        List<SkillSlot> targetList = null;

        if (currentSkillTab == 0) targetList = profile.equippedAttackSkills;
        else if (currentSkillTab == 1) targetList = profile.equippedDefendSkills;
        else if (currentSkillTab == 2) targetList = profile.equippedSpecialSkills;

        if (skillSlots != null)
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (i >= maxSkillSlots)
                {
                    skillSlots[i].gameObject.SetActive(false);
                    continue;
                }
                skillSlots[i].gameObject.SetActive(true);

                SkillSlot slot = null;
                if (targetList != null && i < targetList.Count) slot = targetList[i];
                skillSlots[i].UpdateUI(slot);
            }
        }
    }

    private void OnSkillSlotClicked(int index)
    {
        if (skillListUI == null) return;
        PlayerProfile profile = GameManager.Instance.playerProfile;

        List<SkillSlot> targetList = null;
        string tabName = "";
        List<SkillType> allowedTypes = new List<SkillType>();

        if (currentSkillTab == 0)
        {
            targetList = profile.equippedAttackSkills;
            tabName = "攻击招式";
            allowedTypes.Add(SkillType.Attack);
        }
        else if (currentSkillTab == 1)
        {
            targetList = profile.equippedDefendSkills;
            tabName = "防闪招式";
            allowedTypes.Add(SkillType.Defend);
            allowedTypes.Add(SkillType.Dodge);
        }
        else if (currentSkillTab == 2)
        {
            targetList = profile.equippedSpecialSkills;
            tabName = "特殊招式";
            allowedTypes.Add(SkillType.Special);
        }

        SkillSlot currentSkill = null;
        if (targetList != null && index < targetList.Count) currentSkill = targetList[index];

        int equippedCount = 0;
        if (targetList != null) foreach (var s in targetList) if (s != null) equippedCount++;
        bool isOnlyOneLeft = equippedCount <= 1 && currentSkill != null;

        skillListUI.OpenList(tabName, allowedTypes, currentSkill, isOnlyOneLeft,
            (newSkill) => EquipSkill(index, targetList, newSkill),
            () => UnequipSkill(index, targetList)
        );
    }

    private void EquipSkill(int index, List<SkillSlot> targetList, SkillSlot newSkill)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        UnequipSkill(index, targetList);

        while (targetList.Count <= index) targetList.Add(null);
        targetList[index] = newSkill;
        profile.storageSkillsAndItems.Remove(newSkill);
        RefreshAllUI();
    }

    private void UnequipSkill(int index, List<SkillSlot> targetList)
    {
        PlayerProfile profile = GameManager.Instance.playerProfile;
        if (index < targetList.Count)
        {
            SkillSlot currentSkill = targetList[index];
            if (currentSkill != null)
            {
                profile.storageSkillsAndItems.Add(currentSkill);
                targetList[index] = null;
            }
        }
        RefreshAllUI();
    }

    private void ForceRefreshLayout()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(RefreshLayoutRoutine());
    }

    private IEnumerator RefreshLayoutRoutine()
    {
        // 第一帧：强制同步画布并等待一帧，让 Text 等组件计算出正确大小
        Canvas.ForceUpdateCanvases();
        yield return null;

        // 第二帧：递归标记所有布局组为脏，并从子到父强制重绘
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            LayoutGroup[] layouts = GetComponentsInChildren<LayoutGroup>(true);
            for (int i = layouts.Length - 1; i >= 0; i--)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layouts[i].GetComponent<RectTransform>());
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}