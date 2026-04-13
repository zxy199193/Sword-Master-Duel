using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Core Systems & Entities")]
    public HitBarManager hitBarManager;
    public BattleEntity playerEntity;
    public BattleEntity enemyEntity;

    [Header("UI References")]
    public ActionPanelUI actionPanelUI;
    public RoleInfoUI playerInfoUI;
    public RoleInfoUI enemyInfoUI;

    [Header("VFX & Prefabs")]
    public Canvas floatingTextCanvas;
    public GameObject normalDamagePrefab;
    public GameObject critDamagePrefab;
    public GameObject missPrefab;

    // ==========================================
    // 运行时状态 (Runtime State)
    // ==========================================
    private BattleState currentState;

    [HideInInspector] public List<HitSection?> currentHitResults = new List<HitSection?>();
    [HideInInspector] public bool currentAttackTimeout;
    [HideInInspector] public bool isPlayerAttacking;

    [HideInInspector] public SkillData currentPlayerSkill;
    [HideInInspector] public SkillData currentPlayerSubSkill;
    [HideInInspector] public SkillData currentEnemySkill;

    // ==========================================
    // Unity 生命周期
    // ==========================================
    private void Start()
    {
        ChangeState(new BattleInitState(this));
    }

    private void Update()
    {
        currentState?.Execute();
    }

    // ==========================================
    // 公共接口：状态机与战斗流转
    // ==========================================

    public void ChangeState(BattleState newState)
    {
        if (currentState != null) currentState.Exit();

        currentState = newState;
        Debug.Log($"<color=cyan>[BattleManager] 进入状态：{newState.GetType().Name}</color>");

        currentState.Enter();
    }

    public void OnPlayerSelectedAction() { } // 预留接口：玩家选中主技能时触发

    /// <summary>
    /// 玩家点击“准备完成”后的核心战斗流转逻辑
    /// </summary>
    public void OnPlayerActionConfirmed(SkillData mainSkill, SkillData subSkill)
    {
        // Phase 0: 数据重置与指令录入
        playerEntity.ResetTurnData();
        enemyEntity.ResetTurnData();

        currentPlayerSkill = mainSkill;
        currentPlayerSubSkill = subSkill;

        // TODO: AI 决策层 (目前为随机抽取主技能)
        currentEnemySkill = null;
        var enemySkills = enemyEntity.runtimeSkills;
        if (enemySkills != null && enemySkills.Count > 0)
        {
            currentEnemySkill = enemySkills[Random.Range(0, enemySkills.Count)];
        }

        Debug.Log($"<color=cyan>[Combat] 玩家亮出: 主[{currentPlayerSkill?.skillName}] 副[{currentPlayerSubSkill?.skillName}] | 敌人亮出: [{currentEnemySkill?.skillName}]</color>");

        // Phase 1: 扣除体力
        if (currentPlayerSkill != null) playerEntity.ConsumeStamina(currentPlayerSkill.staminaCost);
        if (currentPlayerSubSkill != null) playerEntity.ConsumeStamina(currentPlayerSubSkill.staminaCost);
        if (currentEnemySkill != null) enemyEntity.ConsumeStamina(currentEnemySkill.staminaCost);

        // Phase 2: 次级行为优先结算 (道具 / 特殊效果)
        ExecuteSecondaryAction(playerEntity, enemyEntity, currentPlayerSubSkill);
        ExecuteSecondaryAction(enemyEntity, playerEntity, currentEnemySkill);

        // Phase 3: 防守与闪避 Buff 挂载
        ApplyNonAttackSkills(playerEntity, currentPlayerSkill);
        ApplyNonAttackSkills(enemyEntity, currentEnemySkill);

        // Phase 4: 致命伤害拦截 (检查是否有实体被道具直接击杀)
        if (playerEntity.currentBasicLife <= 0 || enemyEntity.currentBasicLife <= 0)
        {
            Debug.Log("<color=red>[Combat] 触发致命一击，跳过攻击阶段！</color>");
            return; // TODO: 接入 BattleEndState
        }

        // Phase 5: 主要攻击行为状态分发
        if (currentPlayerSkill != null && currentPlayerSkill.skillType == SkillType.Attack)
        {
            ChangeState(new HitBarActionState(this));
        }
        else if (currentEnemySkill != null && currentEnemySkill.skillType == SkillType.Attack)
        {
            ChangeState(new EnemyActionState(this));
        }
        else
        {
            Debug.Log("[Combat] 双方均无攻击行为，回合结束。");
            playerEntity.RecoverStamina();
            enemyEntity.RecoverStamina();
            ChangeState(new PreparationState(this));
        }
    }

    // ==========================================
    // 公共接口：UI 与表现层
    // ==========================================

    public void SpawnDamagePopup(Vector3 targetPosition, string textContent, int hitLevel)
    {
        GameObject prefabToSpawn = normalDamagePrefab;
        if (hitLevel == 0) prefabToSpawn = missPrefab;
        else if (hitLevel >= 2) prefabToSpawn = critDamagePrefab;

        if (prefabToSpawn == null || floatingTextCanvas == null) return;

        Vector3 spawnPos = targetPosition + Vector3.up * 1.5f;
        GameObject popupObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, floatingTextCanvas.transform);

        DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
        if (popupScript != null) popupScript.Setup(textContent);
    }

    // ==========================================
    // 内部私有逻辑
    // ==========================================

    private void ApplyNonAttackSkills(BattleEntity entity, SkillData skill)
    {
        if (skill == null) return;

        if (skill.skillType == SkillType.Defend)
        {
            entity.tempDamageReduction = skill.basicDefend + entity.roleData.strength;
            entity.tempHitWidthModifier = skill.hitAmend;
        }
        else if (skill.skillType == SkillType.Dodge)
        {
            entity.tempHitWidthModifier = skill.hitAmend - entity.roleData.mentality;
        }
    }

    private void ExecuteSecondaryAction(BattleEntity user, BattleEntity target, SkillData skill)
    {
        if (skill == null) return;

        // 1. 判断并处理消耗逻辑
        if (skill.skillType == SkillType.Item)
        {
            if (skill.quantity <= 0) return;
            skill.quantity--;
            Debug.Log($"[{user.roleData.roleName}] 使用了道具 [{skill.skillName}]，剩余: {skill.quantity}");
        }
        else if (skill.skillType == SkillType.Special)
        {
            Debug.Log($"[{user.roleData.roleName}] 发动了特殊技能 [{skill.skillName}]");
        }
        else
        {
            return; // 非次级行为，跳过
        }

        // 2. 遍历执行所有附加多态特效
        if (skill.effects != null && skill.effects.Count > 0)
        {
            foreach (var effect in skill.effects)
            {
                if (effect == null) continue;
                // 【核心修复】：加上 skill.skillLevel 参数
                effect.Execute(user, target, this, skill.skillLevel);
            }
        }
    }
}