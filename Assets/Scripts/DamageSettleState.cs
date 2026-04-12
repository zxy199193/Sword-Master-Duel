using UnityEngine;

/// <summary>
/// 伤害结算状态：监听动画事件并逐段结算多段攻击的伤害与表现
/// </summary>
public class DamageSettleState : BattleState
{
    private float stateTimer = 0f;
    private int currentHitIndex = 0;  // 当前正在结算的多段攻击索引
    private float animEndTime = 3.5f; // 动画演出保底超时时间，防止卡死

    public DamageSettleState(BattleManager manager) : base(manager) { }

    // ==========================================
    // 状态机生命周期 (State Lifecycle)
    // ==========================================

    public override void Enter()
    {
        stateTimer = 0f;
        currentHitIndex = 0;

        // 订阅双方的动画命中事件
        battleManager.playerEntity.OnAnimHitPoint += ExecuteDamage;
        battleManager.enemyEntity.OnAnimHitPoint += ExecuteDamage;

        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;
        SkillData attackSkill = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;

        // 检查超时未输入指令的情况
        if (battleManager.currentHitResults.Count == 0 && battleManager.currentAttackTimeout)
        {
            Debug.Log("[DamageSettleState] 攻击超时，未输入任何有效指令。");
        }

        // 播放配置好的专属攻击动画
        if (attacker != null && attackSkill != null)
        {
            attacker.PlayAnim(attackSkill.animationTriggerName);
        }
    }

    public override void Execute()
    {
        stateTimer += Time.deltaTime;

        // 超时保底机制：强制结束当前状态
        if (stateTimer >= animEndTime)
        {
            FinishStateAndTurn();
        }
    }

    public override void Exit()
    {
        // 严谨注销事件监听，防止内存泄漏
        battleManager.playerEntity.OnAnimHitPoint -= ExecuteDamage;
        battleManager.enemyEntity.OnAnimHitPoint -= ExecuteDamage;
    }

    // ==========================================
    // 核心伤害逻辑 (Core Damage Logic)
    // ==========================================

    /// <summary>
    /// 动画事件触发时的回调：读取当前段数结果并应用伤害
    /// </summary>
    private void ExecuteDamage()
    {
        var results = battleManager.currentHitResults;
        HitSection? currentHit = null;

        // 若索引未越界，取出玩家实际打出的有效判定
        if (currentHitIndex < results.Count)
        {
            currentHit = results[currentHitIndex];
        }
        // 若超出列表长度，说明后续段数因超时未点击，视为 Miss (null)

        ApplyDamageLogic(currentHit);
        currentHitIndex++; // 指针推移到下一段
    }

    /// <summary>
    /// 结算单次命中判定，计算数值并触发表现层 (扣血、飘字、动画)
    /// </summary>
    private void ApplyDamageLogic(HitSection? hit)
    {
        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;
        BattleEntity defender = battleManager.isPlayerAttacking ? battleManager.enemyEntity : battleManager.playerEntity;
        SkillData skill = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;

        if (hit.HasValue)
        {
            // 命中结算
            float multiplier = GlobalBattleRules.GetHitMultiplier(hit.Value.level);
            int rawDamage = Mathf.RoundToInt(multiplier * (skill.basicDamage + attacker.roleData.strength));
            int finalDamage = Mathf.Max(0, rawDamage - Mathf.RoundToInt(defender.tempDamageReduction));

            defender.TakeDamage(finalDamage);

            // 飘字等级判定 (Level 3及以上为暴击红字)
            int hitLevelTag = (int)hit.Value.level >= 3 ? 2 : 1;
            battleManager.SpawnDamagePopup(defender.transform.position, finalDamage.ToString(), hitLevelTag);

            if (defender.currentBasicLife <= 0) defender.PlayDieAnim();
            else defender.PlayHitAnim();
        }
        else
        {
            // 挥空结算
            Debug.Log($"[DamageSettleState] {attacker.roleData.roleName} 的该段攻击 Miss！");
            defender.PlayMissAnim();
            battleManager.SpawnDamagePopup(defender.transform.position, "MISS", 0);
        }
    }

    // ==========================================
    // 状态流转控制 (State Transitions)
    // ==========================================

    private void FinishStateAndTurn()
    {
        if (battleManager.playerEntity.currentBasicLife <= 0 || battleManager.enemyEntity.currentBasicLife <= 0)
        {
            Debug.Log("<color=red>[Combat] 决斗分出胜负！</color>");
        }
        else
        {
            // 若玩家攻击完毕且敌人也有攻击意图，转交敌方回合
            if (battleManager.isPlayerAttacking &&
                battleManager.currentEnemySkill != null &&
                battleManager.currentEnemySkill.skillType == SkillType.Attack)
            {
                battleManager.ChangeState(new EnemyActionState(battleManager));
            }
            else
            {
                EndFullTurn();
            }
        }
    }

    private void EndFullTurn()
    {
        battleManager.playerEntity.RecoverStamina();
        battleManager.enemyEntity.RecoverStamina();
        battleManager.ChangeState(new PreparationState(battleManager));
    }
}