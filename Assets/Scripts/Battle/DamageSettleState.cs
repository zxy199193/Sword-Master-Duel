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

    // ==========================================
    // Lifecycle Methods
    // ==========================================

    public override void Enter()
    {
        stateTimer = 0f;
        currentHitIndex = 0;
        isFinished = false;

        battleManager.playerEntity.OnAnimHitPoint += ExecuteDamage;
        battleManager.enemyEntity.OnAnimHitPoint += ExecuteDamage;

        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;
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

    // ==========================================
    // Damage Execution
    // ==========================================

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

    private void ApplyDamageLogic(HitSection? hit)
    {
        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;
        BattleEntity defender = battleManager.isPlayerAttacking ? battleManager.enemyEntity : battleManager.playerEntity;

        SkillSlot attackSlot = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;
        SkillData skill = attackSlot != null ? attackSlot.skillData : null;
        int level = attackSlot != null ? attackSlot.level : 1;

        bool isPlayerTakingDamage = !battleManager.isPlayerAttacking;

        if (hit.HasValue && skill != null)
        {
            float multiplier = GlobalBattleRules.GetHitMultiplier(hit.Value.level);
            int finalStrength = attacker.roleData.strength;
            float weaponAtkFactor = 1.0f;

            if (battleManager.isPlayerAttacking && GameManager.Instance != null)
            {
                PlayerProfile profile = GameManager.Instance.playerProfile;
                finalStrength = profile.GetFinalStrength();
                if (profile.equippedWeapon != null) weaponAtkFactor = profile.equippedWeapon.atkFactor;
            }

            if (skill.effects != null && skill.effects.Count > 0)
            {
                foreach (var effect in skill.effects)
                {
                    if (effect != null)
                    {
                        effect.OnPreDamageSettle(attacker, defender, battleManager, level, multiplier, weaponAtkFactor);
                    }
                }
            }

            int equipDamageModifier = 0;
            if (battleManager.isPlayerAttacking) 
                equipDamageModifier = battleManager.TriggerPlayerEquipEffects(EquipTriggerTiming.OnAttackHit, hit.Value.level);
            else 
                equipDamageModifier = battleManager.TriggerPlayerEquipEffects(EquipTriggerTiming.OnDefendHit, hit.Value.level);

            // 1. 收集攻击方的特效增伤
            int skillEffectBaseDamageMod = 0;
            if (skill.effects != null && skill.effects.Count > 0)
            {
                foreach (var effect in skill.effects)
                {
                    if (effect != null)
                    {
                        skillEffectBaseDamageMod += effect.GetBaseDamageModifier(attacker, defender, battleManager, level);
                    }
                }
            }

            // 2. 收集防御方的特效减伤
            SkillSlot defendSlot = battleManager.isPlayerAttacking ? battleManager.currentEnemySkill : battleManager.currentPlayerSkill;
            SkillData defendSkill = defendSlot != null ? defendSlot.skillData : null;
            int defendLevel = defendSlot != null ? defendSlot.level : 1;

            int skillEffectDefenseMod = 0;
            if (defendSkill != null && defendSkill.effects != null && defendSkill.effects.Count > 0)
            {
                foreach (var effect in defendSkill.effects)
                {
                    if (effect != null)
                    {
                        skillEffectDefenseMod += effect.GetDefenseModifier(defender, attacker, battleManager, defendLevel, hit.Value.level);
                    }
                }
            }

            // 3. 计算最终伤害: (总攻击力 - 总减伤) × 武器倍率 × 打击条倍率
            float totalBaseDamage = skill.GetBasicDamage(level) + (finalStrength * 2) + equipDamageModifier + skillEffectBaseDamageMod;
            
            if (attacker.activeStatuses.ContainsKey(StatusType.Excited))
            {
                totalBaseDamage += 6;
            }

            int totalReduction = Mathf.RoundToInt(defender.tempDamageReduction) + skillEffectDefenseMod;

            float netBaseDamage = Mathf.Max(0, totalBaseDamage - totalReduction);
            int finalDamage = Mathf.RoundToInt(weaponAtkFactor * multiplier * netBaseDamage);

            // 造成最终伤害
            defender.TakeDamage(finalDamage);

            // 播放音效
            int hitSoundType = 1; // 正常命中
            if (defendSkill != null && defendSkill.skillType == SkillType.Defend)
            {
                hitSoundType = 2; // 被防御
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHitSound(hitSoundType);
            }

            // 播放特效和飘字
            battleManager.SpawnHitEffect(defender.transform);
            int hitLevelTag = (int)hit.Value.level >= 3 ? 2 : 1;
            battleManager.SpawnDamagePopup(isPlayerTakingDamage, finalDamage.ToString(), hitLevelTag);

            // 触发特效 OnAttackHit 钩子
            if (skill.effects != null && skill.effects.Count > 0)
            {
                foreach (var effect in skill.effects)
                {
                    if (effect != null)
                    {
                        effect.Execute(attacker, defender, battleManager, level);
                        effect.OnAttackHit(attacker, defender, battleManager, level, hit.Value.level);
                    }
                }
            }

            // 判断生死与动画
            if (defender.currentBasicLife <= 0) defender.PlayDieAnim();
            else defender.PlayHitAnim();
        }
        else
        {
            Debug.Log($"[DamageSettleState] {attacker.roleData.roleName} 的该段攻击 Miss！");
            defender.PlayMissAnim();
            battleManager.SpawnDamagePopup(isPlayerTakingDamage, "MISS", 0);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHitSound(0); // Miss 音效
            }

            // 触发防守方闪避成功时的特效 (如：无刀取)
            SkillSlot defendSlot = battleManager.isPlayerAttacking ? battleManager.currentEnemySkill : battleManager.currentPlayerSkill;
            SkillData defendSkill = defendSlot != null ? defendSlot.skillData : null;
            int defendLevel = defendSlot != null ? defendSlot.level : 1;

            if (defendSkill != null && defendSkill.effects != null && defendSkill.effects.Count > 0)
            {
                foreach (var effect in defendSkill.effects)
                {
                    if (effect != null)
                    {
                        effect.OnEvadeSuccess(defender, attacker, battleManager, defendLevel);
                    }
                }
            }
        }
    }

    // ==========================================
    // State Control
    // ==========================================

    private IEnumerator DelayFinish(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        if (!isFinished)
        {
            isFinished = true;
            FinishStateAndTurn();
        }
    }

    private void FinishStateAndTurn()
    {
        battleManager.ProceedNextAttack();
    }
}