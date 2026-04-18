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
    public int unallocatedPoints = 0; // 未分配的属性点

    // 以下为玩家通过升级加点后的真实基础面板（不包含装备加成）
    public int baseMaxLife;
    public int baseMaxStamina;
    public int baseStrength;
    public int baseMentality;

    [Header("战斗运行时资源 (跨关卡继承)")]
    public int currentHp;
    public int currentStamina;
    public int totalGold;

    [Header("当前穿戴装备 (Equipped)")]
    public EquipmentData equippedWeapon;
    public EquipmentData equippedArmor;
    public List<EquipmentData> equippedAccessories = new List<EquipmentData>(); // 默认可带2个

    [Header("当前战斗配置 (Loadout)")]
    public List<SkillSlot> equippedAttackSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedDefendSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedSpecialSkills = new List<SkillSlot>();
    public List<SkillSlot> equippedItems = new List<SkillSlot>();

    [Header("无限仓库 (Storage)")]
    public List<EquipmentData> storageEquipments = new List<EquipmentData>();
    public List<SkillSlot> storageSkillsAndItems = new List<SkillSlot>();

    // ==========================================
    // 在 PlayerProfile 的最下方，更新这些获取属性的方法
    // ==========================================

    public int GetMaxLoad()
    {
        return 10 + GetFinalMaxStamina() + GetFinalStrength();
    }

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

    /// <summary>
    /// 获取当前角色的负重状态
    /// </summary>
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

    [Header("Managers Reference")]
    public BattleManager battleManager;
    public LevelUIManager levelUIManager;
    public RestUIManager restUIManager;
    public BattleResultUI battleResultUI; // 【新增】：结算弹窗引用

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartNewGame()
    {
        // 1. 初始化等级与经验
        playerProfile.level = 1;
        playerProfile.currentExp = 0;
        playerProfile.unallocatedPoints = 0;
        playerProfile.totalGold = 0;

        // 2. 从 RoleData 读取初始白板属性
        playerProfile.baseMaxLife = playerProfile.playerRoleAsset.maxBasicLife;
        playerProfile.baseMaxStamina = playerProfile.playerRoleAsset.maxStamina;
        playerProfile.baseStrength = playerProfile.playerRoleAsset.strength;
        playerProfile.baseMentality = playerProfile.playerRoleAsset.mentality;

        // 3. 回满血量和体力
        playerProfile.currentHp = playerProfile.baseMaxLife;
        playerProfile.currentStamina = playerProfile.baseMaxStamina;

        currentMainLevelIndex = 0;
        currentNodeIndex = 0;

        EnterLevelNodeUI();
    }

    /// <summary>
    /// 接收 BattleEndState 的战斗结果
    /// </summary>
    public void OnBattleResolution(bool isWin, int goldReward = 0)
    {
        if (isWin)
        {
            playerProfile.totalGold += goldReward;
            SavePlayerBattleState();

            // 【核心修改】：不再直接推进节点，而是呼出结算弹窗！
            if (battleResultUI != null)
            {
                battleResultUI.ShowResult(goldReward);
            }
            else
            {
                AdvanceToNextNode(); // 防呆：如果没配弹窗就直接进
            }
        }
        else
        {
            Debug.Log("Game Over! 请重新开始游戏。");
        }
    }

    public void OnBattleRetreat()
    {
        SavePlayerBattleState();
        AdvanceToNextNode(); // 撤退没有奖励，直接无缝推进
    }

    private void SavePlayerBattleState()
    {
        playerProfile.currentHp = battleManager.playerEntity.currentBasicLife;
        playerProfile.currentStamina = battleManager.playerEntity.currentStamina;
    }

    /// <summary>
    /// 【修改可见性】：改为 public，供 BattleResultUI 点击继续时调用
    /// </summary>
    public void AdvanceToNextNode()
    {
        currentNodeIndex++;

        if (currentNodeIndex >= 3)
        {
            EnterRestUI();
        }
        else
        {
            // 【核心修改：无缝连战】：还没打完 3 个怪，直接刷下一个怪开打，不再退回关卡 UI
            StartCombatNode();
        }
    }

    public void AdvanceToNextMainLevel()
    {
        currentMainLevelIndex++;
        currentNodeIndex = 0;

        if (currentMainLevelIndex >= allLevels.Count) return;

        playerProfile.currentStamina = playerProfile.playerRoleAsset.maxStamina / 2;
        EnterLevelNodeUI();
    }

    // ==========================================
    // UI 层流转调度 
    // ==========================================

    private void EnterLevelNodeUI()
    {
        battleManager.gameObject.SetActive(false);
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        levelUIManager.UpdateAndShow(currentLevel, currentNodeIndex);
    }

    public void StartCombatNode()
    {
        LevelData currentLevel = allLevels[currentMainLevelIndex];
        RoleData currentEnemyData = currentLevel.enemies[currentNodeIndex];

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