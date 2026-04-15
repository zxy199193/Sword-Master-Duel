using UnityEngine;
using System.Collections;

/// <summary>
/// 伤害结算状态：监听动画事件并逐段结算多段攻击的伤害与表现
/// </summary>
public class DamageSettleState : BattleState
{
    private float stateTimer = 0f;
    private int currentHitIndex = 0;
    private float animEndTime = 3.5f;

    // 【核心修复】：加一把防爆锁，防止 Update 每帧疯狂调用发牌器
    private bool isFinished = false;

    public DamageSettleState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        stateTimer = 0f;
        currentHitIndex = 0;
        isFinished = false; // 进入状态时重置锁

        battleManager.playerEntity.OnAnimHitPoint += ExecuteDamage;
        battleManager.enemyEntity.OnAnimHitPoint += ExecuteDamage;

        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;
        SkillData attackSkill = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;

        if (battleManager.currentHitResults.Count == 0 && battleManager.currentAttackTimeout)
        {
            Debug.Log("[DamageSettleState] 攻击超时，未输入任何有效指令。");
            // 超时没打出任何段数，直接给个 1秒 缓冲就结束，不用死等 3.5秒
            battleManager.StartCoroutine(DelayFinish(1.0f));
        }
        else if (attacker != null && attackSkill != null)
        {
            attacker.PlayAnim(attackSkill.animationTriggerName);
        }
    }

    public override void Execute()
    {
        if (isFinished) return; // 【锁上了就不准再进了！】

        stateTimer += Time.deltaTime;

        // 超时保底机制：如果动画卡了，3.5秒强制结束
        if (stateTimer >= animEndTime)
        {
            isFinished = true;
            FinishStateAndTurn();
        }
    }

    public override void Exit()
    {
        battleManager.playerEntity.OnAnimHitPoint -= ExecuteDamage;
        battleManager.enemyEntity.OnAnimHitPoint -= ExecuteDamage;
    }

    private void ExecuteDamage()
    {
        var results = battleManager.currentHitResults;
        HitSection? currentHit = null;

        if (currentHitIndex < results.Count)
        {
            currentHit = results[currentHitIndex];
        }

        ApplyDamageLogic(currentHit);
        currentHitIndex++;

        // 【体验神级优化】：如果已经结算完了所有的段数，不需要死等 3.5 秒保底！
        if (currentHitIndex >= Mathf.Max(1, results.Count))
        {
            // 给 0.5 秒的缓冲时间让受击特效和动画播完，然后丝滑移交控制权
            battleManager.StartCoroutine(DelayFinish(0.5f));
        }
    }

    // 协程：延迟结束当前状态
    private IEnumerator DelayFinish(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        if (!isFinished)
        {
            isFinished = true;
            FinishStateAndTurn();
        }
    }

    private void ApplyDamageLogic(HitSection? hit)
    {
        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;
        BattleEntity defender = battleManager.isPlayerAttacking ? battleManager.enemyEntity : battleManager.playerEntity;
        SkillData skill = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;

        bool isPlayerTakingDamage = !battleManager.isPlayerAttacking;

        if (hit.HasValue)
        {
            float multiplier = GlobalBattleRules.GetHitMultiplier(hit.Value.level);
            int rawDamage = Mathf.RoundToInt(multiplier * (skill.basicDamage + attacker.roleData.strength));
            int finalDamage = Mathf.Max(0, rawDamage - Mathf.RoundToInt(defender.tempDamageReduction));

            defender.TakeDamage(finalDamage);

            // 1. 播放击中特效
            battleManager.SpawnHitEffect(defender.transform);

            // 2. 飘字 
            int hitLevelTag = (int)hit.Value.level >= 3 ? 2 : 1;
            battleManager.SpawnDamagePopup(isPlayerTakingDamage, finalDamage.ToString(), hitLevelTag);

            // 3. 动画反馈
            if (defender.currentBasicLife <= 0) defender.PlayDieAnim();
            else defender.PlayHitAnim();
        }
        else
        {
            Debug.Log($"[DamageSettleState] {attacker.roleData.roleName} 的该段攻击 Miss！");
            defender.PlayMissAnim();
            battleManager.SpawnDamagePopup(isPlayerTakingDamage, "MISS", 0);
        }
    }

    private void FinishStateAndTurn()
    {
        battleManager.ProceedNextAttack();
    }
}