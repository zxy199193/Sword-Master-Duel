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

    [Header("固定飘字位置 (UI Anchors)")]
    public Transform playerDamageAnchor;
    public Transform enemyDamageAnchor;

    [Header("特效预制体 (Effect Prefabs)")]
    public GameObject hitEffectPrefab;
    public float hitEffectLifeTime = 0.5f;

    [Header("Broadcast UI (广播系统)")]
    public GameObject broadcastUIRoot;
    public UnityEngine.UI.Text broadcastText;

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

        // 动态获取费用并扣除体力
        if (currentPlayerSkill != null) playerEntity.ConsumeStamina(GetActualSkillCost(playerEntity, currentPlayerSkill, currentPlayerSubSkill));
        if (currentPlayerSubSkill != null) playerEntity.ConsumeStamina(GetActualSkillCost(playerEntity, currentPlayerSubSkill));
        if (currentEnemySkill != null) enemyEntity.ConsumeStamina(GetActualSkillCost(enemyEntity, currentEnemySkill, currentEnemySubSkill));
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
                    SpawnDamagePopup(entity.isPlayer, "<color=#8B0000>扎伤 -3</color>", 1);
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

    public void SpawnDamagePopup(Vector3 targetPosition, string textContent, int hitLevel)
    {
        GameObject prefabToSpawn = normalDamagePrefab;
        if (hitLevel == 0) prefabToSpawn = missPrefab;
        else if (hitLevel >= 2) prefabToSpawn = critDamagePrefab;

        if (prefabToSpawn == null || floatingTextCanvas == null) return;
        Vector3 worldPos = targetPosition + Vector3.up * 1.5f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        GameObject popupObj = Instantiate(prefabToSpawn, floatingTextCanvas.transform);
        popupObj.transform.position = new Vector3(screenPos.x, screenPos.y, 0);

        DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
        if (popupScript != null) popupScript.Setup(textContent);
    }
    private void ApplyNonAttackSkills(BattleEntity entity, SkillSlot slot)
    {
        if (slot == null || slot.skillData == null) return;

        if (slot.skillData.skillType == SkillType.Defend)
        {
            entity.tempDamageReduction = slot.skillData.GetBasicDefend(slot.level) + entity.GetFinalStrength();
            entity.tempHitWidthModifier = slot.skillData.GetHitAmend(slot.level);
        }
        else if (slot.skillData.skillType == SkillType.Dodge)
        {
            // 如果有灵动状态，额外提供 6 点判定缩减！
            float agileBonus = entity.activeStatuses.ContainsKey(StatusType.Agile) ? 6f : 0f;
            entity.tempHitWidthModifier = slot.skillData.GetHitAmend(slot.level) - entity.GetFinalMentality() - agileBonus;
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
                    SpawnDamagePopup(target.isPlayer, "<color=#888888>免疫!</color>", 0);
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

        // (1) 我方 - 增益/恢复道具 (如：步法、调息)
        if (currentPlayerSubSkill != null && pSubIsSelf)
        {
            ShowBroadcast($"我方使用了【{currentPlayerSubSkill.skillData.skillName}】");
            yield return new WaitForSeconds(1.5f);
            ExecuteSecondaryAction(playerEntity, enemyEntity, currentPlayerSubSkill);
        }

        // (2) 敌方 - 增益/恢复道具
        if (currentEnemySubSkill != null && eSubIsSelf)
        {
            ShowBroadcast($"对方使用了【{currentEnemySubSkill.skillData.skillName}】");
            yield return new WaitForSeconds(1.5f);
            ExecuteSecondaryAction(enemyEntity, playerEntity, currentEnemySubSkill);
        }

        // (3) & (4) 我方和敌方 - 防御/闪避
        if (currentPlayerSkill != null && currentPlayerSkill.skillData != null && (currentPlayerSkill.skillData.skillType == SkillType.Defend || currentPlayerSkill.skillData.skillType == SkillType.Dodge))
        {
            ShowBroadcast($"我方使用了【{currentPlayerSkill.skillData.skillName}】");
            yield return new WaitForSeconds(1f);
            ApplyNonAttackSkills(playerEntity, currentPlayerSkill);
        }

        if (currentEnemySkill != null && currentEnemySkill.skillData != null && (currentEnemySkill.skillData.skillType == SkillType.Defend || currentEnemySkill.skillData.skillType == SkillType.Dodge))
        {
            ShowBroadcast($"对方使用了【{currentEnemySkill.skillData.skillName}】");
            yield return new WaitForSeconds(1f);
            ApplyNonAttackSkills(enemyEntity, currentEnemySkill);
        }

        // (5) 我方 - 有害特殊/道具 (如：炸弹)
        if (currentPlayerSubSkill != null && !pSubIsSelf)
        {
            ShowBroadcast($"我方使用了【{currentPlayerSubSkill.skillData.skillName}】");
            yield return new WaitForSeconds(1.5f);
            ExecuteSecondaryAction(playerEntity, enemyEntity, currentPlayerSubSkill);
        }

        // (6) 敌方 - 有害特殊/道具
        if (currentEnemySubSkill != null && !eSubIsSelf)
        {
            ShowBroadcast($"对方使用了【{currentEnemySubSkill.skillData.skillName}】");
            yield return new WaitForSeconds(1.5f);
            ExecuteSecondaryAction(enemyEntity, playerEntity, currentEnemySubSkill);
        }

        HideBroadcast();
        isPlayerAttackResolved = false;
        isEnemyAttackResolved = false;

        // (7) & (8) 进入攻击互砍结算
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
            StartCoroutine(DelayAttackState(new HitBarActionState(this), $"我方发动了攻击，使用【{currentPlayerSkill.skillData.skillName}】"));
            return;
        }

        if (!isEnemyAttackResolved && currentEnemySkill != null && currentEnemySkill.skillData != null && currentEnemySkill.skillData.skillType == SkillType.Attack)
        {
            isEnemyAttackResolved = true;
            StartCoroutine(DelayAttackState(new EnemyActionState(this), $"对方发动了攻击，使用【{currentEnemySkill.skillData.skillName}】"));
            return;
        }

        CheckBattleEndOrNextTurn();
    }

    private IEnumerator DelayAttackState(BattleState nextState, string msg)
    {
        ShowBroadcast(msg);
        yield return new WaitForSeconds(1f);
        HideBroadcast();
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
        totalMod += RunEquipEffect(profile.equippedArmor, timing, hitLevel);
        foreach (var acc in profile.equippedAccessories)
        {
            totalMod += RunEquipEffect(acc, timing, hitLevel);
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
}