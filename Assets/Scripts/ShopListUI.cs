using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ShopListUI : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;
    public Transform permanentContentRoot;
    public Transform randomContentRoot;
    public Text playerGoldText;
    public Button closeBtn;

    [Header("Prefabs")]
    public GameObject shopEquipPrefab;
    public GameObject shopSkillPrefab;

    [Header("Shop Refresh UI")]
    public GameObject refreshArea;
    public Button refreshBtn;
    public Text refreshCostText;

    [Header("Shop Auto Equip Confirmation UI")]
    public GameObject autoEquipConfirmPanel;
    public Button autoEquipConfirmBtn;
    public Button autoEquipCancelBtn;

    [Header("Shop Auto Equip Full UI")]
    public GameObject autoEquipFullPanel;
    public Button autoEquipFullConfirmBtn;
    public Button autoEquipFullCancelBtn;

    private ShopConfig currentConfig;
    private RestUIManager restUIManager;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Awake()
    {
        if (closeBtn) closeBtn.onClick.AddListener(CloseList);
        if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(false);
        if (autoEquipFullPanel) autoEquipFullPanel.SetActive(false);
    }

    // ==========================================
    // Public Methods - Initialization
    // ==========================================

    public void Init(ShopConfig config, RestUIManager manager)
    {
        currentConfig = config;
        restUIManager = manager;
    }

    public void CloseList() 
    {
        if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(false);
        if (autoEquipFullPanel) autoEquipFullPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    // ==========================================
    // Public Methods - Dojo Actions
    // ==========================================

    public void OpenLearnSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式学习 (耗时1天)"; 
        if (refreshArea) refreshArea.SetActive(false);
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(true);
        RefreshGoldText();
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;

        // 常驻招式
        foreach (var skill in GameManager.Instance.currentDojoSkills)
        {
            bool isOwned = HasSkill(skill, profile);
            SkillSlot tempSlot = new SkillSlot { skillData = skill, level = 1, quantity = 1 };
            bool canAfford = !isOwned && profile.totalGold >= skill.price && profile.currentRestDays >= 1;

            CreateSkillUI(tempSlot, skill.price, "学习", canAfford, isOwned, permanentContentRoot, () =>
            {
                if (profile.currentRestDays >= 1 && profile.ConsumeGold(skill.price))
                {
                    profile.currentRestDays -= 1;
                    TryAutoEquipSkill(skill, profile, () => restUIManager.RefreshPlayerStatusUI());
                    Debug.Log($"购买并尝试装备招式: {skill.skillName}");
                    OpenLearnSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }

        // 随机招式
        foreach (var skill in GameManager.Instance.randDojoSkills)
        {
            bool isOwned = HasSkill(skill, profile);
            SkillSlot tempSlot = new SkillSlot { skillData = skill, level = 1, quantity = 1 };
            bool canAfford = !isOwned && profile.totalGold >= skill.price && profile.currentRestDays >= 1;

            CreateSkillUI(tempSlot, skill.price, "学习", canAfford, isOwned, randomContentRoot, () =>
            {
                if (profile.currentRestDays >= 1 && profile.ConsumeGold(skill.price))
                {
                    profile.currentRestDays -= 1;
                    TryAutoEquipSkill(skill, profile, () => restUIManager.RefreshPlayerStatusUI());
                    Debug.Log($"购买并尝试装备招式(随机): {skill.skillName}");
                    OpenLearnSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }

        ForceRefreshLayout();
    }

    public void OpenUpgradeSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式进阶 (Lv.1 -> Lv.2) (耗时1天)"; 
        if (refreshArea) refreshArea.SetActive(false);
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(false);
        RefreshGoldText();
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;
        var lv1Skills = GetOwnedSkillsOfLevel(1, profile);

        foreach (var slot in lv1Skills)
        {
            int cost = slot.skillData.price;
            SkillSlot previewSlot = new SkillSlot { skillData = slot.skillData, level = 2, quantity = slot.quantity };
            bool canAfford = profile.totalGold >= cost && profile.currentRestDays >= 1;

            CreateSkillUI(previewSlot, cost, "进阶", canAfford, false, permanentContentRoot, () =>
            {
                if (profile.currentRestDays >= 1 && profile.ConsumeGold(cost))
                {
                    profile.currentRestDays -= 1;
                    slot.level = 2;
                    Debug.Log($"招式进阶成功: {slot.skillData.skillName}升至 Lv.2");
                    OpenUpgradeSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }

        ForceRefreshLayout();
    }

    public void OpenMasterSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式精通 (Lv.2 -> Lv.3) (耗时2天)"; 
        if (refreshArea) refreshArea.SetActive(false);
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(false);
        RefreshGoldText();
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;
        var lv2Skills = GetOwnedSkillsOfLevel(2, profile);

        foreach (var slot in lv2Skills)
        {
            int cost = slot.skillData.price * 2;
            SkillSlot previewSlot = new SkillSlot { skillData = slot.skillData, level = 3, quantity = slot.quantity };
            bool canAfford = profile.totalGold >= cost && profile.currentRestDays >= 2;

            CreateSkillUI(previewSlot, cost, "精通", canAfford, false, permanentContentRoot, () =>
            {
                if (profile.currentRestDays >= 2 && profile.ConsumeGold(cost))
                {
                    profile.currentRestDays -= 2;
                    slot.level = 3;
                    Debug.Log($"招式精通成功: {slot.skillData.skillName}升至 Lv.3");
                    OpenMasterSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }

        ForceRefreshLayout();
    }

    public void OpenShop(ShopCategory category)
    {
        gameObject.SetActive(true);
        titleText.text = $"商店 - {GetCategoryName(category)}";

        if (refreshArea)
        {
            refreshArea.SetActive(false); // 暂时隐藏刷新功能
        }
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(true);
        RefreshGoldText();
        ClearList();
        var profile = GameManager.Instance.playerProfile;

        // 加载装备或道具
        if (category == ShopCategory.Item)
        {
            foreach (var item in GameManager.Instance.permItems) CreateSkillShopUI(item, profile, permanentContentRoot, category);
            foreach (var item in GameManager.Instance.randItems) CreateSkillShopUI(item, profile, randomContentRoot, category);
        }
        else
        {
            List<EquipmentData> perm = new List<EquipmentData>();
            List<EquipmentData> rand = new List<EquipmentData>();

            switch (category)
            {
                case ShopCategory.Weapon:
                    perm = GameManager.Instance.permWeapons;
                    rand = GameManager.Instance.randWeapons;
                    break;
                case ShopCategory.Armor:
                    perm = GameManager.Instance.permArmors;
                    rand = GameManager.Instance.randArmors;
                    break;
                case ShopCategory.Accessory:
                    perm = GameManager.Instance.permAccessories;
                    rand = GameManager.Instance.randAccessories;
                    break;
            }

            foreach (var item in perm) CreateEquipShopUI(item, profile, permanentContentRoot, category);
            foreach (var item in rand) CreateEquipShopUI(item, profile, randomContentRoot, category);
        }

        ForceRefreshLayout();
    }

    private string GetCategoryName(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Weapon: return "武器";
            case ShopCategory.Armor: return "防具";
            case ShopCategory.Accessory: return "饰品";
            case ShopCategory.Item: return "道具";
            default: return "物品";
        }
    }

    private void RefreshGoldText()
    {
        if (playerGoldText != null && GameManager.Instance != null && GameManager.Instance.playerProfile != null)
        {
            playerGoldText.text = GameManager.Instance.playerProfile.totalGold.ToString();
        }
    }

    // ==========================================
    // Private Methods - UI Generation
    // ==========================================

    private void ClearList()
    {
        if (permanentContentRoot)
        {
            foreach (Transform child in permanentContentRoot) Destroy(child.gameObject);
        }
        if (randomContentRoot)
        {
            foreach (Transform child in randomContentRoot) Destroy(child.gameObject);
        }
    }

    private void CreateEquipShopUI(EquipmentData equip, PlayerProfile profile, Transform targetRoot, ShopCategory category)
    {
        bool isOwned = HasEquipment(equip, profile);
        bool canAfford = !isOwned && profile.totalGold >= equip.price;
        CreateEquipUI(equip, canAfford, isOwned, targetRoot, () =>
        {
            if (profile.ConsumeGold(equip.price))
            {
                TryAutoEquipEquipment(equip, profile, () => restUIManager.RefreshPlayerStatusUI());
                OpenShop(category);
                restUIManager.RefreshPlayerStatusUI();
            }
        });
    }

    private void CreateSkillShopUI(SkillData skill, PlayerProfile profile, Transform targetRoot, ShopCategory category)
    {
        // 道具为消耗品可叠加购买，不判断「已拥有」，判断上限
        int currentCount = GetItemTotalQuantity(skill, profile);
        int maxCapacity = profile.GetMaxItemCapacity();

        bool canAfford = profile.totalGold >= skill.price && (skill.skillType != SkillType.Item || currentCount < maxCapacity);
        string btnText = (skill.skillType == SkillType.Item && currentCount >= maxCapacity) ? "已满" : "购买";

        SkillSlot tempSlot = new SkillSlot { skillData = skill, level = 1, quantity = 0 };
        CreateSkillUI(tempSlot, skill.price, btnText, canAfford, false, targetRoot, () =>
        {
            if (profile.ConsumeGold(skill.price))
            {
                TryAutoEquipSkill(skill, profile, () => restUIManager.RefreshPlayerStatusUI());
                OpenShop(category);
                restUIManager.RefreshPlayerStatusUI();
            }
        });
    }

    private int GetItemTotalQuantity(SkillData itemData, PlayerProfile profile)
    {
        int count = 0;
        foreach (var s in profile.equippedItems) { if (s != null && s.skillData == itemData) count += s.quantity; }
        foreach (var s in profile.storageSkillsAndItems) { if (s != null && s.skillData == itemData) count += s.quantity; }
        return count;
    }

    private void TryAutoEquipEquipment(EquipmentData equip, PlayerProfile profile, System.Action onEquipSuccess = null)
    {
        profile.storageEquipments.Add(equip);

        if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(true);

        if (autoEquipConfirmBtn != null) {
            autoEquipConfirmBtn.onClick.RemoveAllListeners();
            autoEquipConfirmBtn.onClick.AddListener(() => {
                if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(false);

                bool canEquip = false;
                int accIndex = -1;
                
                if (equip.equipType == EquipmentType.Weapon && profile.equippedWeapon == null) canEquip = true;
                else if (equip.equipType == EquipmentType.Armor && profile.equippedArmor == null) canEquip = true;
                else if (equip.equipType == EquipmentType.Accessory) 
                {
                    for(int i = 0; i < 3; i++) {
                        if (i >= profile.equippedAccessories.Count || profile.equippedAccessories[i] == null) {
                            accIndex = i;
                            break;
                        }
                    }
                    if (accIndex != -1) canEquip = true;
                }

                if (canEquip) {
                    profile.storageEquipments.Remove(equip);
                    if (equip.equipType == EquipmentType.Weapon) profile.equippedWeapon = equip;
                    else if (equip.equipType == EquipmentType.Armor) { profile.equippedArmor = equip; profile.currentExtraLife = equip.durability; }
                    else if (equip.equipType == EquipmentType.Accessory) {
                        while (profile.equippedAccessories.Count <= accIndex) profile.equippedAccessories.Add(null);
                        profile.equippedAccessories[accIndex] = equip;
                    }
                    onEquipSuccess?.Invoke();
                    Debug.Log($"已自动装备: {equip.equipName}");
                } else {
                    if (autoEquipFullPanel) autoEquipFullPanel.SetActive(true);
                }
            });
        }

        if (autoEquipCancelBtn != null) {
            autoEquipCancelBtn.onClick.RemoveAllListeners();
            autoEquipCancelBtn.onClick.AddListener(() => {
                if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(false);
                Debug.Log($"已放入仓库: {equip.equipName}");
            });
        }
        
        if (autoEquipFullConfirmBtn != null) {
            autoEquipFullConfirmBtn.onClick.RemoveAllListeners();
            autoEquipFullConfirmBtn.onClick.AddListener(() => {
                if (autoEquipFullPanel) autoEquipFullPanel.SetActive(false);
                CloseList();
                restUIManager.OnOpenRolePanelClicked();
            });
        }
        
        if (autoEquipFullCancelBtn != null) {
            autoEquipFullCancelBtn.onClick.RemoveAllListeners();
            autoEquipFullCancelBtn.onClick.AddListener(() => {
                if (autoEquipFullPanel) autoEquipFullPanel.SetActive(false);
                Debug.Log($"槽位已满，已放入仓库: {equip.equipName}");
            });
        }
    }

    private void TryAutoEquipSkill(SkillData skill, PlayerProfile profile, System.Action onEquipSuccess = null)
    {
        if (skill.skillType == SkillType.Item)
        {
            bool isAlreadyEquipped = false;
            foreach (var s in profile.equippedItems)
            {
                if (s != null && s.skillData == skill) 
                { 
                    isAlreadyEquipped = true;
                    break; 
                }
            }

            if (isAlreadyEquipped)
            {
                AddOrStackItem(skill, profile);
                onEquipSuccess?.Invoke();
                return;
            }
        }

        AddOrStackItem(skill, profile);

        if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(true);

        if (autoEquipConfirmBtn != null) {
            autoEquipConfirmBtn.onClick.RemoveAllListeners();
            autoEquipConfirmBtn.onClick.AddListener(() => {
                if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(false);
                
                bool canEquip = false;
                List<SkillSlot> targetList = null;
                int limit = 0;
                
                if (skill.skillType == SkillType.Item) {
                    targetList = profile.equippedItems; limit = 4;
                } else {
                    if (skill.skillType == SkillType.Attack) { targetList = profile.equippedAttackSkills; limit = 4; }
                    else if (skill.skillType == SkillType.Defend || skill.skillType == SkillType.Dodge) { targetList = profile.equippedDefendSkills; limit = 4; }
                    else if (skill.skillType == SkillType.Special) { targetList = profile.equippedSpecialSkills; limit = 4; }
                }

                int emptyIndex = -1;
                if (targetList != null) {
                    for(int i = 0; i < limit; i++) {
                        if (i >= targetList.Count || targetList[i] == null) {
                            emptyIndex = i;
                            break;
                        }
                    }
                    if (emptyIndex != -1) canEquip = true;
                }

                if (canEquip) {
                    SkillSlot foundInStorage = profile.storageSkillsAndItems.Find(s => s != null && s.skillData == skill);
                    if (foundInStorage != null) {
                        if (foundInStorage.quantity > 1) {
                            int maxCap = profile.GetMaxItemCapacity();
                            int moveQty = Mathf.Min(foundInStorage.quantity, maxCap);
                            foundInStorage.quantity -= moveQty;
                            
                            SkillSlot newSlot = new SkillSlot { skillData = skill, level = foundInStorage.level, quantity = moveQty };
                            while(targetList.Count <= emptyIndex) targetList.Add(null);
                            targetList[emptyIndex] = newSlot;

                            if (foundInStorage.quantity <= 0) profile.storageSkillsAndItems.Remove(foundInStorage);
                        } else {
                            profile.storageSkillsAndItems.Remove(foundInStorage);
                            while(targetList.Count <= emptyIndex) targetList.Add(null);
                            targetList[emptyIndex] = foundInStorage;
                        }
                    }
                    onEquipSuccess?.Invoke();
                    Debug.Log($"已自动装备: {skill.skillName}");
                } else {
                    if (autoEquipFullPanel) autoEquipFullPanel.SetActive(true);
                }
            });
        }

        if (autoEquipCancelBtn != null) {
            autoEquipCancelBtn.onClick.RemoveAllListeners();
            autoEquipCancelBtn.onClick.AddListener(() => {
                if (autoEquipConfirmPanel) autoEquipConfirmPanel.SetActive(false);
                Debug.Log($"已放入仓库: {skill.skillName}");
            });
        }

        if (autoEquipFullConfirmBtn != null) {
            autoEquipFullConfirmBtn.onClick.RemoveAllListeners();
            autoEquipFullConfirmBtn.onClick.AddListener(() => {
                if (autoEquipFullPanel) autoEquipFullPanel.SetActive(false);
                CloseList();
                restUIManager.OnOpenRolePanelClicked();
            });
        }

        if (autoEquipFullCancelBtn != null) {
            autoEquipFullCancelBtn.onClick.RemoveAllListeners();
            autoEquipFullCancelBtn.onClick.AddListener(() => {
                if (autoEquipFullPanel) autoEquipFullPanel.SetActive(false);
                Debug.Log($"槽位已满，已放入仓库: {skill.skillName}");
            });
        }
    }

    private void CreateEquipUI(EquipmentData equip, bool canAfford, bool isOwned, Transform targetRoot, System.Action onClick)
    {
        var go = Instantiate(shopEquipPrefab, targetRoot);
        var ui = go.GetComponent<EquipItemUI>();

        System.Action<EquipmentData> clickWrapper = (e) => onClick?.Invoke();
        ui.SetupForShop(equip, equip.price, canAfford, isOwned, clickWrapper);
    }

    private void CreateSkillUI(SkillSlot slot, int price, string btnText, bool canAfford, bool isOwned, Transform targetRoot, System.Action onClick)
    {
        var go = Instantiate(shopSkillPrefab, targetRoot);
        var ui = go.GetComponent<RoleSkillItemUI>();

        System.Action<SkillSlot> clickWrapper = (s) => onClick?.Invoke();
        ui.SetupForShop(slot, price, canAfford, isOwned, btnText, clickWrapper);
    }

    // ==========================================
    // Private Methods - Data Checks
    // ==========================================

    private bool HasSkill(SkillData skillData, PlayerProfile profile)
    {
        var allSlots = GetAllSkillSlots(profile);
        return allSlots.Any(s => s.skillData != null && s.skillData == skillData);
    }

    private bool HasEquipment(EquipmentData equipData, PlayerProfile profile)
    {
        if (profile.equippedWeapon == equipData || profile.equippedArmor == equipData) return true;
        if (profile.equippedAccessories.Contains(equipData)) return true;
        if (profile.storageEquipments.Contains(equipData)) return true;
        return false;
    }

    private List<SkillSlot> GetOwnedSkillsOfLevel(int level, PlayerProfile profile)
    {
        var allSlots = GetAllSkillSlots(profile);
        return allSlots.Where(s => s.skillData != null && s.level == level && s.skillData.skillType != SkillType.Item).ToList();
    }

    private List<SkillSlot> GetAllSkillSlots(PlayerProfile profile)
    {
        var list = new List<SkillSlot>();
        if (profile.equippedAttackSkills != null) list.AddRange(profile.equippedAttackSkills.Where(s => s != null));
        if (profile.equippedDefendSkills != null) list.AddRange(profile.equippedDefendSkills.Where(s => s != null));
        if (profile.equippedSpecialSkills != null) list.AddRange(profile.equippedSpecialSkills.Where(s => s != null));
        if (profile.storageSkillsAndItems != null) list.AddRange(profile.storageSkillsAndItems.Where(s => s != null));
        return list;
    }

    private void AddOrStackItem(SkillData itemData, PlayerProfile profile)
    {
        int maxCap = profile.GetMaxItemCapacity();

        foreach (var slot in profile.equippedItems)
        {
            if (slot != null && slot.skillData == itemData) 
            { 
                if (slot.quantity < maxCap) slot.quantity++; 
                return; 
            }
        }
            
        foreach (var slot in profile.storageSkillsAndItems)
        {
            if (slot != null && slot.skillData == itemData) 
            { 
                if (slot.quantity < maxCap) slot.quantity++; 
                return; 
            }
        }
            
        profile.storageSkillsAndItems.Add(new SkillSlot { skillData = itemData, level = 1, quantity = 1 });
    }

    // ==========================================
    // Private Methods - Layout
    // ==========================================

    private void ForceRefreshLayout()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(RefreshLayoutRoutine());
    }

    private IEnumerator RefreshLayoutRoutine()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;

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