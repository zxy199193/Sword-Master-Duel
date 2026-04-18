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

    // 【核心修复】：全部改为接收 SkillSlot 包装盒
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

    // 【核心修复】：参数类型改为 SkillSlot
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

            AIPhaseConfig currentPhase = new AIPhaseConfig();
            bool phaseFound = false;

            if (enemyEntity.roleData.aiPhases != null && enemyEntity.roleData.aiPhases.Count > 0)
            {
                var sortedPhases = new List<AIPhaseConfig>(enemyEntity.roleData.aiPhases);
                sortedPhases.Sort((a, b) => a.hpPercentageThreshold.CompareTo(b.hpPercentageThreshold));

                foreach (var phase in sortedPhases)
                {
                    if (hpPercentage <= phase.hpPercentageThreshold)
                    {
                        currentPhase = phase;
                        phaseFound = true;
                        break;
                    }
                }
                if (!phaseFound) currentPhase = sortedPhases[sortedPhases.Count - 1];
            }

            // 【核心修复】：键值对改为 SkillSlot
            var validMainSkills = new List<KeyValuePair<SkillSlot, int>>();
            var validSubSkills = new List<KeyValuePair<SkillSlot, int>>();

            if (phaseFound)
            {
                Debug.Log($"<color=#FFD700>[AI 决策] 血量 {hpPercentage:P0}，进入 AI 阶段 (阈值: {currentPhase.hpPercentageThreshold:P0})</color>");

                foreach (var slot in enemyEntity.runtimeSkills)
                {
                    if (slot == null || slot.skillData == null) continue;

                    // 【核心修复】：传入 level 获取动态消耗
                    if (slot.skillData.GetStaminaCost(slot.level) > enemyEntity.currentStamina) continue;

                    if (slot.skillData.skillType == SkillType.Attack || slot.skillData.skillType == SkillType.Defend || slot.skillData.skillType == SkillType.Dodge)
                    {
                        int weight = 10;
                        if (currentPhase.mainSkillWeights != null)
                        {
                            int idx = currentPhase.mainSkillWeights.FindIndex(w => w.skill != null && w.skill.skillName == slot.skillData.skillName);
                            if (idx >= 0) weight = currentPhase.mainSkillWeights[idx].weight;
                        }

                        if (weight > 0) validMainSkills.Add(new KeyValuePair<SkillSlot, int>(slot, weight));
                    }
                    else if (slot.skillData.skillType == SkillType.Special || (slot.skillData.skillType == SkillType.Item && slot.quantity > 0))
                    {
                        int weight = 10;
                        if (currentPhase.subSkillWeights != null)
                        {
                            int idx = currentPhase.subSkillWeights.FindIndex(w => w.skill != null && w.skill.skillName == slot.skillData.skillName);
                            if (idx >= 0) weight = currentPhase.subSkillWeights[idx].weight;
                        }

                        if (weight > 0) validSubSkills.Add(new KeyValuePair<SkillSlot, int>(slot, weight));
                    }
                }
            }

            currentEnemySkill = SelectSkillByWeight(validMainSkills);

            int mainSkillCost = currentEnemySkill != null ? currentEnemySkill.skillData.GetStaminaCost(currentEnemySkill.level) : 0;
            int remainingStamina = enemyEntity.currentStamina - mainSkillCost;
            float subProb = phaseFound ? currentPhase.subSkillProbability : 0.5f;

            if (Random.value < subProb && remainingStamina > 0 && validSubSkills.Count > 0)
            {
                validSubSkills.RemoveAll(kvp => kvp.Key.skillData.GetStaminaCost(kvp.Key.level) > remainingStamina);
                currentEnemySubSkill = SelectSkillByWeight(validSubSkills);
            }
        }

        string pMain = currentPlayerSkill != null ? currentPlayerSkill.skillData.skillName : "无";
        string pSub = currentPlayerSubSkill != null ? currentPlayerSubSkill.skillData.skillName : "无";
        string eMain = currentEnemySkill != null ? currentEnemySkill.skillData.skillName : "无";
        string eSub = currentEnemySubSkill != null ? currentEnemySubSkill.skillData.skillName : "无";

        Debug.Log($"<color=cyan>[Combat] 玩家: 主[{pMain}] 副[{pSub}] | 敌人: 主[{eMain}] 副[{eSub}]</color>");

        if (currentPlayerSkill != null) playerEntity.ConsumeStamina(currentPlayerSkill.skillData.GetStaminaCost(currentPlayerSkill.level));
        if (currentPlayerSubSkill != null) playerEntity.ConsumeStamina(currentPlayerSubSkill.skillData.GetStaminaCost(currentPlayerSubSkill.level));
        if (currentEnemySkill != null) enemyEntity.ConsumeStamina(currentEnemySkill.skillData.GetStaminaCost(currentEnemySkill.level));
        if (currentEnemySubSkill != null) enemyEntity.ConsumeStamina(currentEnemySubSkill.skillData.GetStaminaCost(currentEnemySubSkill.level));

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
            entity.tempDamageReduction = slot.skillData.GetBasicDefend(slot.level) + entity.roleData.strength;
            entity.tempHitWidthModifier = slot.skillData.GetHitAmend(slot.level);
        }
        else if (slot.skillData.skillType == SkillType.Dodge)
        {
            entity.tempHitWidthModifier = slot.skillData.GetHitAmend(slot.level) - entity.roleData.mentality;
        }
    }

    private void ExecuteSecondaryAction(BattleEntity user, BattleEntity target, SkillSlot slot)
    {
        if (slot == null || slot.skillData == null) return;

        if (slot.skillData.skillType == SkillType.Item)
        {
            if (slot.quantity <= 0) return;
            slot.quantity--;
            Debug.Log($"[{user.roleData.roleName}] 使用了道具 [{slot.skillData.skillName}]，剩余: {slot.quantity}");
        }
        else if (slot.skillData.skillType == SkillType.Special)
        {
            Debug.Log($"[{user.roleData.roleName}] 发动了特殊技能 [{slot.skillData.skillName}]");
        }
        else
        {
            return;
        }

        if (slot.skillData.castEffectPrefab != null)
        {
            SpawnCastEffect(user.transform, slot.skillData.castEffectPrefab);
        }

        if (slot.skillData.effects != null && slot.skillData.effects.Count > 0)
        {
            foreach (var effect in slot.skillData.effects)
            {
                if (effect == null) continue;
                effect.Execute(user, target, this, slot.level); // 传入 slot.level
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

    public IEnumerator RoutineActionBroadcast()
    {
        if (currentPlayerSubSkill != null && currentPlayerSubSkill.skillData != null)
        {
            ShowBroadcast($"我方使用了【{currentPlayerSubSkill.skillData.skillName}】");
            yield return new WaitForSeconds(2f);
            ExecuteSecondaryAction(playerEntity, enemyEntity, currentPlayerSubSkill);
        }

        if (currentEnemySubSkill != null && currentEnemySubSkill.skillData != null)
        {
            ShowBroadcast($"对方使用了【{currentEnemySubSkill.skillData.skillName}】");
            yield return new WaitForSeconds(2f);
            ExecuteSecondaryAction(enemyEntity, playerEntity, currentEnemySubSkill);
        }

        if (currentPlayerSkill != null && currentPlayerSkill.skillData != null && (currentPlayerSkill.skillData.skillType == SkillType.Defend || currentPlayerSkill.skillData.skillType == SkillType.Dodge))
        {
            ShowBroadcast($"我方进行了防御，使用【{currentPlayerSkill.skillData.skillName}】");
            yield return new WaitForSeconds(1f);
            ApplyNonAttackSkills(playerEntity, currentPlayerSkill);
        }

        if (currentEnemySkill != null && currentEnemySkill.skillData != null && (currentEnemySkill.skillData.skillType == SkillType.Defend || currentEnemySkill.skillData.skillType == SkillType.Dodge))
        {
            ShowBroadcast($"对方进行了防御，使用【{currentEnemySkill.skillData.skillName}】");
            yield return new WaitForSeconds(1f);
            ApplyNonAttackSkills(enemyEntity, currentEnemySkill);
        }

        HideBroadcast();
        isPlayerAttackResolved = false;
        isEnemyAttackResolved = false;
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