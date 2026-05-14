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
        if (level >= 12) return;

        currentExp += amount;

        while (currentExp >= 100 && level < 12)
        {
            currentExp -= 100;
            level++;
            unallocatedPoints += 4;
            Debug.Log($"<color=lime>升级！当前等级 Lv.{level}，获得 4 点属性点！</color>");
        }

        if (level >= 12) currentExp = 0;
    }

    public int GetMaxLoad() => 10 + GetFinalEndurance() * 3;

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
        int total = baseMaxLife + GetFinalVitality() * 6;
        if (equippedWeapon != null) total += equippedWeapon.bonusLife;
        if (equippedArmor != null && currentExtraLife > 0) total += equippedArmor.bonusLife;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusLife;
        return total;
    }

    public int GetFinalMaxStamina()
    {
        int total = baseMaxStamina + Mathf.FloorToInt(GetFinalEndurance() / 4f);
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

    public float GetFinalHitBarSpeed()
    {
        int mentality = GetFinalMentality();
        float speedReduction = mentality * 0.03f;
        float mentalMultiplier = Mathf.Max(0.2f, 1.0f - speedReduction);
        return GlobalBattleRules.globalHitBarBaseSpeed * mentalMultiplier;
    }

    public float GetFinalHitBarSlowdown()
    {
        float loadMultiplier = 1.0f;
        var loadState = GetLoadWeightState();
        switch (loadState)
        {
            case GlobalBattleRules.LoadWeightState.Light: loadMultiplier = 1.3f; break;
            case GlobalBattleRules.LoadWeightState.Medium: loadMultiplier = 1.0f; break;
            case GlobalBattleRules.LoadWeightState.Heavy: loadMultiplier = 0.7f; break;
            case GlobalBattleRules.LoadWeightState.Extreme: loadMultiplier = 0.4f; break;
        }
        return GlobalBattleRules.globalHitBarBaseSlowdown * loadMultiplier;
    }

    public float GetWeaponAtkFactor()
    {
        return equippedWeapon != null ? equippedWeapon.atkFactor : 1.0f;
    }

    public int GetArmorExtraLife()
    {
        return equippedArmor != null ? equippedArmor.durability : 0;
    }

    public int GetHpRecoverPerTurn()
    {
        return (GetFinalVitality() / 4) * 2;  // 每4点活力 +2 生命恢复
    }

    public int GetStaminaRecoverPerTurn()
    {
        int baseRecover = playerRoleAsset != null ? playerRoleAsset.staminaRecoverPerTurn : 2;
        return baseRecover + Mathf.FloorToInt(GetFinalEndurance() / 8f);
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

    public bool HasSkillUpgradeEffect()
    {
        List<EquipmentData> equips = new List<EquipmentData>();
        if (equippedWeapon != null) equips.Add(equippedWeapon);
        if (equippedArmor != null && currentExtraLife > 0) equips.Add(equippedArmor);
        if (equippedAccessories != null) equips.AddRange(equippedAccessories);

        foreach (var eq in equips)
        {
            if (eq == null || eq.equipEffects == null) continue;
            foreach (var effect in eq.equipEffects)
            {
                if (effect is GlobalBattleRules.SkillUpgradeEquipEffect) return true;
            }
        }
        return false;
    }

    public int GetMaxItemCapacity()
    {
        int cap = 2; // 初始上限为2
        List<EquipmentData> equips = new List<EquipmentData>();
        if (equippedWeapon != null) equips.Add(equippedWeapon);
        if (equippedArmor != null && currentExtraLife > 0) equips.Add(equippedArmor);
        if (equippedAccessories != null) equips.AddRange(equippedAccessories);

        foreach (var eq in equips)
        {
            if (eq == null || eq.equipEffects == null) continue;
            foreach (var effect in eq.equipEffects)
            {
                if (effect is GlobalBattleRules.ItemCapacityEquipEffect capEffect)
                {
                    cap += capEffect.extraCapacity;
                }
            }
        }
        return cap;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("全局数据 (Global Data)")]
    public PlayerProfile playerProfile;
    public List<LevelData> allLevels;

    [Tooltip("按强度分级的全局敌人数据库")]
    public EnemyDifficultyDatabase enemyDatabase;

    [Header("核心管理器引用 (Managers Reference)")]
    public BattleManager battleManager;
    public LevelUIManager levelUIManager;
    public RestUIManager restUIManager;
    public BattleResultUI battleResultUI;
    public GameObject intermediateResultUI; // 过渡节点

    [Header("运行进度 (Runtime Progress)")]
    public int currentMainLevelIndex = 0;
    public int currentNodeIndex = 0;

    // 当前大关 AB 两组敌人（各3个）
    [HideInInspector] public List<RoleData> currentGroupA = new List<RoleData>();
    [HideInInspector] public List<RoleData> currentGroupB = new List<RoleData>();

    // 玩家选择后，当前进行中的组
    [HideInInspector] public List<RoleData> currentLevelEnemies = new List<RoleData>();
    [HideInInspector] public bool selectedGroupIsA = true;

    // 本关 AB 组各自抽到的额外奖励词条
    [HideInInspector] public LevelExtraRewardEntry currentGroupAExtraReward;
    [HideInInspector] public LevelExtraRewardEntry currentGroupBExtraReward;

    /// <summary>
    /// 玩家从关卡选择界面返回休息场景时为 true，
    /// 此时点击"继续"应回到关卡界面而非推进到下一关。
    /// </summary>
    [HideInInspector] public bool isReturnedFromLevelSelect = false;

    [HideInInspector] public List<SkillData> currentDojoSkills = new List<SkillData>();  // 常驻招式
    [HideInInspector] public List<SkillData> randDojoSkills   = new List<SkillData>();  // 随机招式

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

        // 移动端帧率设置：默认 Android 是 30fps，这里统一设为 60fps
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // 移动端关闭垂直同步，由 targetFrameRate 控制
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

        if (playerProfile.playerRoleAsset != null)
        {
            playerProfile.baseMaxLife = playerProfile.playerRoleAsset.maxBasicLife;
            playerProfile.baseMaxStamina = playerProfile.playerRoleAsset.maxStamina;
            playerProfile.vitality = playerProfile.playerRoleAsset.vitality;
            playerProfile.endurance = playerProfile.playerRoleAsset.endurance;
            playerProfile.baseStrength = playerProfile.playerRoleAsset.strength;
            playerProfile.baseMentality = playerProfile.playerRoleAsset.mentality;
        }
        else
        {
            playerProfile.baseMaxLife = 10;
            playerProfile.baseMaxStamina = 5;
            playerProfile.vitality = 0;
            playerProfile.endurance = 0;
            playerProfile.baseStrength = 0;
            playerProfile.baseMentality = 0;
        }

        playerProfile.currentHp = playerProfile.GetFinalMaxLife();
        playerProfile.currentStamina = playerProfile.GetFinalMaxStamina();
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

        // 因为在 OnBattleResolution 里已经拦截了最后一场去调用 EndCurrentLevelGroup，
        // 这里理论上只会是前两场继续
        if (currentNodeIndex < 3) StartCombatNode();
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

    /// <summary>
    /// 玩家在关卡UI选择A组或B组后调用，将对应组赋给 currentLevelEnemies 并开始第一场战斗。
    /// </summary>
    public void SelectGroupAndStartCombat(bool isGroupA)
    {
        selectedGroupIsA = isGroupA;
        currentLevelEnemies = isGroupA ? new List<RoleData>(currentGroupA) : new List<RoleData>(currentGroupB);
        currentNodeIndex = 0;

        levelUIManager.gameObject.SetActive(false);
        StartCombatNode();
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

    public void OnBattleResolution(bool isWin)
    {
        if (isWin)
        {
            SavePlayerBattleState();

            if (currentNodeIndex < 2)
            {
                // 还有下一场战斗，弹出过渡节点
                if (intermediateResultUI != null)
                {
                    intermediateResultUI.SetActive(true);
                }
                else
                {
                    AdvanceToNextNode();
                }
            }
            else
            {
                // 第3场打完，直接进入关卡结算
                EndCurrentLevelGroup();
            }
        }
        else
        {
            Debug.Log("Game Over! 玩家阵亡。");
        }
    }

    /// <summary>
    /// 供中间过渡节点的“继续”按钮调用，隐藏节点并开始下一场战斗。
    /// </summary>
    public void ContinueFromIntermediateNode()
    {
        if (intermediateResultUI != null) intermediateResultUI.SetActive(false);
        AdvanceToNextNode();
    }

    /// <summary>
    /// 供大结算弹窗（BattleResultUI）动画播完点击继续时调用，正式回到休息区。
    /// </summary>
    public void EnterRestFromBattleResult()
    {
        EnterRestUI();
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
        if (enemyDatabase == null)
        {
            Debug.LogError("[GameManager] EnemyDifficultyDatabase 未赋值！请在 Inspector 中配置。");
            currentGroupA = new List<RoleData>();
            currentGroupB = new List<RoleData>();
            currentLevelEnemies = new List<RoleData>();
            return;
        }

        LevelData currentLevel = allLevels[currentMainLevelIndex];
        currentGroupA = new List<RoleData>();
        currentGroupB = new List<RoleData>();

        // 生成 A 组（3个节点）
        for (int i = 0; i < 3; i++)
        {
            int difficulty = currentLevel.GetGroupANodeDifficulty(i);
            RoleData enemy = enemyDatabase.GetRandomEnemy(difficulty);
            if (enemy != null)
            {
                currentGroupA.Add(enemy);
                Debug.Log($"<color=orange>[A组 敌人生成] 节点{i + 1} 强度{difficulty} → {enemy.roleName}</color>");
            }
            else
            {
                Debug.LogError($"[GameManager] A组节点{i + 1} 强度{difficulty} 的敌人池为空！");
            }
        }

        // 生成 B 组（3个节点）
        for (int i = 0; i < 3; i++)
        {
            int difficulty = currentLevel.GetGroupBNodeDifficulty(i);
            RoleData enemy = enemyDatabase.GetRandomEnemy(difficulty);
            if (enemy != null)
            {
                currentGroupB.Add(enemy);
                Debug.Log($"<color=cyan>[B组 敌人生成] 节点{i + 1} 强度{difficulty} → {enemy.roleName}</color>");
            }
            else
            {
                Debug.LogError($"[GameManager] B组节点{i + 1} 强度{difficulty} 的敌人池为空！");
            }
        }

        // ── 为 AB 组各抽取额外奖励（保证不重复，池只有1项时相同）──
        RollExtraRewards(currentLevel);
    }

    /// <summary>
    /// 从当前关卡的额外奖励池中为 A 组、B 组各抽一项。
    /// A 先随机；B 从剩余中随机（池≤1项时两组相同）。
    /// </summary>
    private void RollExtraRewards(LevelData levelData)
    {
        currentGroupAExtraReward = null;
        currentGroupBExtraReward = null;

        var pool = levelData.extraRewardPool;
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning("[GameManager] 额外奖励池为空，本关无额外奖励。");
            return;
        }

        // A 组：从完整池随机
        int idxA = Random.Range(0, pool.Count);
        currentGroupAExtraReward = pool[idxA];

        // B 组：从剩余项目中随机（池只有1项则与 A 相同）
        if (pool.Count >= 2)
        {
            // 构建不包含 A 那项的候选列表
            List<LevelExtraRewardEntry> remaining = new List<LevelExtraRewardEntry>(pool);
            remaining.RemoveAt(idxA);
            int idxB = Random.Range(0, remaining.Count);
            currentGroupBExtraReward = remaining[idxB];
        }
        else
        {
            currentGroupBExtraReward = pool[0];
        }

        Debug.Log($"<color=orange>[奖励抽签] A组额外奖励: {currentGroupAExtraReward?.GetDisplayName() ?? "无"}</color>");
        Debug.Log($"<color=cyan>[奖励抽签] B组额外奖励: {currentGroupBExtraReward?.GetDisplayName() ?? "无"}</color>");
    }

    private void RefreshShopAndDojo()
    {
        if (restUIManager == null || restUIManager.currentShopConfig == null) return;
        var shopConfig = restUIManager.currentShopConfig;
        var dojo = shopConfig.dojoSkillShop;

        // 1. 道场常驻招式
        currentDojoSkills = dojo.permanentSkills != null
            ? new List<SkillData>(dojo.permanentSkills)
            : new List<SkillData>();

        // 2. 道场随机招式（每大关刷新，ensureDifferent = false，因为是整体重刷）
        randDojoSkills = RollRandomSkills(dojo.randomSkillsPool, randDojoSkills, dojo.randomCount, false);

        // 3. 刷新所有分类商店
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
        if (currentGroupA == null || currentGroupA.Count == 0) RollEnemiesForCurrentLevel();

        battleManager.gameObject.SetActive(false);
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        levelUIManager.UpdateAndShow(currentLevel, currentGroupA, currentGroupB,
                                     currentGroupAExtraReward, currentGroupBExtraReward);
    }

    private void EndCurrentLevelGroup()
    {
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        string groupLabel = selectedGroupIsA ? "A" : "B";

        int oldLevel = playerProfile.level;
        int oldExp = playerProfile.currentExp;

        // ── 基础奖励 ──
        int finalGold = currentLevel.baseGoldReward;
        int finalExp  = currentLevel.baseExpReward;

        int goldBonusPct = 0;
        int expBonusPct = 0;

        // 扫描饰品奖励加成
        if (playerProfile.equippedAccessories != null)
        {
            foreach (var acc in playerProfile.equippedAccessories)
            {
                if (acc == null || acc.equipEffects == null) continue;
                foreach (var effect in acc.equipEffects)
                {
                    if (effect is GlobalBattleRules.RewardModifierEquipEffect rewardMod)
                    {
                        if (rewardMod.isGold) goldBonusPct += rewardMod.bonusAmount;
                        else expBonusPct += rewardMod.bonusAmount;
                    }
                }
            }
        }

        if (goldBonusPct > 0)
        {
            finalGold += Mathf.RoundToInt(currentLevel.baseGoldReward * (goldBonusPct / 100f));
        }

        if (expBonusPct > 0)
        {
            finalExp += Mathf.RoundToInt(currentLevel.baseExpReward * (expBonusPct / 100f));
        }

        playerProfile.totalGold += finalGold;
        playerProfile.AddExp(finalExp);
        Debug.Log($"<color=lime>[关卡结算] 基础奖励：+{finalGold}金币 +{finalExp}经验</color>");

        // ── 额外奖励（本关已在生成时抽好的词条）──
        LevelExtraRewardEntry extra = selectedGroupIsA ? currentGroupAExtraReward : currentGroupBExtraReward;

        if (extra != null)
        {
            if (extra.rewardType == LevelExtraRewardEntry.RewardType.Equipment && extra.equipment != null)
            {
                playerProfile.storageEquipments.Add(extra.equipment);
                Debug.Log($"<color=yellow>[关卡结算] {groupLabel}组额外装备: {extra.equipment.equipName} → 仓库</color>");
            }
            else if (extra.rewardType == LevelExtraRewardEntry.RewardType.Item && extra.item != null)
            {
                int maxCap = playerProfile.GetMaxItemCapacity();
                SkillSlot existingEquip = playerProfile.equippedItems.Find(s => s != null && s.skillData == extra.item);
                SkillSlot existingStorage = playerProfile.storageSkillsAndItems.Find(s => s != null && s.skillData == extra.item);

                // 优先加到已装备的道具槽中
                if (existingEquip != null)
                {
                    existingEquip.quantity += extra.quantity;
                    if (existingEquip.quantity > maxCap) existingEquip.quantity = maxCap;
                }
                else if (existingStorage != null)
                {
                    existingStorage.quantity += extra.quantity;
                    if (existingStorage.quantity > maxCap) existingStorage.quantity = maxCap;
                }
                else
                {
                    int addQty = Mathf.Min(extra.quantity, maxCap);
                    // 如果装备栏未满则自动装备
                    if (playerProfile.equippedItems.Count < 4)
                    {
                        playerProfile.equippedItems.Add(new SkillSlot { skillData = extra.item, level = 1, quantity = addQty });
                    }
                    else
                    {
                        playerProfile.storageSkillsAndItems.Add(new SkillSlot { skillData = extra.item, level = 1, quantity = addQty });
                    }
                }
                Debug.Log($"<color=yellow>[关卡结算] {groupLabel}组额外道具: {extra.item.skillName} ×{extra.quantity}</color>");
            }
        }

        if (battleResultUI != null)
        {
            battleResultUI.ShowResult(oldLevel, oldExp, finalExp, finalGold, extra);
        }
        else
        {
            EnterRestUI();
        }
    }

    /// <summary>
    /// 由关卡选择界面的"返回休息"按钮调用（第一关不可用）。
    /// 标记来源为关卡界面，进入休息场景后"继续"时会回到当前关卡而非推进到下一关。
    /// </summary>
    public void EnterRestFromLevelUI()
    {
        isReturnedFromLevelSelect = true;
        EnterRestUI();
    }

    /// <summary>
    /// 由 RestUIManager 在 isReturnedFromLevelSelect=true 时调用：
    /// 清除标志位并重新显示当前关卡选择界面（不重新抽敌人和奖励）。
    /// </summary>
    public void ReturnToLevelUIFromRest()
    {
        isReturnedFromLevelSelect = false;
        battleManager.gameObject.SetActive(false);
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        levelUIManager.UpdateAndShow(currentLevel, currentGroupA, currentGroupB,
                                     currentGroupAExtraReward, currentGroupBExtraReward);
    }

    private void EnterRestUI()
    {
        battleManager.gameObject.SetActive(false);
        levelUIManager.gameObject.SetActive(false);
        restUIManager.ShowPanel();
    }
}
