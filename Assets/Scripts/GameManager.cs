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
        return (GetFinalVitality() / 4);  // 每4点活力 +1 生命恢复
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
        int cap = 3; // 初始上限为3
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

    [Header("主菜单 UI (Main Menu UI)")]
    public GameObject mainMenuPanel;
    public UnityEngine.UI.Button startNewGameBtn;
    public UnityEngine.UI.Button continueGameBtn;

    [Header("全局数据 (Global Data)")]
    public PlayerProfile playerProfile;
    public List<LevelData> allLevels;
    public TaskDatabase taskDatabase;

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

    [Header("任务系统 (Task System)")]
    [HideInInspector] public bool isDoingTask = false;
    [HideInInspector] public TaskDifficulty currentTaskDifficulty;

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
        InitializeDataDictionaries();

        if (startNewGameBtn != null)
        {
            startNewGameBtn.onClick.AddListener(() => {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                StartNewGame();
            });
        }
        if (continueGameBtn != null)
        {
            continueGameBtn.onClick.AddListener(() => {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                LoadGame();
            });
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            RefreshMainMenuButtons();
        }
        else
        {
            StartNewGame();
        }
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

        // 保证体力规则：
        if (playerProfile != null)
        {
            if (isDoingTask || currentNodeIndex == 0)
            {
                // 第一场战斗或任务战斗：体力直接全满
                playerProfile.currentStamina = playerProfile.GetFinalMaxStamina();
            }
            else
            {
                // 连战中的后续场次：延续上一场剩余体力，并根据自身的被动恢复属性进行恢复
                int recoverAmount = playerProfile.GetStaminaRecoverPerTurn();
                playerProfile.currentStamina = Mathf.Min(playerProfile.GetFinalMaxStamina(), playerProfile.currentStamina + recoverAmount);
                Debug.Log($"<color=cyan>[连战体力恢复] 延续上一场体力，加上被动恢复 {recoverAmount}，当前体力为：{playerProfile.currentStamina}/{playerProfile.GetFinalMaxStamina()}</color>");
            }
        }

        battleManager.gameObject.SetActive(true);
        battleManager.SetupNewBattle(playerProfile.playerRoleAsset, currentEnemyData);
    }

    public void StartTaskBattle(TaskDifficulty difficulty)
    {
        if (taskDatabase == null) { Debug.LogError("TaskDatabase is null!"); return; }
        if (playerProfile.currentRestDays < 1) { Debug.LogWarning("天数不足！"); return; }

        var config = taskDatabase.GetConfig(difficulty);
        if (config == null || config.enemyPool.Count == 0) { Debug.LogError($"Task config for {difficulty} is invalid!"); return; }

        isDoingTask = true;
        currentTaskDifficulty = difficulty;
        playerProfile.currentRestDays -= 1;

        // 随机选择一个敌人
        RoleData enemyData = config.enemyPool[Random.Range(0, config.enemyPool.Count)];
        
        // 准备单场战斗环境
        currentLevelEnemies = new List<RoleData> { enemyData };
        currentNodeIndex = 0;

        if (restUIManager != null)
        {
            if (restUIManager.transitionUI != null) restUIManager.transitionUI.ForceClear();
            restUIManager.ClosePanel();
        }
        StartCombatNode();
    }

    // ==========================================
    // Public Methods - Battle Callbacks
    // ==========================================

    public void OnBattleResolution(bool isWin)
    {
        if (isDoingTask)
        {
            EndTaskBattle(isWin);
            return;
        }

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
            ClearSave();
            if (battleManager != null && battleManager.gameDefeatPanel != null)
            {
                battleManager.gameObject.SetActive(true);
                battleManager.gameDefeatPanel.SetActive(true);
            }
            else
            {
                ReturnToMainMenu();
            }
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
        if (isDoingTask) EndTaskBattle(false);
        else AdvanceToNextNode();
    }

    private void EndTaskBattle(bool isWin)
    {
        isDoingTask = false;
        
        if (isWin)
        {
            SavePlayerBattleState();
            
            var config = taskDatabase.GetConfig(currentTaskDifficulty);
            int oldLevel = playerProfile.level;
            int oldExp = playerProfile.currentExp;

            playerProfile.totalGold += config.goldReward;
            playerProfile.AddExp(config.expReward);

            // 抽取随机奖励
            LevelExtraRewardEntry extra = null;
            if (config.rewardPool != null && config.rewardPool.Count > 0)
            {
                extra = config.rewardPool[Random.Range(0, config.rewardPool.Count)];
                AwardLevelReward(extra);
            }

            if (battleResultUI != null)
            {
                battleResultUI.ShowResult(oldLevel, oldExp, config.expReward, config.goldReward, extra);
            }
            else
            {
                EnterRestUI();
            }
        }
        else
        {
            // 失败：保留1点生命回到休息区
            playerProfile.currentHp = 1;
            playerProfile.currentStamina = 0;
            Debug.Log("任务失败：玩家重伤返回休息区。");
            EnterRestUI();
        }
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
            AwardLevelReward(extra);
        }

        // 推进大关卡索引
        currentMainLevelIndex++;
        currentNodeIndex = 0;

        if (currentMainLevelIndex >= allLevels.Count)
        {
            // 通过所有关卡，游戏胜利！
            ClearSave();
            if (battleResultUI != null)
            {
                battleResultUI.continueBtn.onClick.RemoveAllListeners();
                battleResultUI.continueBtn.onClick.AddListener(() => {
                    battleResultUI.gameObject.SetActive(false);
                    if (battleManager != null && battleManager.gameVictoryPanel != null)
                    {
                        battleManager.gameObject.SetActive(true);
                        battleManager.gameVictoryPanel.SetActive(true);
                    }
                    else
                    {
                        ReturnToMainMenu();
                    }
                });
                battleResultUI.ShowResult(oldLevel, oldExp, finalExp, finalGold, extra);
            }
            else
            {
                if (battleManager != null && battleManager.gameVictoryPanel != null)
                {
                    battleManager.gameVictoryPanel.SetActive(true);
                }
                else
                {
                    ReturnToMainMenu();
                }
            }
            return;
        }

        // 否则进入下一个关卡的准备阶段（天数重置、敌人与商店预先生成）
        playerProfile.currentStamina = playerProfile.GetFinalMaxStamina();
        playerProfile.currentExtraLife = playerProfile.equippedArmor != null ? playerProfile.equippedArmor.durability : 0;
        playerProfile.currentRestDays = playerProfile.maxRestDays;

        RollEnemiesForCurrentLevel();
        RefreshShopAndDojo();

        // 此时通关并准备好下一关环境，立刻存档！
        SaveGame();

        if (battleResultUI != null)
        {
            battleResultUI.continueBtn.onClick.RemoveAllListeners();
            battleResultUI.continueBtn.onClick.AddListener(() => {
                battleResultUI.gameObject.SetActive(false);
                EnterRestFromBattleResult();
            });
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

    private void AwardLevelReward(LevelExtraRewardEntry extra)
    {
        if (extra == null) return;

        if (extra.rewardType == LevelExtraRewardEntry.RewardType.Equipment && extra.equipment != null)
        {
            EquipmentData equip = extra.equipment;
            bool equipped = false;

            if (equip.equipType == EquipmentType.Weapon && playerProfile.equippedWeapon == null)
            {
                playerProfile.equippedWeapon = equip;
                equipped = true;
            }
            else if (equip.equipType == EquipmentType.Armor && playerProfile.equippedArmor == null)
            {
                playerProfile.equippedArmor = equip;
                playerProfile.currentExtraLife = equip.durability;
                equipped = true;
            }
            else if (equip.equipType == EquipmentType.Accessory)
            {
                int emptyIdx = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (i >= playerProfile.equippedAccessories.Count || playerProfile.equippedAccessories[i] == null)
                    {
                        emptyIdx = i;
                        break;
                    }
                }
                if (emptyIdx != -1)
                {
                    while (playerProfile.equippedAccessories.Count <= emptyIdx) playerProfile.equippedAccessories.Add(null);
                    playerProfile.equippedAccessories[emptyIdx] = equip;
                    equipped = true;
                }
            }

            if (!equipped)
            {
                playerProfile.storageEquipments.Add(equip);
                Debug.Log($"<color=yellow>[奖励结算] 槽位已满或不匹配，{equip.equipName} 放入仓库</color>");
            }
            else
            {
                Debug.Log($"<color=lime>[奖励结算] 自动装备：{equip.equipName}</color>");
            }
        }
        else if (extra.rewardType == LevelExtraRewardEntry.RewardType.Item && extra.item != null)
        {
            SkillData item = extra.item;
            int qty = extra.quantity;
            int maxCap = playerProfile.GetMaxItemCapacity();

            SkillSlot existingEquip = playerProfile.equippedItems.Find(s => s != null && s.skillData == item);
            SkillSlot existingStorage = playerProfile.storageSkillsAndItems.Find(s => s != null && s.skillData == item);

            if (existingEquip != null)
            {
                existingEquip.quantity = Mathf.Min(existingEquip.quantity + qty, maxCap);
                Debug.Log($"<color=lime>[奖励结算] 已有装备道具叠加：{item.skillName} ×{qty}</color>");
            }
            else if (existingStorage != null)
            {
                existingStorage.quantity = Mathf.Min(existingStorage.quantity + qty, maxCap);
                Debug.Log($"<color=lime>[奖励结算] 已有仓库道具叠加：{item.skillName} ×{qty}</color>");
            }
            else
            {
                int emptyIdx = -1;
                for (int i = 0; i < 4; i++)
                {
                    // 检查 i 是否超出 Count，或者该位置为 null，或者该位置的 skillData 为空
                    if (i >= playerProfile.equippedItems.Count || playerProfile.equippedItems[i] == null || playerProfile.equippedItems[i].skillData == null)
                    {
                        emptyIdx = i;
                        break;
                    }
                }

                if (emptyIdx != -1)
                {
                    while (playerProfile.equippedItems.Count <= emptyIdx) playerProfile.equippedItems.Add(null);
                    playerProfile.equippedItems[emptyIdx] = new SkillSlot { skillData = item, level = 1, quantity = Mathf.Min(qty, maxCap) };
                    Debug.Log($"<color=lime>[奖励结算] 自动装备道具到槽位{emptyIdx + 1}：{item.skillName}</color>");
                }
                else
                {
                    playerProfile.storageSkillsAndItems.Add(new SkillSlot { skillData = item, level = 1, quantity = Mathf.Min(qty, maxCap) });
                    Debug.Log($"<color=yellow>[奖励结算] 道具槽已满，{item.skillName} 放入仓库</color>");
                }
            }
        }
    }

    public void EnterLevelNodeUIFromRest()
    {
        EnterLevelNodeUI();
    }

    // ==========================================
    // Save / Load System & Dictionaries Registry
    // ==========================================

    private Dictionary<string, EquipmentData> allEquipsDict = new Dictionary<string, EquipmentData>();
    private Dictionary<string, SkillData> allSkillsDict = new Dictionary<string, SkillData>();
    private Dictionary<string, RoleData> allRolesDict = new Dictionary<string, RoleData>();

    public void InitializeDataDictionaries()
    {
        allEquipsDict.Clear();
        allSkillsDict.Clear();
        allRolesDict.Clear();

        if (playerProfile != null)
        {
            RegisterRole(playerProfile.playerRoleAsset);
            RegisterEquip(playerProfile.equippedWeapon);
            RegisterEquip(playerProfile.equippedArmor);
            if (playerProfile.equippedAccessories != null)
            {
                foreach (var acc in playerProfile.equippedAccessories) RegisterEquip(acc);
            }
            if (playerProfile.storageEquipments != null)
            {
                foreach (var eq in playerProfile.storageEquipments) RegisterEquip(eq);
            }
            RegisterSkillSlotList(playerProfile.equippedAttackSkills);
            RegisterSkillSlotList(playerProfile.equippedDefendSkills);
            RegisterSkillSlotList(playerProfile.equippedSpecialSkills);
            RegisterSkillSlotList(playerProfile.equippedItems);
            RegisterSkillSlotList(playerProfile.storageSkillsAndItems);
        }

        if (allLevels != null)
        {
            foreach (var lvl in allLevels)
            {
                if (lvl == null) continue;
                RegisterRewardList(lvl.extraRewardPool);
            }
        }

        if (enemyDatabase != null && enemyDatabase.tiers != null)
        {
            foreach (var tier in enemyDatabase.tiers)
            {
                if (tier != null) RegisterRoleList(tier.enemies);
            }
        }

        if (taskDatabase != null)
        {
            foreach (TaskDifficulty diff in System.Enum.GetValues(typeof(TaskDifficulty)))
            {
                var cfg = taskDatabase.GetConfig(diff);
                if (cfg != null)
                {
                    RegisterRoleList(cfg.enemyPool);
                    RegisterRewardList(cfg.rewardPool);
                }
            }
        }

        if (restUIManager != null && restUIManager.currentShopConfig != null)
        {
            var shop = restUIManager.currentShopConfig;
            if (shop.weaponShop != null)
            {
                RegisterEquipList(shop.weaponShop.permanentEquips);
                RegisterEquipList(shop.weaponShop.randomEquipsPool);
            }
            if (shop.armorShop != null)
            {
                RegisterEquipList(shop.armorShop.permanentEquips);
                RegisterEquipList(shop.armorShop.randomEquipsPool);
            }
            if (shop.accessoryShop != null)
            {
                RegisterEquipList(shop.accessoryShop.permanentEquips);
                RegisterEquipList(shop.accessoryShop.randomEquipsPool);
            }
            if (shop.itemShop != null)
            {
                RegisterSkillList(shop.itemShop.permanentItems);
                RegisterSkillList(shop.itemShop.randomItemsPool);
            }
            if (shop.dojoSkillShop != null)
            {
                RegisterSkillList(shop.dojoSkillShop.permanentSkills);
                RegisterSkillList(shop.dojoSkillShop.randomSkillsPool);
            }
        }
    }

    private void RegisterEquip(EquipmentData eq)
    {
        if (eq != null && !string.IsNullOrEmpty(eq.name) && !allEquipsDict.ContainsKey(eq.name))
            allEquipsDict[eq.name] = eq;
    }

    private void RegisterSkill(SkillData sk)
    {
        if (sk != null && !string.IsNullOrEmpty(sk.name) && !allSkillsDict.ContainsKey(sk.name))
            allSkillsDict[sk.name] = sk;
    }

    private void RegisterRole(RoleData rd)
    {
        if (rd != null && !string.IsNullOrEmpty(rd.name) && !allRolesDict.ContainsKey(rd.name))
        {
            allRolesDict[rd.name] = rd;
            if (rd.npcSkills != null)
            {
                foreach (var skillCfg in rd.npcSkills)
                {
                    if (skillCfg != null && skillCfg.skillSlot != null) 
                        RegisterSkill(skillCfg.skillSlot.skillData);
                }
            }
        }
    }

    private void RegisterEquipList(List<EquipmentData> list)
    {
        if (list == null) return;
        foreach (var eq in list) RegisterEquip(eq);
    }

    private void RegisterSkillList(List<SkillData> list)
    {
        if (list == null) return;
        foreach (var sk in list) RegisterSkill(sk);
    }

    private void RegisterRoleList(List<RoleData> list)
    {
        if (list == null) return;
        foreach (var rd in list) RegisterRole(rd);
    }

    private void RegisterSkillSlotList(List<SkillSlot> list)
    {
        if (list == null) return;
        foreach (var slot in list)
        {
            if (slot != null) RegisterSkill(slot.skillData);
        }
    }

    private void RegisterRewardList(List<LevelExtraRewardEntry> list)
    {
        if (list == null) return;
        foreach (var entry in list)
        {
            if (entry == null) continue;
            if (entry.rewardType == LevelExtraRewardEntry.RewardType.Equipment) RegisterEquip(entry.equipment);
            else if (entry.rewardType == LevelExtraRewardEntry.RewardType.Item) RegisterSkill(entry.item);
        }
    }

    private List<SkillSlotSaveData> ToSaveDataList(List<SkillSlot> slots)
    {
        var list = new List<SkillSlotSaveData>();
        if (slots == null) return list;
        foreach (var slot in slots)
        {
            if (slot == null)
            {
                list.Add(null);
                continue;
            }
            list.Add(new SkillSlotSaveData
            {
                skillDataName = slot.skillData != null ? slot.skillData.name : "",
                level = slot.level,
                quantity = slot.quantity
            });
        }
        return list;
    }

    private List<SkillSlot> FromSaveDataList(List<SkillSlotSaveData> saveDataList)
    {
        var list = new List<SkillSlot>();
        if (saveDataList == null) return list;
        foreach (var sd in saveDataList)
        {
            if (sd == null || string.IsNullOrEmpty(sd.skillDataName))
            {
                list.Add(null);
                continue;
            }
            allSkillsDict.TryGetValue(sd.skillDataName, out var skill);
            list.Add(new SkillSlot
            {
                skillData = skill,
                level = sd.level,
                quantity = sd.quantity
            });
        }
        return list;
    }

    private LevelExtraRewardSaveData ToSaveData(LevelExtraRewardEntry entry)
    {
        if (entry == null) return null;
        return new LevelExtraRewardSaveData
        {
            rewardType = (int)entry.rewardType,
            assetName = entry.rewardType == LevelExtraRewardEntry.RewardType.Equipment 
                ? (entry.equipment != null ? entry.equipment.name : "")
                : (entry.item != null ? entry.item.name : ""),
            quantity = entry.quantity
        };
    }

    private LevelExtraRewardEntry FromSaveData(LevelExtraRewardSaveData sd)
    {
        if (sd == null || string.IsNullOrEmpty(sd.assetName)) return null;
        var entry = new LevelExtraRewardEntry
        {
            rewardType = (LevelExtraRewardEntry.RewardType)sd.rewardType,
            quantity = sd.quantity
        };
        if (entry.rewardType == LevelExtraRewardEntry.RewardType.Equipment)
        {
            allEquipsDict.TryGetValue(sd.assetName, out var eq);
            entry.equipment = eq;
        }
        else
        {
            allSkillsDict.TryGetValue(sd.assetName, out var sk);
            entry.item = sk;
        }
        return entry;
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        data.currentMainLevelIndex = currentMainLevelIndex;
        data.currentNodeIndex = currentNodeIndex;
        data.isReturnedFromLevelSelect = isReturnedFromLevelSelect;

        data.level = playerProfile.level;
        data.currentExp = playerProfile.currentExp;
        data.unallocatedPoints = playerProfile.unallocatedPoints;
        data.baseMaxLife = playerProfile.baseMaxLife;
        data.baseMaxStamina = playerProfile.baseMaxStamina;
        data.vitality = playerProfile.vitality;
        data.endurance = playerProfile.endurance;
        data.baseStrength = playerProfile.baseStrength;
        data.baseMentality = playerProfile.baseMentality;
        data.currentHp = playerProfile.currentHp;
        data.currentStamina = playerProfile.currentStamina;
        data.currentExtraLife = playerProfile.currentExtraLife;
        data.totalGold = playerProfile.totalGold;
        data.currentRestDays = playerProfile.currentRestDays;
        data.maxRestDays = playerProfile.maxRestDays;
        data.hasMassageBuff = playerProfile.hasMassageBuff;

        data.playerRoleAssetName = playerProfile.playerRoleAsset != null ? playerProfile.playerRoleAsset.name : "";
        data.equippedWeaponName = playerProfile.equippedWeapon != null ? playerProfile.equippedWeapon.name : "";
        data.equippedArmorName = playerProfile.equippedArmor != null ? playerProfile.equippedArmor.name : "";

        if (playerProfile.equippedAccessories != null)
        {
            foreach (var acc in playerProfile.equippedAccessories)
                data.equippedAccessoriesNames.Add(acc != null ? acc.name : "");
        }

        data.equippedAttackSkills = ToSaveDataList(playerProfile.equippedAttackSkills);
        data.equippedDefendSkills = ToSaveDataList(playerProfile.equippedDefendSkills);
        data.equippedSpecialSkills = ToSaveDataList(playerProfile.equippedSpecialSkills);
        data.equippedItems = ToSaveDataList(playerProfile.equippedItems);

        if (playerProfile.storageEquipments != null)
        {
            foreach (var eq in playerProfile.storageEquipments)
                data.storageEquipmentsNames.Add(eq != null ? eq.name : "");
        }
        data.storageSkillsAndItems = ToSaveDataList(playerProfile.storageSkillsAndItems);

        if (currentGroupA != null)
        {
            foreach (var enemy in currentGroupA)
                data.currentGroupANames.Add(enemy != null ? enemy.name : "");
        }
        if (currentGroupB != null)
        {
            foreach (var enemy in currentGroupB)
                data.currentGroupBNames.Add(enemy != null ? enemy.name : "");
        }
        data.currentGroupAExtraReward = ToSaveData(currentGroupAExtraReward);
        data.currentGroupBExtraReward = ToSaveData(currentGroupBExtraReward);

        if (currentDojoSkills != null)
        {
            foreach (var sk in currentDojoSkills) data.currentDojoSkillsNames.Add(sk != null ? sk.name : "");
        }
        if (randDojoSkills != null)
        {
            foreach (var sk in randDojoSkills) data.randDojoSkillsNames.Add(sk != null ? sk.name : "");
        }

        if (permWeapons != null)
        {
            foreach (var eq in permWeapons) data.permWeaponsNames.Add(eq != null ? eq.name : "");
        }
        if (randWeapons != null)
        {
            foreach (var eq in randWeapons) data.randWeaponsNames.Add(eq != null ? eq.name : "");
        }

        if (permArmors != null)
        {
            foreach (var eq in permArmors) data.permArmorsNames.Add(eq != null ? eq.name : "");
        }
        if (randArmors != null)
        {
            foreach (var eq in randArmors) data.randArmorsNames.Add(eq != null ? eq.name : "");
        }

        if (permAccessories != null)
        {
            foreach (var eq in permAccessories) data.permAccessoriesNames.Add(eq != null ? eq.name : "");
        }
        if (randAccessories != null)
        {
            foreach (var eq in randAccessories) data.randAccessoriesNames.Add(eq != null ? eq.name : "");
        }

        if (permItems != null)
        {
            foreach (var sk in permItems) data.permItemsNames.Add(sk != null ? sk.name : "");
        }
        if (randItems != null)
        {
            foreach (var sk in randItems) data.randItemsNames.Add(sk != null ? sk.name : "");
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SwordMasterDuel_SaveData", json);
        PlayerPrefs.Save();
        Debug.Log("<color=lime>[GameManager] Game Saved Successfully!</color>");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey("SwordMasterDuel_SaveData"))
        {
            Debug.LogWarning("No save data found!");
            return;
        }

        InitializeDataDictionaries();

        string json = PlayerPrefs.GetString("SwordMasterDuel_SaveData");
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        currentMainLevelIndex = data.currentMainLevelIndex;
        currentNodeIndex = data.currentNodeIndex;
        isReturnedFromLevelSelect = data.isReturnedFromLevelSelect;

        playerProfile.level = data.level;
        playerProfile.currentExp = data.currentExp;
        playerProfile.unallocatedPoints = data.unallocatedPoints;
        playerProfile.baseMaxLife = data.baseMaxLife;
        playerProfile.baseMaxStamina = data.baseMaxStamina;
        playerProfile.vitality = data.vitality;
        playerProfile.endurance = data.endurance;
        playerProfile.baseStrength = data.baseStrength;
        playerProfile.baseMentality = data.baseMentality;
        playerProfile.currentHp = data.currentHp;
        playerProfile.currentStamina = data.currentStamina;
        playerProfile.currentExtraLife = data.currentExtraLife;
        playerProfile.totalGold = data.totalGold;
        playerProfile.currentRestDays = data.currentRestDays;
        playerProfile.maxRestDays = data.maxRestDays;
        playerProfile.hasMassageBuff = data.hasMassageBuff;

        allRolesDict.TryGetValue(data.playerRoleAssetName, out var roleAsset);
        playerProfile.playerRoleAsset = roleAsset;

        allEquipsDict.TryGetValue(data.equippedWeaponName, out var weapon);
        playerProfile.equippedWeapon = weapon;

        allEquipsDict.TryGetValue(data.equippedArmorName, out var armor);
        playerProfile.equippedArmor = armor;

        playerProfile.equippedAccessories.Clear();
        foreach (var accName in data.equippedAccessoriesNames)
        {
            if (string.IsNullOrEmpty(accName)) playerProfile.equippedAccessories.Add(null);
            else
            {
                allEquipsDict.TryGetValue(accName, out var acc);
                playerProfile.equippedAccessories.Add(acc);
            }
        }

        playerProfile.equippedAttackSkills = FromSaveDataList(data.equippedAttackSkills);
        playerProfile.equippedDefendSkills = FromSaveDataList(data.equippedDefendSkills);
        playerProfile.equippedSpecialSkills = FromSaveDataList(data.equippedSpecialSkills);
        playerProfile.equippedItems = FromSaveDataList(data.equippedItems);

        playerProfile.storageEquipments.Clear();
        foreach (var eqName in data.storageEquipmentsNames)
        {
            if (string.IsNullOrEmpty(eqName)) playerProfile.storageEquipments.Add(null);
            else
            {
                allEquipsDict.TryGetValue(eqName, out var eq);
                playerProfile.storageEquipments.Add(eq);
            }
        }
        playerProfile.storageSkillsAndItems = FromSaveDataList(data.storageSkillsAndItems);

        currentGroupA.Clear();
        foreach (var name in data.currentGroupANames)
        {
            if (string.IsNullOrEmpty(name)) currentGroupA.Add(null);
            else
            {
                allRolesDict.TryGetValue(name, out var role);
                currentGroupA.Add(role);
            }
        }

        currentGroupB.Clear();
        foreach (var name in data.currentGroupBNames)
        {
            if (string.IsNullOrEmpty(name)) currentGroupB.Add(null);
            else
            {
                allRolesDict.TryGetValue(name, out var role);
                currentGroupB.Add(role);
            }
        }

        currentGroupAExtraReward = FromSaveData(data.currentGroupAExtraReward);
        currentGroupBExtraReward = FromSaveData(data.currentGroupBExtraReward);

        currentDojoSkills.Clear();
        foreach (var name in data.currentDojoSkillsNames)
        {
            allSkillsDict.TryGetValue(name, out var sk);
            currentDojoSkills.Add(sk);
        }

        randDojoSkills.Clear();
        foreach (var name in data.randDojoSkillsNames)
        {
            allSkillsDict.TryGetValue(name, out var sk);
            randDojoSkills.Add(sk);
        }

        permWeapons.Clear();
        foreach (var name in data.permWeaponsNames)
        {
            allEquipsDict.TryGetValue(name, out var eq);
            permWeapons.Add(eq);
        }

        randWeapons.Clear();
        foreach (var name in data.randWeaponsNames)
        {
            allEquipsDict.TryGetValue(name, out var eq);
            randWeapons.Add(eq);
        }

        permArmors.Clear();
        foreach (var name in data.permArmorsNames)
        {
            allEquipsDict.TryGetValue(name, out var eq);
            permArmors.Add(eq);
        }

        randArmors.Clear();
        foreach (var name in data.randArmorsNames)
        {
            allEquipsDict.TryGetValue(name, out var eq);
            randArmors.Add(eq);
        }

        permAccessories.Clear();
        foreach (var name in data.permAccessoriesNames)
        {
            allEquipsDict.TryGetValue(name, out var eq);
            permAccessories.Add(eq);
        }

        randAccessories.Clear();
        foreach (var name in data.randAccessoriesNames)
        {
            allEquipsDict.TryGetValue(name, out var eq);
            randAccessories.Add(eq);
        }

        permItems.Clear();
        foreach (var name in data.permItemsNames)
        {
            allSkillsDict.TryGetValue(name, out var sk);
            permItems.Add(sk);
        }

        randItems.Clear();
        foreach (var name in data.randItemsNames)
        {
            allSkillsDict.TryGetValue(name, out var sk);
            randItems.Add(sk);
        }

        battleManager.gameObject.SetActive(false);
        levelUIManager.gameObject.SetActive(false);
        restUIManager.ShowPanel();

        Debug.Log("<color=lime>[GameManager] Game Loaded Successfully!</color>");
    }

    public bool HasSave()
    {
        return PlayerPrefs.HasKey("SwordMasterDuel_SaveData");
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey("SwordMasterDuel_SaveData");
        PlayerPrefs.Save();
    }

    public void RefreshMainMenuButtons()
    {
        if (continueGameBtn != null)
        {
            continueGameBtn.gameObject.SetActive(HasSave());
        }
    }

    public void ReturnToMainMenu()
    {
        if (battleManager != null)
        {
            battleManager.gameObject.SetActive(false);
            if (battleManager.gameVictoryPanel != null) battleManager.gameVictoryPanel.SetActive(false);
            if (battleManager.gameDefeatPanel != null) battleManager.gameDefeatPanel.SetActive(false);
        }
        if (levelUIManager != null) levelUIManager.gameObject.SetActive(false);
        if (restUIManager != null) restUIManager.gameObject.SetActive(false);
        if (battleResultUI != null) battleResultUI.gameObject.SetActive(false);
        if (intermediateResultUI != null) intermediateResultUI.SetActive(false);

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            RefreshMainMenuButtons();
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("SwordMaster/Clear Save Data")]
    public static void ClearSaveDataMenu()
    {
        PlayerPrefs.DeleteKey("SwordMasterDuel_SaveData");
        PlayerPrefs.Save();
        Debug.Log("<color=red>[Editor] Save data cleared successfully!</color>");
    }
