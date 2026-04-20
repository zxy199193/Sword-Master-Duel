using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    [Header("角色原始资产")]
    public RoleData playerRoleAsset;

    [Header("成长属性 (Level & Attributes)")]
    public int level = 1;
    public int currentExp = 0;
    public int unallocatedPoints = 0;

    public int baseMaxLife;
    public int baseMaxStamina;
    public int baseStrength;
    public int baseMentality;

    [Header("战斗运行时资源 (跨关卡继承)")]
    public int currentHp;
    public int currentStamina;

    // 【核心修复 Bug 2】：跨局继承护盾！
    public int currentExtraLife;

    public int totalGold;

    [Header("当前穿戴装备 (Equipped)")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public List<EquipmentData> equippedAccessories = new List<EquipmentData>();

    [Header("当前战斗配置 (Loadout)")]
    public List<SkillSlot> equippedAttackSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedDefendSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedSpecialSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedItems = new List<SkillSlot>();

    [Header("无限仓库 (Storage)")]
    public List<EquipmentData> storageEquipments = new List<EquipmentData>();
    public List<SkillSlot> storageSkillsAndItems = new List<SkillSlot>();

    public void AddExp(int amount)
    {
        if (level >= 10) return;

        currentExp += amount;

        while (currentExp >= 100 && level < 10)
        {
            currentExp -= 100;
            level++;
            unallocatedPoints += 4;
            Debug.Log($"<color=lime>升级啦！当前等级：Lv.{level}，获得 4 点属性点！</color>");
        }

        if (level >= 10) currentExp = 0;
    }

    public int GetMaxLoad() => 10 + GetFinalMaxStamina() + GetFinalStrength();

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
        int total = baseMaxLife;
        if (equippedWeapon != null) total += equippedWeapon.bonusLife;
        if (equippedArmor != null) total += equippedArmor.bonusLife;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusLife;
        return total;
    }

    public int GetFinalMaxStamina()
    {
        int total = baseMaxStamina;
        if (equippedWeapon != null) total += equippedWeapon.bonusStamina;
        if (equippedArmor != null) total += equippedArmor.bonusStamina;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusStamina;
        return total;
    }

    public int GetFinalStrength()
    {
        int total = baseStrength;
        if (equippedWeapon != null) total += equippedWeapon.bonusStrength;
        if (equippedArmor != null) total += equippedArmor.bonusStrength;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusStrength;
        return total;
    }

    public int GetFinalMentality()
    {
        int total = baseMentality;
        if (equippedWeapon != null) total += equippedWeapon.bonusMentality;
        if (equippedArmor != null) total += equippedArmor.bonusMentality;
        foreach (var acc in equippedAccessories) if (acc != null) total += acc.bonusMentality;
        return total;
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

    [Header("跨场景 Buff")]
    public bool hasMassageBuff = false;

    public bool ConsumeGold(int amount)
    {
        if (totalGold >= amount) { totalGold -= amount; return true; }
        return false;
    }

    public bool ConsumeHpSafely(int amount)
    {
        if (currentHp > 1) { currentHp -= amount; if (currentHp < 1) currentHp = 1; return true; }
        return false;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Global Data")]
    public PlayerProfile playerProfile;
    public List<LevelData> allLevels;

    [Header("Runtime Progress")]
    public int currentMainLevelIndex = 0;
    public int currentNodeIndex = 0;
    [HideInInspector] public List<RoleData> currentLevelEnemies = new List<RoleData>();

    [Header("Managers Reference")]
    public BattleManager battleManager;
    public LevelUIManager levelUIManager;
    public RestUIManager restUIManager;
    public BattleResultUI battleResultUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        playerProfile.level = 1;
        playerProfile.currentExp = 0;
        playerProfile.unallocatedPoints = 4;
        playerProfile.totalGold = 0;

        playerProfile.baseMaxLife = 20;
        playerProfile.baseMaxStamina = 10;
        playerProfile.baseStrength = 0;
        playerProfile.baseMentality = 0;

        playerProfile.currentHp = playerProfile.baseMaxLife;
        playerProfile.currentStamina = playerProfile.baseMaxStamina;

        // 【核心修复 Bug 2】：游戏开始时给满护盾
        playerProfile.currentExtraLife = playerProfile.equippedArmor != null ? playerProfile.equippedArmor.durability : 0;

        currentMainLevelIndex = 0;
        currentNodeIndex = 0;

        RollEnemiesForCurrentLevel();
        EnterLevelNodeUI();
    }

    private void RollEnemiesForCurrentLevel()
    {
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        var validGroups = currentLevel.possibleGroups.FindAll(g => g.enemies != null && g.enemies.Count > 0);

        if (validGroups.Count > 0)
        {
            int randIndex = Random.Range(0, validGroups.Count);
            currentLevelEnemies = validGroups[randIndex].enemies;
            Debug.Log($"<color=orange>新关卡！随机抽中了敌人组：{validGroups[randIndex].groupName}</color>");
        }
        else
        {
            Debug.LogError($"关卡 {currentLevel.levelTitle} 没有配置有效的敌人组（或组内没拖入敌人）！");
            currentLevelEnemies = new List<RoleData>();
        }
    }

    public void OnBattleResolution(bool isWin, int goldReward = 0, int expReward = 0)
    {
        if (isWin)
        {
            playerProfile.totalGold += goldReward;
            playerProfile.AddExp(expReward);
            SavePlayerBattleState();

            if (battleResultUI != null) battleResultUI.ShowResult(goldReward);
            else AdvanceToNextNode();
        }
        else
        {
            Debug.Log("Game Over! 请重新开始游戏。");
        }
    }

    public void OnBattleRetreat()
    {
        SavePlayerBattleState();
        AdvanceToNextNode();
    }

    private void SavePlayerBattleState()
    {
        playerProfile.currentHp = battleManager.playerEntity.currentBasicLife;
        playerProfile.currentStamina = battleManager.playerEntity.currentStamina;

        // 【核心修复 Bug 2】：战斗结束时，将打剩下的护盾保存下来！
        playerProfile.currentExtraLife = battleManager.playerEntity.currentExtraLife;
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

        // 【核心修复 Bug 1】：进入下一大关时，根据最终属性点给一半体力（也可以自己改回全满）
        playerProfile.currentStamina = playerProfile.GetFinalMaxStamina() / 2;

        // 【核心修复 Bug 2】：进入新的一大关，护盾重新恢复到最大值！
        playerProfile.currentExtraLife = playerProfile.equippedArmor != null ? playerProfile.equippedArmor.durability : 0;

        RollEnemiesForCurrentLevel();
        EnterLevelNodeUI();
    }

    private void EnterLevelNodeUI()
    {
        if (currentLevelEnemies == null || currentLevelEnemies.Count == 0) RollEnemiesForCurrentLevel();

        battleManager.gameObject.SetActive(false);
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        levelUIManager.UpdateAndShow(currentLevel, currentNodeIndex, currentLevelEnemies);
    }

    public void StartCombatNode()
    {
        if (currentLevelEnemies == null || currentNodeIndex >= currentLevelEnemies.Count)
        {
            Debug.LogError($"开战失败！当前抽取的敌人列表为空，或超出了索引！currentNodeIndex: {currentNodeIndex}");
            return;
        }

        RoleData currentEnemyData = currentLevelEnemies[currentNodeIndex];

        battleManager.gameObject.SetActive(true);
        battleManager.SetupNewBattle(playerProfile.playerRoleAsset, currentEnemyData);
    }

    private void EnterRestUI()
    {
        battleManager.gameObject.SetActive(false);
        levelUIManager.gameObject.SetActive(false);
        restUIManager.ShowPanel();
    }
}