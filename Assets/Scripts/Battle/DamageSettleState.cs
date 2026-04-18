using System.Collections;
using UnityEngine;
using static GlobalBattleRules;

/// <summary>
/// 伤害结算状态：监听动画事件并逐段结算多段攻击的伤害与表现
/// </summary>
public class DamageSettleState : BattleState
{
    private float stateTimer = 0f;
    private int currentHitIndex = 0;
    private float animEndTime = 3.5f;
    private bool isFinished = false;

    public DamageSettleState(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        stateTimer = 0f;
        currentHitIndex = 0;
        isFinished = false;

        battleManager.playerEntity.OnAnimHitPoint += ExecuteDamage;
        battleManager.enemyEntity.OnAnimHitPoint += ExecuteDamage;

        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;

        // 【已修复：获取 SkillSlot】
        SkillSlot attackSlot = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;
        SkillData attackSkill = attackSlot != null ? attackSlot.skillData : null;

        if (battleManager.currentHitResults.Count == 0 && battleManager.currentAttackTimeout)
        {
            Debug.Log("[DamageSettleState] 攻击超时，未输入任何有效指令。");
            battleManager.StartCoroutine(DelayFinish(1.0f));
        }
        else if (attacker != null && attackSkill != null)
        {
            attacker.PlayAnim(attackSkill.animationTriggerName);
        }
    }

    public override void Execute()
    {
        if (isFinished) return;

        stateTimer += Time.deltaTime;

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

        if (currentHitIndex >= Mathf.Max(1, results.Count))
        {
            battleManager.StartCoroutine(DelayFinish(0.5f));
        }
    }

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

        // 【已修复：提取 SkillSlot 与 Level】
        SkillSlot attackSlot = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;
        SkillData skill = attackSlot != null ? attackSlot.skillData : null;
        int level = attackSlot != null ? attackSlot.level : 1;

        bool isPlayerTakingDamage = !battleManager.isPlayerAttacking;

        if (hit.HasValue && skill != null)
        {
            float multiplier = GlobalBattleRules.GetHitMultiplier(hit.Value.level);

            // 【已修复：读取最终力量与装备倍率】
            int finalStrength = attacker.roleData.strength;
            float weaponAtkFactor = 1.0f;

            if (battleManager.isPlayerAttacking && GameManager.Instance != null)
            {
                PlayerProfile profile = GameManager.Instance.playerProfile;
                finalStrength = profile.GetFinalStrength();
                if (profile.equippedWeapon != null) weaponAtkFactor = profile.equippedWeapon.atkFactor;
            }

            int equipDamageModifier = 0;
            if (battleManager.isPlayerAttacking) equipDamageModifier = battleManager.TriggerPlayerEquipEffects(EquipTriggerTiming.OnAttackHit, hit.Value.level);
            else equipDamageModifier = battleManager.TriggerPlayerEquipEffects(EquipTriggerTiming.OnDefendHit, hit.Value.level);

            // 【已修复：动态读取不同等级的伤害值】
            float totalBaseDamage = skill.GetBasicDamage(level) + finalStrength + equipDamageModifier;
            int rawDamage = Mathf.RoundToInt(weaponAtkFactor * multiplier * totalBaseDamage);

            int finalDamage = Mathf.Max(0, rawDamage - Mathf.RoundToInt(defender.tempDamageReduction));

            defender.TakeDamage(finalDamage);

            battleManager.SpawnHitEffect(defender.transform);
            int hitLevelTag = (int)hit.Value.level >= 3 ? 2 : 1;
            battleManager.SpawnDamagePopup(isPlayerTakingDamage, finalDamage.ToString(), hitLevelTag);

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