#endif
}

[System.Serializable]
public class SaveData
{
    public int currentMainLevelIndex;
    public int currentNodeIndex;
    public bool isReturnedFromLevelSelect;

    public int level;
    public int currentExp;
    public int unallocatedPoints;
    public int baseMaxLife;
    public int baseMaxStamina;
    public int vitality;
    public int endurance;
    public int baseStrength;
    public int baseMentality;
    public int currentHp;
    public int currentStamina;
    public int currentExtraLife;
    public int totalGold;
    public int currentRestDays;
    public int maxRestDays;
    public bool hasMassageBuff;

    public string playerRoleAssetName;
    public string equippedWeaponName;
    public string equippedArmorName;
    public List<string> equippedAccessoriesNames = new List<string>();

    public List<SkillSlotSaveData> equippedAttackSkills = new List<SkillSlotSaveData>();
    public List<SkillSlotSaveData> equippedDefendSkills = new List<SkillSlotSaveData>();
    public List<SkillSlotSaveData> equippedSpecialSkills = new List<SkillSlotSaveData>();
    public List<SkillSlotSaveData> equippedItems = new List<SkillSlotSaveData>();

    public List<string> storageEquipmentsNames = new List<string>();
    public List<SkillSlotSaveData> storageSkillsAndItems = new List<SkillSlotSaveData>();

    public List<string> currentGroupANames = new List<string>();
    public List<string> currentGroupBNames = new List<string>();
    public LevelExtraRewardSaveData currentGroupAExtraReward;
    public LevelExtraRewardSaveData currentGroupBExtraReward;

    public List<string> currentDojoSkillsNames = new List<string>();
    public List<string> randDojoSkillsNames = new List<string>();

    public List<string> permWeaponsNames = new List<string>();
    public List<string> randWeaponsNames = new List<string>();
    public List<string> permArmorsNames = new List<string>();
    public List<string> randArmorsNames = new List<string>();
    public List<string> permAccessoriesNames = new List<string>();
    public List<string> randAccessoriesNames = new List<string>();
    public List<string> permItemsNames = new List<string>();
    public List<string> randItemsNames = new List<string>();
}

[System.Serializable]
public class SkillSlotSaveData
{
    public string skillDataName;
    public int level;
    public int quantity;
}

[System.Serializable]
public class LevelExtraRewardSaveData
{
    public int rewardType;
    public string assetName;
    public int quantity;
}
