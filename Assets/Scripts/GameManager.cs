using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    [Header("角色原始资产")]
    public RoleData playerRoleAsset;

    [Header("成长与属性 (Level & Attributes)")]
    public int level = 1;
    public int currentExp = 0;
    public int unallocatedPoints = 0;

    public int baseMaxLife;
    public int baseMaxStamina;
    public int vitality;
    public int endurance;
    public int baseStrength;
    public int baseMentality;

    [Header("战斗与实时资源 (Combat Resources)")]
    public int currentHp;
    public int currentStamina;
    public int currentExtraLife; // 护甲耐久度
    public int totalGold;

    [Header("休息场景 (Rest Scene)")]
    public int maxRestDays = 3;
    public int currentRestDays = 3;

    [Header("当前装备 (Equipped)")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public List<EquipmentData> equippedAccessories = new List<EquipmentData>();

    [Header("当前战斗配置 (Loadout)")]
    public List<SkillSlot> equippedAttackSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedDefendSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedSpecialSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedItems = new List<SkillSlot>();

    [Header("仓库 (Storage)")]
    public List<EquipmentData> storageEquipments = new List<EquipmentData>();
    public List<SkillSlot> storageSkillsAndItems = new List<SkillSlot>();

    [Header("场景 Buff")]
    public bool hasMassageBuff = false;

    // ==========================================
    // Public Methods
    // ==========================================

    public void AddExp(int amount)
    {
        if (level >= 10) return;

        currentExp += amount;

        while (currentExp >= 100 && level < 10)
        {
            currentExp -= 100;
            level++;
            unallocatedPoints += 4;
            Debug.Log($"<color=lime>升级！当前等级 Lv.{level}，获得 4 点属性点！</color>");
        }

        if (level >= 10) currentExp = 0;
    }

    public int GetMaxLoad() => 10 + GetFinalEndurance() * 2;

    public int GetCurrentLoadWeight()
    {
        int weight = 0;
        if (equippedWeapon != null) weight += equippedWeapon.weight;
        if (equippedArmor != null) weight += equippedArmor.weight;
        foreach (var acc in equippedAccessories) if (acc != null) weight += acc.weight;
        return weight;
    }

    public int GetFinalMaxLife()
    {
        int total = baseMaxLife + GetFinalVitality() * 5;
        if (equippedWeapon != null) total += equippedWeapon.bonusLife;
        if (equippedArmor != null && currentExtraLife > 0) total += equippedArmor.bonusLife;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusLife;
        return total;
    }

    public int GetFinalMaxStamina()
    {
        int total = baseMaxStamina + GetFinalEndurance() * 2;
        if (equippedWeapon != null) total += equippedWeapon.bonusStamina;
        if (equippedArmor != null && currentExtraLife > 0) total += equippedArmor.bonusStamina;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusStamina;
        return total;
    }

    public int GetFinalVitality()
    {
        int total = vitality;
        if (equippedWeapon != null) total += equippedWeapon.bonusVitality;
        if (equippedArmor != null && currentExtraLife > 0) total += equippedArmor.bonusVitality;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusVitality;
        return total;
    }

    public int GetFinalEndurance()
    {
        int total = endurance;
        if (equippedWeapon != null) total += equippedWeapon.bonusEndurance;
        if (equippedArmor != null && currentExtraLife > 0) total += equippedArmor.bonusEndurance;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusEndurance;
        return total;
    }

    public int GetFinalStrength()
    {
        int total = baseStrength;
        if (equippedWeapon != null) total += equippedWeapon.bonusStrength;
        if (equippedArmor != null && currentExtraLife > 0) total += equippedArmor.bonusStrength;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusStrength;
        return total;
    }

    public int GetFinalMentality()
    {
        int total = baseMentality;
        if (equippedWeapon != null) total += equippedWeapon.bonusMentality;
        if (equippedArmor != null && currentExtraLife > 0) total += equippedArmor.bonusMentality;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusMentality;
        return total;
    }

    public int GetHpRecoverPerTurn()
    {
        return Mathf.FloorToInt(GetFinalVitality() / 2f);
    }

    public int GetStaminaRecoverPerTurn()
    {
        int baseRecover = playerRoleAsset != null ? playerRoleAsset.staminaRecoverPerTurn : 2;
        return baseRecover + Mathf.FloorToInt(GetFinalMentality() / 6f);
    }

    public GlobalBattleRules.LoadWeightState GetLoadWeightState()
    {
        int currentLoad = GetCurrentLoadWeight();
        int maxLoad = GetMaxLoad();
        float ratio = maxLoad > 0 ? (float)currentLoad / maxLoad : 0f;

        if (ratio < 0.3f) return GlobalBattleRules.LoadWeightState.Light;
        if (ratio <= 1.0f) return GlobalBattleRules.LoadWeightState.Medium;
        if (ratio <= 1.5f) return GlobalBattleRules.LoadWeightState.Heavy;
        return GlobalBattleRules.LoadWeightState.Extreme;
    }

    public bool ConsumeGold(int amount)
    {
        if (totalGold >= amount) 
        { 
            totalGold -= amount; 
            return true; 
        }
        return false;
    }

    public bool ConsumeHpSafely(int amount)
    {
        if (currentHp > 1) 
        { 
            currentHp -= amount; 
            if (currentHp < 1) currentHp = 1; 
            return true; 
        }
        return false;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("全局数据 (Global Data)")]
    public PlayerProfile playerProfile;
    public List<LevelData> allLevels;

    [Header("核心管理器引用 (Managers Reference)")]
    public BattleManager battleManager;
    public LevelUIManager levelUIManager;
    public RestUIManager restUIManager;
    public BattleResultUI battleResultUI;

    [Header("运行进度 (Runtime Progress)")]
    public int currentMainLevelIndex = 0;
    public int currentNodeIndex = 0;
    
    [HideInInspector] 
    public List<RoleData> currentLevelEnemies = new List<RoleData>();

    [HideInInspector] public List<SkillData> currentDojoSkills = new List<SkillData>();

    // 武器商店 (运行时)
    [HideInInspector] public List<EquipmentData> permWeapons = new List<EquipmentData>();
    [HideInInspector] public List<EquipmentData> randWeapons = new List<EquipmentData>();
    // 防具商店 (运行时)
    [HideInInspector] public List<EquipmentData> permArmors = new List<EquipmentData>();
    [HideInInspector] public List<EquipmentData> randArmors = new List<EquipmentData>();
    // 饰品商店 (运行时)
    [HideInInspector] public List<EquipmentData> permAccessories = new List<EquipmentData>();
    [HideInInspector] public List<EquipmentData> randAccessories = new List<EquipmentData>();
    // 道具商店 (运行时)
    [HideInInspector] public List<SkillData> permItems = new List<SkillData>();
    [HideInInspector] public List<SkillData> randItems = new List<SkillData>();

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartNewGame();
    }

    // ==========================================
    // Public Methods - Game Flow
    // ==========================================

    public void StartNewGame()
    {
        playerProfile.level = 1;
        playerProfile.currentExp = 0;
        playerProfile.unallocatedPoints = 4;
        playerProfile.totalGold = 0;

        playerProfile.baseMaxLife = 10;
        playerProfile.baseMaxStamina = 5;
        playerProfile.vitality = 0;
        playerProfile.endurance = 0;
        playerProfile.baseStrength = 0;
        playerProfile.baseMentality = 0;

        playerProfile.currentHp = playerProfile.baseMaxLife;
        playerProfile.currentStamina = playerProfile.baseMaxStamina;
        playerProfile.currentExtraLife = playerProfile.equippedArmor != null ? playerProfile.equippedArmor.durability : 0;
        playerProfile.currentRestDays = playerProfile.maxRestDays;

        currentMainLevelIndex = 0;
        currentNodeIndex = 0;

        RollEnemiesForCurrentLevel();
        RefreshShopAndDojo();
        EnterLevelNodeUI();
    }

    public void AdvanceToNextNode()
    {
        currentNodeIndex++;

        if (currentNodeIndex >= 3) EnterRestUI();
        else StartCombatNode();
    }

    public void AdvanceToNextMainLevel()
    {
        currentMainLevelIndex++;
        currentNodeIndex = 0;

        if (currentMainLevelIndex >= allLevels.Count) return;

        // 进入新关卡时，回复100%体力，重置护甲耐久
        playerProfile.currentStamina = playerProfile.GetFinalMaxStamina();
        playerProfile.currentExtraLife = playerProfile.equippedArmor != null ? playerProfile.equippedArmor.durability : 0;
        playerProfile.currentRestDays = playerProfile.maxRestDays;

        RollEnemiesForCurrentLevel();
        RefreshShopAndDojo();
        EnterLevelNodeUI();
    }

    public void StartCombatNode()
    {
        if (currentLevelEnemies == null || currentNodeIndex >= currentLevelEnemies.Count)
        {
            Debug.LogError($"战斗加载失败：当前敌人列表为空或越界。currentNodeIndex: {currentNodeIndex}");
            return;
        }

        RoleData currentEnemyData = currentLevelEnemies[currentNodeIndex];

        battleManager.gameObject.SetActive(true);
        battleManager.SetupNewBattle(playerProfile.playerRoleAsset, currentEnemyData);
    }

    // ==========================================
    // Public Methods - Battle Callbacks
    // ==========================================

    public void OnBattleResolution(bool isWin, int goldReward = 0, int expReward = 0)
    {
        if (isWin)
        {
            int finalGold = goldReward;
            int finalExp = expReward;

            // 扫描饰品效果
            if (playerProfile.equippedAccessories != null)
            {
                foreach (var acc in playerProfile.equippedAccessories)
                {
                    if (acc == null || acc.equipEffects == null) continue;
                    foreach (var effect in acc.equipEffects)
                    {
                        if (effect is GlobalBattleRules.RewardModifierEquipEffect rewardMod)
                        {
                            if (rewardMod.isGold) finalGold += rewardMod.bonusAmount;
                            else finalExp += rewardMod.bonusAmount;
                        }
                    }
                }
            }

            playerProfile.totalGold += finalGold;
            playerProfile.AddExp(finalExp);
            
            SavePlayerBattleState();


            if (battleResultUI != null) battleResultUI.ShowResult(goldReward);
            else AdvanceToNextNode();
        }
        else
        {
            Debug.Log("Game Over! 玩家阵亡。");
        }
    }

    public void OnBattleRetreat()
    {
        SavePlayerBattleState();
        AdvanceToNextNode();
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void RollEnemiesForCurrentLevel()
    {
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        var validGroups = currentLevel.possibleGroups.FindAll(g => g.enemies != null && g.enemies.Count > 0);

        if (validGroups.Count > 0)
        {
            int randIndex = Random.Range(0, validGroups.Count);
            currentLevelEnemies = validGroups[randIndex].enemies;
            Debug.Log($"<color=orange>生成随机敌人组：{validGroups[randIndex].groupName}</color>");
        }
        else
        {
            Debug.LogError($"关卡 {currentLevel.levelTitle} 没有配置有效的敌人组！");
            currentLevelEnemies = new List<RoleData>();
        }
    }

    private void RefreshShopAndDojo()
    {
        currentDojoSkills.Clear();
        if (restUIManager == null || restUIManager.currentShopConfig == null) return;
        var shopConfig = restUIManager.currentShopConfig;

        // 1. 道场招式：随机8个
        if (shopConfig.availableSkills != null)
        {
            var skillsPool = new List<SkillData>(shopConfig.availableSkills);
            for (int i = 0; i < 8 && skillsPool.Count > 0; i++)
            {
                int idx = Random.Range(0, skillsPool.Count);
                currentDojoSkills.Add(skillsPool[idx]);
                skillsPool.RemoveAt(idx);
            }
        }

        // 2. 刷新所有分类商店
        RefreshAllCategorizedShops(false);
    }

    public bool ManualRefreshShop()
    {
        if (restUIManager == null || restUIManager.currentShopConfig == null) return false;
        var shopConfig = restUIManager.currentShopConfig;

        if (playerProfile.ConsumeGold(shopConfig.refreshCost))
        {
            RefreshAllCategorizedShops(true);
            return true;
        }
        return false;
    }

    private void RefreshAllCategorizedShops(bool ensureDifferent)
    {
        var config = restUIManager.currentShopConfig;

        // 武器
        permWeapons = new List<EquipmentData>(config.weaponShop.permanentEquips);
        randWeapons = RollRandomEquips(config.weaponShop.randomEquipsPool, randWeapons, config.weaponShop.randomCount, ensureDifferent);

        // 防具
        permArmors = new List<EquipmentData>(config.armorShop.permanentEquips);
        randArmors = RollRandomEquips(config.armorShop.randomEquipsPool, randArmors, config.armorShop.randomCount, ensureDifferent);

        // 饰品
        permAccessories = new List<EquipmentData>(config.accessoryShop.permanentEquips);
        randAccessories = RollRandomEquips(config.accessoryShop.randomEquipsPool, randAccessories, config.accessoryShop.randomCount, ensureDifferent);

        // 道具
        permItems = new List<SkillData>(config.itemShop.permanentItems);
        randItems = RollRandomSkills(config.itemShop.randomItemsPool, randItems, config.itemShop.randomCount, ensureDifferent);
        
        Debug.Log("所有分类商店商品已刷新 (直接类型)");
    }

    private List<EquipmentData> RollRandomEquips(List<EquipmentData> pool, List<EquipmentData> current, int count, bool ensureDifferent)
    {
        if (pool == null || pool.Count == 0) return new List<EquipmentData>();
        List<EquipmentData> workingPool = new List<EquipmentData>(pool);
        int finalCount = Mathf.Min(count, pool.Count);
        if (ensureDifferent && pool.Count > finalCount)
        {
            foreach (var item in current) workingPool.Remove(item);
        }
        List<EquipmentData> result = new List<EquipmentData>();
        for (int i = 0; i < finalCount && workingPool.Count > 0; i++)
        {
            int idx = Random.Range(0, workingPool.Count);
            result.Add(workingPool[idx]);
            workingPool.RemoveAt(idx);
        }
        return result;
    }

    private List<SkillData> RollRandomSkills(List<SkillData> pool, List<SkillData> current, int count, bool ensureDifferent)
    {
        if (pool == null || pool.Count == 0) return new List<SkillData>();
        List<SkillData> workingPool = new List<SkillData>(pool);
        int finalCount = Mathf.Min(count, pool.Count);
        if (ensureDifferent && pool.Count > finalCount)
        {
            foreach (var item in current) workingPool.Remove(item);
        }
        List<SkillData> result = new List<SkillData>();
        for (int i = 0; i < finalCount && workingPool.Count > 0; i++)
        {
            int idx = Random.Range(0, workingPool.Count);
            result.Add(workingPool[idx]);
            workingPool.RemoveAt(idx);
        }
        return result;
    }

    private void SavePlayerBattleState()
    {
        playerProfile.currentHp = battleManager.playerEntity.currentBasicLife;
        playerProfile.currentStamina = battleManager.playerEntity.currentStamina;
        playerProfile.currentExtraLife = battleManager.playerEntity.currentExtraLife;
    }

    private void EnterLevelNodeUI()
    {
        if (currentLevelEnemies == null || currentLevelEnemies.Count == 0) RollEnemiesForCurrentLevel();

        battleManager.gameObject.SetActive(false);
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        levelUIManager.UpdateAndShow(currentLevel, currentNodeIndex, currentLevelEnemies);
    }

    private void EnterRestUI()
    {
        battleManager.gameObject.SetActive(false);
        levelUIManager.gameObject.SetActive(false);
        restUIManager.ShowPanel();
    }
}
