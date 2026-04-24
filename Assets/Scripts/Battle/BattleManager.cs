using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GlobalBattleRules;

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

    [Header("飘字挂点 (1) 通用 - Miss/Debuff等提示文字")]
    public Transform playerGeneralAnchor;
    public Transform enemyGeneralAnchor;
    [Header("飘字挂点 (2) 战斗伤害 - 随机偏移")]
    public Transform playerDamageAnchor;
    public Transform enemyDamageAnchor;
    [Header("飘字挂点 (3) 体力恢复")]
    public Transform playerStaminaRecoverAnchor;
    public Transform enemyStaminaRecoverAnchor;
    [Header("飘字挂点 (4) 生命恢复")]
    public Transform playerHpRecoverAnchor;
    public Transform enemyHpRecoverAnchor;
    [Header("飘字挂点 (5) 行动信息")]
    public Transform playerActionInfoAnchor;
    public Transform enemyActionInfoAnchor;

    [Header("特效预制体 (Effect Prefabs)")]
    public GameObject hitEffectPrefab;
    public float hitEffectLifeTime = 0.5f;

    [Header("Broadcast UI (广播系统)")]
    public GameObject broadcastUIRoot;
    public UnityEngine.UI.Text broadcastText;

    [Header("VFX & Prefabs")]
    public GameObject actionInfoPrefab;
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

    // 全部改为接收 SkillSlot 包装盒
    [HideInInspector] public SkillSlot currentPlayerSkill;
    [HideInInspector] public SkillSlot currentPlayerSubSkill;
    [HideInInspector] public SkillSlot currentEnemySkill;
    [HideInInspector] public SkillSlot currentEnemySubSkill;

    // ==========================================
    // 战斗序列追踪 (Queue Tracking)
    // ==========================================
    [HideInInspector] public bool isPlayerAttackResolved;
    [HideInInspector] public bool isEnemyAttackResolved;

    private void Update()
    {
        currentState?.Execute();
    }

    public void ChangeState(BattleState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        Debug.Log($"<color=cyan>[BattleManager] 进入状态：{newState.GetType().Name}</color>");
        currentState.Enter();
    }

    public void OnPlayerSelectedAction() { }

    // 参数类型改为 SkillSlot
    public void OnPlayerActionConfirmed(SkillSlot mainSkill, SkillSlot subSkill)
    {
        playerEntity.ResetTurnData();
        enemyEntity.ResetTurnData();

        currentPlayerSkill = mainSkill;
        currentPlayerSubSkill = subSkill;

        currentEnemySkill = null;
        currentEnemySubSkill = null;

        if (enemyEntity.runtimeSkills != null && enemyEntity.runtimeSkills.Count > 0)
        {
            float hpPercentage = (float)enemyEntity.currentBasicLife / enemyEntity.roleData.maxBasicLife;

            // 1. 扁平化阶段判定 (0: 100-75%, 1: 75-50%, 2: 50-25%, 3: 25-0%)
            int phaseIndex = 0;
            if (hpPercentage <= 0.25f) phaseIndex = 3;
            else if (hpPercentage <= 0.50f) phaseIndex = 2;
            else if (hpPercentage <= 0.75f) phaseIndex = 1;

            Debug.Log($"<color=#FFD700>[AI 决策] 血量 {hpPercentage:P0}，进入第 {phaseIndex + 1} 阶段</color>");

            var validMainSkills = new List<KeyValuePair<SkillSlot, int>>();
            var validSubSkills = new List<KeyValuePair<SkillSlot, int>>();

            // 2. 直接根据阶段索引读取权重
            foreach (var slot in enemyEntity.runtimeSkills)
            {
                if (slot == null || slot.skillData == null) continue;

                // 检查体力是否足够
                if (GetActualSkillCost(enemyEntity, slot) > enemyEntity.currentStamina) continue;

                // 从刚才缓存的字典里，直接拿对应阶段的真实权重
                int weight = 0;
                if (enemyEntity.runtimeSkillWeights != null && enemyEntity.runtimeSkillWeights.TryGetValue(slot, out int[] weights))
                {
                    if (weights != null && weights.Length > phaseIndex)
                    {
                        weight = weights[phaseIndex];
                    }
                }

                // 只要权重 > 0 就放入候选池
                if (weight > 0)
                {
                    if (slot.skillData.skillType == SkillType.Attack || slot.skillData.skillType == SkillType.Defend || slot.skillData.skillType == SkillType.Dodge)
                    {
                        validMainSkills.Add(new KeyValuePair<SkillSlot, int>(slot, weight));
                    }
                    else if (slot.skillData.skillType == SkillType.Special || (slot.skillData.skillType == SkillType.Item && slot.quantity > 0))
                    {
                        validSubSkills.Add(new KeyValuePair<SkillSlot, int>(slot, weight));
                    }
                }
            }

            // 3. 摇骰子决定出招
            currentEnemySkill = SelectSkillByWeight(validMainSkills);

            // 扣除主技能的预估费用，看看还剩多少体力放副技能
            int mainSkillCost = currentEnemySkill != null ? GetActualSkillCost(enemyEntity, currentEnemySkill) : 0;
            int remainingStamina = enemyEntity.currentStamina - mainSkillCost;

            // 从缓存中直接读取对应阶段的副技能概率
            float subProb = 0f;
            if (enemyEntity.runtimeSubSkillProbabilities != null && enemyEntity.runtimeSubSkillProbabilities.Length > phaseIndex)
            {
                subProb = enemyEntity.runtimeSubSkillProbabilities[phaseIndex];
            }

            if (Random.value < subProb && remainingStamina > 0 && validSubSkills.Count > 0)
            {
                // 最后再过滤一遍，确保加上主技能消耗后，体力还够放副技能
                validSubSkills.RemoveAll(kvp => GetActualSkillCost(enemyEntity, kvp.Key) > remainingStamina);
                currentEnemySubSkill = SelectSkillByWeight(validSubSkills);
            }
        }

        string pMain = currentPlayerSkill != null ? currentPlayerSkill.skillData.skillName : "无";
        string pSub = currentPlayerSubSkill != null ? currentPlayerSubSkill.skillData.skillName : "无";
        string eMain = currentEnemySkill != null ? currentEnemySkill.skillData.skillName : "无";
        string eSub = currentEnemySubSkill != null ? currentEnemySubSkill.skillData.skillName : "无";

        Debug.Log($"<color=cyan>[Combat] 玩家: 主[{pMain}] 副[{pSub}] | 敌人: 主[{eMain}] 副[{eSub}]</color>");

        // 判定双方是否都选择了防御/闪避（这种情况下不消耗主技能体力）
        bool isBothDefensive = false;
        if (currentPlayerSkill != null && currentEnemySkill != null)
        {
            var pType = currentPlayerSkill.skillData.skillType;
            var eType = currentEnemySkill.skillData.skillType;
            bool pIsDef = (pType == SkillType.Defend || pType == SkillType.Dodge);
            bool eIsDef = (eType == SkillType.Defend || eType == SkillType.Dodge);
            if (pIsDef && eIsDef)
            {
                isBothDefensive = true;
                Debug.Log("<color=orange>[Combat] 双方均选择了防守，本轮主技能体力消耗豁免！</color>");
            }
        }

        // 动态获取费用并扣除体力
        if (currentPlayerSkill != null) 
        {
            int cost = isBothDefensive ? 0 : GetActualSkillCost(playerEntity, currentPlayerSkill, currentPlayerSubSkill);
            playerEntity.ConsumeStamina(cost);
        }
        if (currentPlayerSubSkill != null) playerEntity.ConsumeStamina(GetActualSkillCost(playerEntity, currentPlayerSubSkill));

        if (currentEnemySkill != null) 
        {
            int cost = isBothDefensive ? 0 : GetActualSkillCost(enemyEntity, currentEnemySkill, currentEnemySubSkill);
            enemyEntity.ConsumeStamina(cost);
        }
        if (currentEnemySubSkill != null) enemyEntity.ConsumeStamina(GetActualSkillCost(enemyEntity, currentEnemySubSkill));
        // ==========================================
        // 钉刺的动作惩罚检测
        // ==========================================
        void ApplySpikesDamage(BattleEntity entity, SkillSlot skill)
        {
            if (skill != null && entity.activeStatuses.ContainsKey(StatusType.Spikes))
            {
                if (skill.skillData.skillType == SkillType.Attack || skill.skillData.skillType == SkillType.Dodge)
                {
                    entity.TakeDamage(3);
                    SpawnGeneralPopup(entity.isPlayer, "<color=#8B0000>扎伤 -3</color>");
                    Debug.Log($"[场地魔法] {entity.roleData.roleName} 因为动作幅度过大，触发了钉刺，受到 3 点伤害！");
                }
            }
        }

        ApplySpikesDamage(playerEntity, currentPlayerSkill);
        ApplySpikesDamage(enemyEntity, currentEnemySkill);

        // 防御性拦截：如果有人按完技能直接被钉刺扎死了，立刻结束战斗
        if (playerEntity.currentBasicLife <= 0 || enemyEntity.currentBasicLife <= 0)
        {
            ChangeState(new BattleEndState(this));
            return;
        }

        // ==========================================
        ChangeState(new ActionBroadcastState(this));
    }


    private void ApplyNonAttackSkills(BattleEntity entity, SkillSlot slot)
    {
        if (slot == null || slot.skillData == null) return;

        if (slot.skillData.skillType == SkillType.Defend)
        {
            int equipBonus = GetSkillTypeBonus(entity, SkillType.Defend);
            entity.tempDamageReduction = slot.skillData.GetBasicDefend(slot.level) + entity.GetFinalEndurance() + equipBonus;
            entity.tempHitWidthModifier = slot.skillData.GetHitAmend(slot.level);
        }
        else if (slot.skillData.skillType == SkillType.Dodge)
        {
            // 如果有灵动状态，额外提供 6 点判定缩减！
            float agileBonus = entity.activeStatuses.ContainsKey(StatusType.Agile) ? 6f : 0f;
            float equipBonus = GetSkillTypeBonus(entity, SkillType.Dodge);
            entity.tempHitWidthModifier = slot.skillData.GetHitAmend(slot.level) - entity.GetFinalMentality() - agileBonus - equipBonus;
        }

        // 触发闪避/防御技能自带的特效 (比如启动免疫状态)
        if (slot.skillData.effects != null && slot.skillData.effects.Count > 0)
        {
            BattleEntity target = (entity == playerEntity) ? enemyEntity : playerEntity;
            foreach (var effect in slot.skillData.effects)
            {
                if (effect != null) effect.Execute(entity, target, this, slot.level);
            }
        }
    }

    private void ExecuteSecondaryAction(BattleEntity user, BattleEntity target, SkillSlot slot)
    {
        if (slot == null || slot.skillData == null) return;

        if (slot.skillData.skillType == SkillType.Item)
        {
            if (slot.quantity <= 0) return;
            slot.quantity--;
            if (slot.sourceSlot != null) slot.sourceSlot.quantity--;
        }

        if (slot.skillData.castEffectPrefab != null)
            SpawnCastEffect(user.transform, slot.skillData.castEffectPrefab);

        if (slot.skillData.effects != null && slot.skillData.effects.Count > 0)
        {
            foreach (var effect in slot.skillData.effects)
            {
                if (effect == null) continue;

                // 免疫拦截机制：如果是害人的特效，且目标处于后撤步的免疫状态，直接没收效果！
                if (effect.IsHarmfulToTarget() && target.isImmuneToSubSkills)
                {
                    SpawnGeneralPopup(target.isPlayer, "<color=#888888>免疫!</color>");
                    Debug.Log($"[战术拦截] {target.roleData.roleName} 通过后撤步免疫了 {slot.skillData.skillName} 的负面效果！");
                    continue;
                }

                effect.Execute(user, target, this, slot.level);
            }
        }
    }

    public void SpawnDamagePopup(bool isPlayerTakingDamage, string textContent, int hitLevel)
    {
        GameObject prefabToSpawn = normalDamagePrefab;
        if (hitLevel == 0) prefabToSpawn = missPrefab;
        else if (hitLevel >= 2) prefabToSpawn = critDamagePrefab;

        if (prefabToSpawn == null) return;

        Transform targetAnchor = isPlayerTakingDamage ? playerDamageAnchor : enemyDamageAnchor;
        if (targetAnchor == null) return;

        GameObject popupObj = Instantiate(prefabToSpawn, targetAnchor);
        float randomRadius = 50f;
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * randomRadius;
        popupObj.transform.localPosition = new Vector3(randomOffset.x, randomOffset.y, 0);

        DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
        if (popupScript != null) popupScript.Setup(textContent);
    }

    public void SpawnRecoverPopup(bool isPlayer, string textContent, bool isHp)
    {
        if (normalDamagePrefab == null) return;

        Transform targetAnchor = isPlayer
            ? (isHp ? playerHpRecoverAnchor : playerStaminaRecoverAnchor)
            : (isHp ? enemyHpRecoverAnchor : enemyStaminaRecoverAnchor);

        if (targetAnchor == null) return;

        GameObject popupObj = Instantiate(normalDamagePrefab, targetAnchor);
        popupObj.transform.localPosition = Vector3.zero;

        DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
        if (popupScript != null) popupScript.Setup(textContent);
    }

    /// <summary>
    /// 通用飘字：固定挂点，用于 Miss、状态提示、特效文字等非伤害数值信息
    /// </summary>
    public void SpawnGeneralPopup(bool isPlayer, string textContent)
    {
        if (normalDamagePrefab == null) return;

        Transform targetAnchor = isPlayer ? playerGeneralAnchor : enemyGeneralAnchor;
        if (targetAnchor == null) return;

        GameObject popupObj = Instantiate(normalDamagePrefab, targetAnchor);
        popupObj.transform.localPosition = Vector3.zero;

        DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
        if (popupScript != null) popupScript.Setup(textContent);
    }

    // ==========================================
    // 行动信息工具方法
    // ==========================================

    /// <summary>
    /// 根据技能类型返回带颜色标签的技能名，用于行动信息文字
    /// </summary>
    private string GetSkillNameColored(SkillData skill)
    {
        if (skill == null) return "???";
        string colorHex = skill.skillType switch
        {
            SkillType.Attack  => "#FF4444",
            SkillType.Defend  => "#4499FF",
            SkillType.Dodge   => "#4499FF",
            SkillType.Special => "#44CC44",
            SkillType.Item    => "#CD853F",
            _                 => "#FFFFFF"
        };
        return $"<color={colorHex}>《{skill.skillName}》</color>";
    }

    /// <summary>
    /// 在指定挂点生成行动信息气泡（1s显示 + 0.5s淡出）
    /// </summary>
    private void SpawnActionInfo(Transform anchor, string text)
    {
        if (actionInfoPrefab == null || anchor == null) return;
        GameObject obj = Instantiate(actionInfoPrefab, anchor);
        obj.transform.localPosition = Vector3.zero;
        ActionInfoPopup popup = obj.GetComponent<ActionInfoPopup>();
        if (popup != null) popup.Setup(text);
    }

    /// <summary>
    /// 清理所有挂点下的残留飘字（战斗开始/结束时调用）
    /// </summary>
    public void ClearAllPopups()
    {
        Transform[] allAnchors = new Transform[]
        {
            playerGeneralAnchor,        enemyGeneralAnchor,
            playerDamageAnchor,         enemyDamageAnchor,
            playerStaminaRecoverAnchor, enemyStaminaRecoverAnchor,
            playerHpRecoverAnchor,      enemyHpRecoverAnchor,
            playerActionInfoAnchor,     enemyActionInfoAnchor,
        };

        foreach (var anchor in allAnchors)
        {
            if (anchor == null) continue;
            foreach (Transform child in anchor)
                GameObject.Destroy(child.gameObject);
        }
    }

    public void SpawnHitEffect(Transform characterRoot)
    {
        if (hitEffectPrefab == null || characterRoot == null) return;
        Transform spawnPoint = characterRoot.Find("Effect Point");
        if (spawnPoint == null) spawnPoint = characterRoot;
        GameObject effect = Instantiate(hitEffectPrefab, spawnPoint);
        effect.transform.localPosition = Vector3.zero;
        Destroy(effect, hitEffectLifeTime);
    }

    public void SpawnCastEffect(Transform characterRoot, GameObject effectPrefab)
    {
        if (effectPrefab == null || characterRoot == null) return;
        Transform spawnPoint = characterRoot.Find("Effect Point");
        if (spawnPoint == null) spawnPoint = characterRoot;
        GameObject effect = Instantiate(effectPrefab, spawnPoint);
        effect.transform.localPosition = Vector3.zero;
        Destroy(effect, 2f);
    }

    // ==========================================
    // 获取技能最终的真实体力消耗 (支持向前预测与新状态)
    // ==========================================
    public int GetActualSkillCost(BattleEntity entity, SkillSlot slot, SkillSlot pairedSubSkill = null)
    {
        if (slot == null || slot.skillData == null) return 0;
        int cost = slot.skillData.GetStaminaCost(slot.level);

        bool hasAgile = entity.activeStatuses.ContainsKey(StatusType.Agile);
        bool hasExcited = entity.activeStatuses.ContainsKey(StatusType.Excited);
        bool hasOverdrawn = entity.activeStatuses.ContainsKey(StatusType.Overdrawn);

        // 前瞻预测（看这回合是不是要立刻上状态）
        if (pairedSubSkill != null && pairedSubSkill.skillData != null && pairedSubSkill.skillData.effects != null)
        {
            foreach (var effect in pairedSubSkill.skillData.effects)
            {
                if (effect is ApplyStatusEffect se && se.applyToSelf)
                {
                    if (se.statusType == StatusType.Agile) hasAgile = true;
                    if (se.statusType == StatusType.Excited) hasExcited = true;
                    if (se.statusType == StatusType.Overdrawn) hasOverdrawn = true;
                }
            }
        }

        // 透支状态绝对优先：招式不消耗体力
        if (hasOverdrawn) return 0;

        // 亢奋状态发威：所有招式体力消耗 + 1
        if (hasExcited) cost += 1;

        // 灵动状态发威：如果是闪避技能，体力消耗 -1
        if (slot.skillData.skillType == SkillType.Dodge && hasAgile)
        {
            cost = Mathf.Max(0, cost - 1);
        }

        // 负重效果：仅对攻击/闪避招式生效，防御招式不受负重影响（仅玩家）
        var skillType = slot.skillData.skillType;
        bool isMainSkillType = (skillType == SkillType.Attack || skillType == SkillType.Dodge);
        if (isMainSkillType && entity.isPlayer)
        {
            var loadState = entity.GetEffectiveLoadState();
            switch (loadState)
            {
                case GlobalBattleRules.LoadWeightState.Light:   cost = Mathf.Max(0, cost - 1); break;
                case GlobalBattleRules.LoadWeightState.Heavy:   cost += 1; break;
                case GlobalBattleRules.LoadWeightState.Extreme: cost += 2; break;
                // Medium: 无修正
            }
        }

        return cost;
    }

    private SkillSlot SelectSkillByWeight(List<KeyValuePair<SkillSlot, int>> weightedSkills)
    {
        if (weightedSkills == null || weightedSkills.Count == 0) return null;

        int totalWeight = 0;
        foreach (var kvp in weightedSkills) totalWeight += kvp.Value;

        if (totalWeight <= 0) return null;

        int randomPoint = Random.Range(0, totalWeight);
        int currentSum = 0;

        foreach (var kvp in weightedSkills)
        {
            currentSum += kvp.Value;
            if (randomPoint < currentSum)
            {
                return kvp.Key;
            }
        }
        return weightedSkills[weightedSkills.Count - 1].Key;
    }

    public void ShowBroadcast(string msg)
    {
        if (broadcastUIRoot != null) broadcastUIRoot.SetActive(true);
        if (broadcastText != null) broadcastText.text = msg;
        Debug.Log($"<color=#FFD700>[广播] {msg}</color>");
    }

    public void HideBroadcast()
    {
        if (broadcastUIRoot != null) broadcastUIRoot.SetActive(false);
    }

    // 【判断这个副技能是给自己上的 Buff，还是给别人上的 Debuff/炸弹】
    private bool IsSelfTargetSkill(SkillData skill)
    {
        if (skill == null || skill.effects == null || skill.effects.Count == 0) return true; // 没效果默认安全
        foreach (var effect in skill.effects)
        {
            if (effect != null && effect.IsHarmfulToTarget()) return false; // 只要有一个害人的效果，就是敌对技能
        }
        return true;
    }

    public IEnumerator RoutineActionBroadcast()
    {
        bool pSubIsSelf = currentPlayerSubSkill != null && IsSelfTargetSkill(currentPlayerSubSkill.skillData);
        bool eSubIsSelf = currentEnemySubSkill != null && IsSelfTargetSkill(currentEnemySubSkill.skillData);

        // 阶段 A：双方自身目的恢复道具（同时显示）
        bool pHasSelfSub = currentPlayerSubSkill != null && pSubIsSelf;
        bool eHasSelfSub = currentEnemySubSkill != null && eSubIsSelf;
        if (pHasSelfSub || eHasSelfSub)
        {
            if (pHasSelfSub) SpawnActionInfo(playerActionInfoAnchor, $"使用{GetSkillNameColored(currentPlayerSubSkill.skillData)}");
            if (eHasSelfSub) SpawnActionInfo(enemyActionInfoAnchor,  $"使用{GetSkillNameColored(currentEnemySubSkill.skillData)}");
            yield return new WaitForSeconds(1f);
            if (pHasSelfSub) ExecuteSecondaryAction(playerEntity, enemyEntity, currentPlayerSubSkill);
            if (eHasSelfSub) ExecuteSecondaryAction(enemyEntity, playerEntity, currentEnemySubSkill);
            yield return new WaitForSeconds(0.5f);
        }

        // 阶段 B：双方防守/闪避主技能（同时显示）
        bool pDefends = currentPlayerSkill != null && currentPlayerSkill.skillData != null &&
                        (currentPlayerSkill.skillData.skillType == SkillType.Defend ||
                         currentPlayerSkill.skillData.skillType == SkillType.Dodge);
        bool eDefends = currentEnemySkill != null && currentEnemySkill.skillData != null &&
                        (currentEnemySkill.skillData.skillType == SkillType.Defend ||
                         currentEnemySkill.skillData.skillType == SkillType.Dodge);
        if (pDefends || eDefends)
        {
            if (pDefends) SpawnActionInfo(playerActionInfoAnchor, $"使用{GetSkillNameColored(currentPlayerSkill.skillData)}");
            if (eDefends) SpawnActionInfo(enemyActionInfoAnchor,  $"使用{GetSkillNameColored(currentEnemySkill.skillData)}");
            yield return new WaitForSeconds(1f);
            if (pDefends) ApplyNonAttackSkills(playerEntity, currentPlayerSkill);
            if (eDefends) ApplyNonAttackSkills(enemyEntity,  currentEnemySkill);
            yield return new WaitForSeconds(0.5f);
        }

        // 阶段 C：双方有害副技能（同时显示）
        bool pHasHarmSub = currentPlayerSubSkill != null && !pSubIsSelf;
        bool eHasHarmSub = currentEnemySubSkill != null && !eSubIsSelf;
        if (pHasHarmSub || eHasHarmSub)
        {
            if (pHasHarmSub) SpawnActionInfo(playerActionInfoAnchor, $"使用{GetSkillNameColored(currentPlayerSubSkill.skillData)}");
            if (eHasHarmSub) SpawnActionInfo(enemyActionInfoAnchor,  $"使用{GetSkillNameColored(currentEnemySubSkill.skillData)}");
            yield return new WaitForSeconds(1f);
            if (pHasHarmSub) ExecuteSecondaryAction(playerEntity, enemyEntity, currentPlayerSubSkill);
            if (eHasHarmSub) ExecuteSecondaryAction(enemyEntity, playerEntity, currentEnemySubSkill);
            yield return new WaitForSeconds(0.5f);
        }

        isPlayerAttackResolved = false;
        isEnemyAttackResolved = false;

        // 阶段 D+E：攻击阶段（由 ProceedNextAttack 驱动）
        ProceedNextAttack();
    }

    public void ProceedNextAttack()
    {
        if (playerEntity.currentBasicLife <= 0 || enemyEntity.currentBasicLife <= 0)
        {
            CheckBattleEndOrNextTurn();
            return;
        }

        if (!isPlayerAttackResolved && currentPlayerSkill != null && currentPlayerSkill.skillData != null && currentPlayerSkill.skillData.skillType == SkillType.Attack)
        {
            isPlayerAttackResolved = true;
            StartCoroutine(DelayAttackState(new HitBarActionState(this), currentPlayerSkill.skillData, true));
            return;
        }

        if (!isEnemyAttackResolved && currentEnemySkill != null && currentEnemySkill.skillData != null && currentEnemySkill.skillData.skillType == SkillType.Attack)
        {
            isEnemyAttackResolved = true;
            StartCoroutine(DelayAttackState(new EnemyActionState(this), currentEnemySkill.skillData, false));
            return;
        }

        CheckBattleEndOrNextTurn();
    }

    private IEnumerator DelayAttackState(BattleState nextState, SkillData attackSkill, bool isPlayerAttacking)
    {
        Transform anchor = isPlayerAttacking ? playerActionInfoAnchor : enemyActionInfoAnchor;
        SpawnActionInfo(anchor, $"发动{GetSkillNameColored(attackSkill)}");
        yield return new WaitForSeconds(1.5f); // 1s显示 + 0.5s淡出，完全消失后进入下一状态
        ChangeState(nextState);
    }

    public void CheckBattleEndOrNextTurn()
    {
        HideBroadcast();
        if (playerEntity.currentBasicLife <= 0 || enemyEntity.currentBasicLife <= 0)
        {
            ChangeState(new BattleEndState(this));
            return;
        }
        Debug.Log("[Combat] 双方回合结束。");
        playerEntity.RecoverStamina();
        enemyEntity.RecoverStamina();
        ChangeState(new PreparationState(this));
    }

    public void SetupNewBattle(RoleData playerData, RoleData enemyData)
    {
        playerEntity.Initialize(playerData, true);
        enemyEntity.Initialize(enemyData, false);
        playerInfoUI.BindEntity(playerEntity);
        enemyInfoUI.BindEntity(enemyEntity);
        ChangeState(new BattleInitState(this));
    }

    public int TriggerPlayerEquipEffects(EquipTriggerTiming timing, SectionLevel? hitLevel)
    {
        if (GameManager.Instance == null) return 0;
        PlayerProfile profile = GameManager.Instance.playerProfile;

        int totalMod = 0;
        totalMod += RunEquipEffect(profile.equippedWeapon, timing, hitLevel);
        if (playerEntity.currentExtraLife > 0)
        {
            totalMod += RunEquipEffect(profile.equippedArmor, timing, hitLevel);
        }
        if (profile.equippedAccessories != null)
        {
            foreach (var acc in profile.equippedAccessories)
            {
                totalMod += RunEquipEffect(acc, timing, hitLevel);
            }
        }
        return totalMod;
    }

    private int RunEquipEffect(EquipmentData equip, EquipTriggerTiming timing, SectionLevel? hitLevel)
    {
        if (equip == null || equip.equipEffects == null) return 0;
        int mod = 0;
        foreach (var effect in equip.equipEffects)
        {
            if (effect.triggerTiming == timing)
            {
                mod += effect.Execute(playerEntity, enemyEntity, hitLevel, this);
            }
        }
        return mod;
    }

    public int GetSkillTypeBonus(BattleEntity entity, SkillType type)
    {
        if (!entity.isPlayer || GameManager.Instance == null) return 0;
        PlayerProfile profile = GameManager.Instance.playerProfile;

        int totalBonus = 0;
        System.Action<EquipmentData> checkEquip = (equip) =>
        {
            if (equip == null || equip.equipEffects == null) return;
            foreach (var effect in equip.equipEffects)
            {
                if (effect is GlobalBattleRules.SkillTypeModifierEquipEffect mod && mod.targetSkillType == type)
                    totalBonus += mod.bonusValue;
            }
        };

        checkEquip(profile.equippedWeapon);
        if (entity.currentExtraLife > 0)
        {
            checkEquip(profile.equippedArmor);
        }
        if (profile.equippedAccessories != null)
        {
            foreach (var acc in profile.equippedAccessories) checkEquip(acc);
        }

        return totalBonus;
    }
}
