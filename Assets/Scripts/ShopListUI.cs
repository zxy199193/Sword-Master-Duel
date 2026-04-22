using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ShopListUI : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;
    public Transform contentRoot;
    public Button closeBtn;

    [Header("Prefabs")]
    public GameObject shopEquipPrefab;
    public GameObject shopSkillPrefab;

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
        titleText.text = "道场 - 招式学习"; 
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;

        foreach (var skill in currentConfig.availableSkills)
        {
            if (HasSkill(skill, profile)) continue;

            SkillSlot tempSlot = new SkillSlot { skillData = skill, level = 1, quantity = 1 };
            CreateSkillUI(tempSlot, skill.price, "学习", () =>
            {
                if (profile.ConsumeGold(skill.price))
                {
                    profile.storageSkillsAndItems.Add(new SkillSlot { skillData = skill, level = 1, quantity = 1 });
                    Debug.Log($"学习了新招式: {skill.skillName}");
                    OpenLearnSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }
    }

    public void OpenUpgradeSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式进阶 (Lv.1 -> Lv.2)"; 
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;
        var lv1Skills = GetOwnedSkillsOfLevel(1, profile);

        foreach (var slot in lv1Skills)
        {
            int cost = slot.skillData.price;
            SkillSlot previewSlot = new SkillSlot { skillData = slot.skillData, level = 2, quantity = slot.quantity };

            CreateSkillUI(previewSlot, cost, "进阶", () =>
            {
                if (profile.ConsumeGold(cost))
                {
                    slot.level = 2;
                    Debug.Log($"招式进阶成功: {slot.skillData.skillName} 升至 Lv.2");
                    OpenUpgradeSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }
    }

    public void OpenMasterSkill()
    {
        gameObject.SetActive(true); 
        titleText.text = "道场 - 招式精通 (Lv.2 -> Lv.3)"; 
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;
        var lv2Skills = GetOwnedSkillsOfLevel(2, profile);

        foreach (var slot in lv2Skills)
        {
            int cost = slot.skillData.price * 2;
            SkillSlot previewSlot = new SkillSlot { skillData = slot.skillData, level = 3, quantity = slot.quantity };

            CreateSkillUI(previewSlot, cost, "精通", () =>
            {
                if (profile.ConsumeGold(cost))
                {
                    slot.level = 3;
                    Debug.Log($"招式精通成功: {slot.skillData.skillName} 升至 Lv.3");
                    OpenMasterSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }
    }

    // ==========================================
    // Public Methods - Shop Actions
    // ==========================================

    public void OpenBuyEquipment()
    {
        gameObject.SetActive(true); 
        titleText.text = "商店 - 购买装备"; 
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;

        foreach (var equip in currentConfig.availableEquipments)
        {
            if (HasEquipment(equip, profile)) continue;

            CreateEquipUI(equip, "购买", () =>
            {
                if (profile.ConsumeGold(equip.price))
                {
                    profile.storageEquipments.Add(equip);
                    Debug.Log($"购买了装备: {equip.equipName}");
                    OpenBuyEquipment();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }
    }

    public void OpenBuyItem()
    {
        gameObject.SetActive(true); 
        titleText.text = "商店 - 购买道具"; 
        ClearList();
        
        var profile = GameManager.Instance.playerProfile;

        foreach (var item in currentConfig.availableItems)
        {
            int ownedCount = 0;
            if (profile.equippedItems != null)
                ownedCount += profile.equippedItems.Where(s => s != null && s.skillData == item).Sum(s => s.quantity);
            if (profile.storageSkillsAndItems != null)
                ownedCount += profile.storageSkillsAndItems.Where(s => s != null && s.skillData == item).Sum(s => s.quantity);

            SkillSlot tempSlot = new SkillSlot { skillData = item, level = 1, quantity = ownedCount };

            CreateSkillUI(tempSlot, item.price, "购买", () =>
            {
                if (profile.ConsumeGold(item.price))
                {
                    AddOrStackItem(item, profile);
                    Debug.Log($"购买了道具: {item.skillName}");
                    OpenBuyItem();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }
    }

    // ==========================================
    // Private Methods - UI Generation
    // ==========================================

    private void ClearList()
    {
        foreach (Transform child in contentRoot) 
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateEquipUI(EquipmentData equip, string btnText, System.Action onClick)
    {
        var go = Instantiate(shopEquipPrefab, contentRoot);
        var ui = go.GetComponent<EquipItemUI>();
        bool canAfford = GameManager.Instance.playerProfile.totalGold >= equip.price;

        System.Action<EquipmentData> clickWrapper = (e) => onClick?.Invoke();
        ui.SetupForShop(equip, equip.price, canAfford, btnText, clickWrapper);
    }

    private void CreateSkillUI(SkillSlot slot, int price, string btnText, System.Action onClick)
    {
        var go = Instantiate(shopSkillPrefab, contentRoot);
        var ui = go.GetComponent<RoleSkillItemUI>();
        bool canAfford = GameManager.Instance.playerProfile.totalGold >= price;

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