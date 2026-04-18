using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ShopListUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Text titleText;
    public Transform contentRoot;

    [Header("预制体引用 (双轨制)")]
    public GameObject shopEquipPrefab; // 给装备用的简单 UI (挂载 ShopItemUI)
    public GameObject shopSkillPrefab; // 给招式用的复杂 UI (挂载 RoleSkillItemUI)

    public Button closeBtn;

    private ShopConfig currentConfig;
    private RestUIManager restUIManager;

    private void Awake()
    {
        if (closeBtn) closeBtn.onClick.AddListener(CloseList);
    }

    public void Init(ShopConfig config, RestUIManager manager)
    {
        currentConfig = config;
        restUIManager = manager;
    }

    public void CloseList() => gameObject.SetActive(false);

    private void ClearList()
    {
        foreach (Transform child in contentRoot) Destroy(child.gameObject);
    }

    // ==========================================
    // 道场功能 (使用 shopSkillPrefab)
    // ==========================================

    public void OpenLearnSkill()
    {
        gameObject.SetActive(true); titleText.text = "道场 - 招式学习"; ClearList();
        var profile = GameManager.Instance.playerProfile;

        foreach (var skill in currentConfig.availableSkills)
        {
            if (HasSkill(skill, profile)) continue;

            // 构建一个临时的 Slot 用于展示
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
        gameObject.SetActive(true); titleText.text = "道场 - 招式进阶 (Lv.1 -> Lv.2)"; ClearList();
        var profile = GameManager.Instance.playerProfile;
        var lv1Skills = GetOwnedSkillsOfLevel(1, profile);

        foreach (var slot in lv1Skills)
        {
            int cost = slot.skillData.price;
            // 传给面板前，虚拟地把等级+1，让玩家看到升级后的属性！
            SkillSlot previewSlot = new SkillSlot { skillData = slot.skillData, level = 2, quantity = slot.quantity };

            CreateSkillUI(previewSlot, cost, "进阶", () =>
            {
                if (profile.ConsumeGold(cost))
                {
                    slot.level = 2; // 修改真实数据
                    Debug.Log($"招式进阶成功: {slot.skillData.skillName} 升至 Lv.2");
                    OpenUpgradeSkill();
                    restUIManager.RefreshPlayerStatusUI();
                }
            });
        }
    }

    public void OpenMasterSkill()
    {
        gameObject.SetActive(true); titleText.text = "道场 - 招式精通 (Lv.2 -> Lv.3)"; ClearList();
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
    // 商店功能 (道具用 SkillPrefab, 装备用 EquipPrefab)
    // ==========================================

    public void OpenBuyEquipment()
    {
        gameObject.SetActive(true); titleText.text = "商店 - 购买装备"; ClearList();
        var profile = GameManager.Instance.playerProfile;

        foreach (var equip in currentConfig.availableEquipments)
        {
            if (HasEquipment(equip, profile)) continue;

            CreateEquipUI(equip.icon, equip.equipName, equip.description, equip.price, "购买", () =>
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
        gameObject.SetActive(true); titleText.text = "商店 - 购买道具"; ClearList();
        var profile = GameManager.Instance.playerProfile;

        foreach (var item in currentConfig.availableItems)
        {
            SkillSlot tempSlot = new SkillSlot { skillData = item, level = 1, quantity = 1 };
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
    // UI 生成器与数据查询
    // ==========================================

    // 生成简单装备 UI
    private void CreateEquipUI(Sprite icon, string name, string desc, int price, string btnText, System.Action onClick)
    {
        var go = Instantiate(shopEquipPrefab, contentRoot);
        var ui = go.GetComponent<ShopItemUI>();
        bool canAfford = GameManager.Instance.playerProfile.totalGold >= price;
        ui.Setup(icon, name, desc, price, btnText, canAfford, onClick);
    }

    // 生成复杂招式/道具 UI
    private void CreateSkillUI(SkillSlot slot, int price, string btnText, System.Action onClick)
    {
        var go = Instantiate(shopSkillPrefab, contentRoot);
        var ui = go.GetComponent<RoleSkillItemUI>();
        bool canAfford = GameManager.Instance.playerProfile.totalGold >= price;

        // 包装一层 Action 签名来适配 RoleSkillItemUI
        System.Action<SkillSlot> clickWrapper = (s) => onClick?.Invoke();

        ui.SetupForShop(slot, price, canAfford, btnText, clickWrapper);
    }

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
            if (slot != null && slot.skillData == itemData) { slot.quantity++; return; }
        foreach (var slot in profile.storageSkillsAndItems)
            if (slot != null && slot.skillData == itemData) { slot.quantity++; return; }
        profile.storageSkillsAndItems.Add(new SkillSlot { skillData = itemData, level = 1, quantity = 1 });
    }
}