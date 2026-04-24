using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ShopListUI : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;
    public Transform permanentContentRoot;
    public Transform randomContentRoot;
    public Button closeBtn;

    [Header("Prefabs")]
    public GameObject shopEquipPrefab;
    public GameObject shopSkillPrefab;

    [Header("Shop Refresh UI")]
    public GameObject refreshArea;
    public Button refreshBtn;
    public Text refreshCostText;

    private ShopConfig currentConfig;
    private RestUIManager restUIManager;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Awake()
    {
        if (closeBtn) closeBtn.onClick.AddListener(CloseList);
    }

    // ==========================================
    // Public Methods - Initialization
    // ==========================================

    public void Init(ShopConfig config, RestUIManager manager)
    {
        currentConfig = config;
        restUIManager = manager;
    }

    public void CloseList() => gameObject.SetActive(false);

    // ==========================================
    // Public Methods - Dojo Actions
    // ==========================================

    public void OpenLearnSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式学习 (耗时1天)"; 
        if (refreshArea) refreshArea.SetActive(false);
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(false);
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;

        foreach (var skill in GameManager.Instance.currentDojoSkills)
        {
            if (HasSkill(skill, profile)) continue;

            SkillSlot tempSlot = new SkillSlot { skillData = skill, level = 1, quantity = 1 };
            bool canAfford = profile.totalGold >= skill.price && profile.currentRestDays >= 1;

            CreateSkillUI(tempSlot, skill.price, "学习", canAfford, permanentContentRoot, () =>
            {
                if (profile.currentRestDays >= 1 && profile.ConsumeGold(skill.price))
                {
                    profile.currentRestDays -= 1;
                    TryAutoEquipSkill(skill, profile);
                    Debug.Log($"购买并尝试装备招式: {skill.skillName}");
                    OpenLearnSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }
    }

    public void OpenUpgradeSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式进阶 (Lv.1 -> Lv.2) (耗时1天)"; 
        if (refreshArea) refreshArea.SetActive(false);
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(false);
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;
        var lv1Skills = GetOwnedSkillsOfLevel(1, profile);

        foreach (var slot in lv1Skills)
        {
            int cost = slot.skillData.price;
            SkillSlot previewSlot = new SkillSlot { skillData = slot.skillData, level = 2, quantity = slot.quantity };
            bool canAfford = profile.totalGold >= cost && profile.currentRestDays >= 1;

            CreateSkillUI(previewSlot, cost, "进阶", canAfford, permanentContentRoot, () =>
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
    }

    public void OpenMasterSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式精通 (Lv.2 -> Lv.3) (耗时2天)"; 
        if (refreshArea) refreshArea.SetActive(false);
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(false);
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;
        var lv2Skills = GetOwnedSkillsOfLevel(2, profile);

        foreach (var slot in lv2Skills)
        {
            int cost = slot.skillData.price * 2;
            SkillSlot previewSlot = new SkillSlot { skillData = slot.skillData, level = 3, quantity = slot.quantity };
            bool canAfford = profile.totalGold >= cost && profile.currentRestDays >= 2;

            CreateSkillUI(previewSlot, cost, "精通", canAfford, permanentContentRoot, () =>
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
    }

    public void OpenShop(ShopCategory category)
    {
        gameObject.SetActive(true);
        titleText.text = $"商店 - {GetCategoryName(category)}";

        if (refreshArea)
        {
            refreshArea.SetActive(true);
            if (refreshCostText) refreshCostText.text = $"刷新耗费: {currentConfig.refreshCost}";
            if (refreshBtn)
            {
                refreshBtn.onClick.RemoveAllListeners();
                refreshBtn.onClick.AddListener(() =>
                {
                    if (GameManager.Instance.ManualRefreshShop())
                    {
                        OpenShop(category);
                        restUIManager.RefreshPlayerStatusUI();
                    }
                });
            }
        }
        if (randomContentRoot) randomContentRoot.gameObject.SetActive(true);

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
        if (HasEquipment(equip, profile)) return;
        bool canAfford = profile.totalGold >= equip.price;
        CreateEquipUI(equip, "购买", canAfford, targetRoot, () =>
        {
            if (profile.ConsumeGold(equip.price))
            {
                TryAutoEquipEquipment(equip, profile);
                OpenShop(category);
                restUIManager.RefreshPlayerStatusUI();
            }
        });
    }

    private void CreateSkillShopUI(SkillData skill, PlayerProfile profile, Transform targetRoot, ShopCategory category)
    {
        bool canAfford = profile.totalGold >= skill.price;
        SkillSlot tempSlot = new SkillSlot { skillData = skill, level = 1, quantity = 0 };
        CreateSkillUI(tempSlot, skill.price, "购买", canAfford, targetRoot, () =>
        {
            if (profile.ConsumeGold(skill.price))
            {
                TryAutoEquipSkill(skill, profile);
                OpenShop(category);
                restUIManager.RefreshPlayerStatusUI();
            }
        });
    }

    private void TryAutoEquipEquipment(EquipmentData equip, PlayerProfile profile)
    {
        bool equipped = false;
        if (equip.equipType == EquipmentType.Weapon && profile.equippedWeapon == null)
        {
            profile.equippedWeapon = equip;
            equipped = true;
        }
        else if (equip.equipType == EquipmentType.Armor && profile.equippedArmor == null)
        {
            profile.equippedArmor = equip;
            profile.currentExtraLife = equip.durability;
            equipped = true;
        }
        else if (equip.equipType == EquipmentType.Accessory && profile.equippedAccessories.Count < 3)
        {
            profile.equippedAccessories.Add(equip);
            equipped = true;
        }

        if (!equipped)
        {
            profile.storageEquipments.Add(equip);
            Debug.Log($"已放入仓库: {equip.equipName}");
        }
        else
        {
            Debug.Log($"已自动装备: {equip.equipName}");
        }
    }

    private void TryAutoEquipSkill(SkillData skill, PlayerProfile profile)
    {
        bool equipped = false;
        SkillSlot newSlot = new SkillSlot { skillData = skill, level = 1, quantity = 1 };

        if (skill.skillType == SkillType.Item)
        {
            // 道具：先尝试堆叠
            foreach (var s in profile.equippedItems)
            {
                if (s != null && s.skillData == skill) { s.quantity++; equipped = true; break; }
            }
            // 堆叠失败则看是否有空位
            if (!equipped && profile.equippedItems.Count < 4)
            {
                profile.equippedItems.Add(newSlot);
                equipped = true;
            }
        }
        else
        {
            // 招式类
            List<SkillSlot> targetList = null;
            int limit = 0;
            if (skill.skillType == SkillType.Attack) { targetList = profile.equippedAttackSkills; limit = 4; }
            else if (skill.skillType == SkillType.Defend || skill.skillType == SkillType.Dodge) { targetList = profile.equippedDefendSkills; limit = 2; }
            else if (skill.skillType == SkillType.Special) { targetList = profile.equippedSpecialSkills; limit = 1; }

            if (targetList != null && targetList.Count < limit)
            {
                targetList.Add(newSlot);
                equipped = true;
            }
        }

        if (!equipped)
        {
            // 放入仓库
            AddOrStackItem(skill, profile);
            Debug.Log($"已放入仓库: {skill.skillName}");
        }
        else
        {
            Debug.Log($"已自动装备: {skill.skillName}");
        }
    }

    private void CreateEquipUI(EquipmentData equip, string btnText, bool canAfford, Transform targetRoot, System.Action onClick)
    {
        var go = Instantiate(shopEquipPrefab, targetRoot);
        var ui = go.GetComponent<EquipItemUI>();

        System.Action<EquipmentData> clickWrapper = (e) => onClick?.Invoke();
        ui.SetupForShop(equip, equip.price, canAfford, btnText, clickWrapper);
    }

    private void CreateSkillUI(SkillSlot slot, int price, string btnText, bool canAfford, Transform targetRoot, System.Action onClick)
    {
        var go = Instantiate(shopSkillPrefab, targetRoot);
        var ui = go.GetComponent<RoleSkillItemUI>();

        System.Action<SkillSlot> clickWrapper = (s) => onClick?.Invoke();
        ui.SetupForShop(slot, price, canAfford, btnText, clickWrapper);
    }

    // ==========================================
    // Private Methods - Data Checks
    // ==========================================

    private bool HasSkill(SkillData skillData, PlayerProfile profile)
    {
        var allSlots = GetAllSkillSlots(profile);
        return allSlots.Any(s => s.skillData == skillData);
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
        return allSlots.Where(s => s.level == level && s.skillData.skillType != SkillType.Item).ToList();
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
        foreach (var slot in profile.equippedItems)
        {
            if (slot != null && slot.skillData == itemData) 
            { 
                slot.quantity++; 
                return; 
            }
        }
            
        foreach (var slot in profile.storageSkillsAndItems)
        {
            if (slot != null && slot.skillData == itemData) 
            { 
                slot.quantity++; 
                return; 
            }
        }
            
        profile.storageSkillsAndItems.Add(new SkillSlot { skillData = itemData, level = 1, quantity = 1 });
    }
}