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

        // 看破机制判定：如果防御方有看破，且攻击方技能与上回合一致，则必定闪避
        bool isInsightDodged = false;
        if (defender.activeStatuses.ContainsKey(StatusType.Insight) && skill != null && skill == attacker.lastUsedAttackSkill)
        {
            isInsightDodged = true;
            Debug.Log($"<color=#00FFFF>[看破触发] {defender.roleData.roleName} 看穿了 {attacker.roleData.roleName} 的招式 {skill.skillName}！</color>");
        }

        bool hasMissDamageEffect = attacker.HasEquipEffect<GlobalBattleRules.MissDamageEquipEffect>(out _);
        if (!hit.HasValue && hasMissDamageEffect && !isInsightDodged && skill != null)
        {
            hit = new HitSection { level = SectionLevel.Level0, width = 0, axisPosition = 0 };
        }

        if (hit.HasValue && skill != null && !isInsightDodged)
        {
            float multiplier = GlobalBattleRules.GetHitMultiplier(hit.Value.level);
            if (hit.Value.level == SectionLevel.Level0 && hasMissDamageEffect)
            {
                multiplier = 0.5f;
                Debug.Log($"[{attacker.roleData.roleName}] 触发必中残响！Miss转化为0.5倍伤害！");
            }
            int finalStrength = attacker.GetFinalStrength();
            float weaponAtkFactor = 1.0f;

            if (battleManager.isPlayerAttacking && GameManager.Instance != null)
            {
                PlayerProfile profile = GameManager.Instance.playerProfile;
                if (profile.equippedWeapon != null) weaponAtkFactor = profile.equippedWeapon.atkFactor;
            }
            else
            {
                // 敌人：从 roleData 上的装备读取武器攻击倍率
                if (attacker.roleData.equippedWeapon != null)
                    weaponAtkFactor = attacker.roleData.equippedWeapon.atkFactor;
            }

            // 磨刀石效果：锐化状态增加 0.5 倍率
            if (attacker.activeStatuses.ContainsKey(StatusType.Sharpened))
            {
                weaponAtkFactor += 0.5f;
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
            equipDamageModifier += battleManager.TriggerEquipEffects(attacker, EquipTriggerTiming.OnAttackHit, hit.Value.level);
            equipDamageModifier += battleManager.TriggerEquipEffects(defender, EquipTriggerTiming.OnDefendHit, hit.Value.level);

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

            // 3. 计算最终伤害:
            // (技能基础伤害 + 各类修正 + 每4点力量+1基础伤害 - 减伤) × 武器倍率 × 打击条倍率 × 力量百分比增幅(1 + 0.03×力量)
            int attackTypeBonus = battleManager.GetSkillTypeBonus(attacker, SkillType.Attack);
            int strengthFlatBonus = finalStrength / 4;  // 每4点力量 +1 基础伤害
            float totalBaseDamage = skill.GetBasicDamage(level) + strengthFlatBonus + equipDamageModifier + skillEffectBaseDamageMod + attackTypeBonus;

            if (defendSkill != null && defendSkill.skillType == SkillType.Defend && attacker.HasEquipEffect<GlobalBattleRules.DoubleDamageOnDefendEquipEffect>(out _))
            {
                totalBaseDamage *= 2;
                Debug.Log($"[{attacker.roleData.roleName}] 触发破防增伤！基础伤害翻倍！");
            }

            if (attacker.HasEquipEffect<GlobalBattleRules.SubSkillDamageBoostEquipEffect>(out var subSkillBoostEff))
            {
                SkillSlot subSlot = battleManager.isPlayerAttacking ? battleManager.currentPlayerSubSkill : battleManager.currentEnemySubSkill;
                if (subSlot != null && subSlot.skillData != null && subSlot.skillData.skillType == SkillType.Special)
                {
                    totalBaseDamage += subSkillBoostEff.damageBoost;
                    Debug.Log($"[{attacker.roleData.roleName}] 触发连击增伤！额外增加 {subSkillBoostEff.damageBoost} 伤害！");
                }
            }
            
            if (attacker.activeStatuses.ContainsKey(StatusType.Excited))
            {
                totalBaseDamage += 6;
            }

            if (attacker.activeStatuses.ContainsKey(StatusType.Enraged))
            {
                totalBaseDamage += 10;
            }

            int totalReduction = Mathf.RoundToInt(defender.tempDamageReduction) + skillEffectDefenseMod;

            float netBaseDamage = Mathf.Max(0, totalBaseDamage - totalReduction);
            float strengthPercentBonus = 1f + 0.03f * finalStrength;  // 每点力量 +3% 技能伤害
            int finalDamage = Mathf.RoundToInt(weaponAtkFactor * multiplier * netBaseDamage * strengthPercentBonus);

            // [伤害公式日志] 每次命中输出计算过程，方便数值验证
            {
                int skillBase = Mathf.RoundToInt(skill.GetBasicDamage(level));
                int excitedBonus = attacker.activeStatuses.ContainsKey(StatusType.Excited) ? 6 : 0;
                string correctionBreakdown = $"力量:{strengthFlatBonus}+装备:{equipDamageModifier}+特效:{skillEffectBaseDamageMod}+攻击类型:{attackTypeBonus}+亢奋:{excitedBonus}-减伤:{totalReduction}";
                Debug.Log(
                    $"<color=#FFD700>[伤害结算] {attacker.roleData.roleName} -> {defender.roleData.roleName}\n" +
                    $"最终伤害 = (基础伤害[{skillBase}] + 修正[{correctionBreakdown}]) " +
                    $"× 武器倍率[{weaponAtkFactor:F2}] × 打击条倍率[{multiplier:F2}] × 力量增幅[{strengthPercentBonus:F2}] = {finalDamage}</color>"
                );
            }

            // 分身增伤：伤害翻倍
            if (attacker.activeStatuses.ContainsKey(StatusType.Clone))
            {
                finalDamage *= 2;
                Debug.Log($"<color=cyan>[{attacker.roleData.roleName}] 触发分身！伤害翻倍 -> {finalDamage}</color>");
            }

            // 造成最终伤害
            bool hitLanded = false;
            if (attacker.HasEquipEffect<GlobalBattleRules.DirectBasicLifeDamageEquipEffect>(out var directLifeEffect) && 
                hit.Value.level >= directLifeEffect.minLevel && hit.Value.level <= directLifeEffect.maxLevel)
            {
                hitLanded = defender.TakeDamage(finalDamage, true);
                battleManager.SpawnGeneralPopup(defender.isPlayer, "<color=#FF0000>贯穿!</color>");
            }
            else
            {
                hitLanded = defender.TakeDamage(finalDamage);
            }

            if (hitLanded)
            {
                if (attackSlot != null && attackSlot.skillData.skillType == SkillType.Attack)
                {
                    battleManager.TriggerEquipEffects(defender, EquipTriggerTiming.OnTakeAttackSkillDamage, hit.Value.level);
                }

                // 火焰附加特效：命中时给对方上 1 回合灼烧
                if (attacker.activeStatuses.ContainsKey(StatusType.FireEnchant))
                {
                    defender.AddStatus(StatusType.Burn, 1, attacker);
                    battleManager.SpawnGeneralPopup(defender.isPlayer, "[灼烧]");
                    Debug.Log($"<color=orange>[{attacker.roleData.roleName}] 触发火焰附加！给 [{defender.roleData.roleName}] 施加了 1 回合灼烧。</color>");
                }

                // 血战：恢复造成最终伤害 30% 的基础生命值
                if (attacker.activeStatuses.ContainsKey(StatusType.Bloodlust))
                {
                    int healAmount = Mathf.FloorToInt(finalDamage * 0.3f);
                    if (healAmount > 0)
                    {
                        attacker.currentBasicLife += healAmount;
                        if (attacker.currentBasicLife > attacker.GetFinalMaxLife()) attacker.currentBasicLife = attacker.GetFinalMaxLife();
                        attacker.OnHpChanged?.Invoke();
                        battleManager.SpawnGeneralPopup(!defender.isPlayer, $"<color=green>血战 +{healAmount}</color>");
                    }
                }

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
                battleManager.SpawnHitEffect(defender.transform, hit.Value.level);
                int hitLevelTag = (int)hit.Value.level >= 3 ? 2 : 1;
                battleManager.SpawnDamagePopup(isPlayerTakingDamage, finalDamage.ToString(), hitLevelTag);

                // 非防御命中时触发屏幕震动
                if (hitSoundType != 2 && CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake();
                }

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

                // 触发防御方的 OnBeingHit 钩子 (用于受击反制类技能)
                if (defendSkill != null && defendSkill.effects != null && defendSkill.effects.Count > 0)
                {
                    foreach (var effect in defendSkill.effects)
                    {
                        if (effect != null)
                        {
                            effect.OnBeingHit(defender, attacker, battleManager, defendLevel, hit.Value.level);
                        }
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
            battleManager.SpawnGeneralPopup(isPlayerTakingDamage, "MISS");

            battleManager.TriggerEquipEffects(defender, EquipTriggerTiming.OnEvade, null);

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
        // 更新上回合使用的攻击技能记录（仅限攻击技能）
        BattleEntity attacker = battleManager.isPlayerAttacking ? battleManager.playerEntity : battleManager.enemyEntity;
        SkillSlot attackSlot = battleManager.isPlayerAttacking ? battleManager.currentPlayerSkill : battleManager.currentEnemySkill;
        if (attackSlot != null && attackSlot.skillData != null && attackSlot.skillData.skillType == SkillType.Attack)
        {
            attacker.lastUsedAttackSkill = attackSlot.skillData;
        }

        battleManager.ProceedNextAttack();
    }
}