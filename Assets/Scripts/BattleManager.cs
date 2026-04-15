using System.Collections;
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

    [Header("固定飘字位置 (UI Anchors)")]
    public Transform playerDamageAnchor; // 玩家受击时的飘字生成点
    public Transform enemyDamageAnchor;  // 敌人受击时的飘字生成点

    [Header("特效预制体 (Effect Prefabs)")]
    public GameObject hitEffectPrefab; // 拖入你的击中帧动画预制体
    public float hitEffectLifeTime = 0.5f;

    [Header("Broadcast UI (广播系统)")]
    public GameObject broadcastUIRoot;      // 包含底图和文本的根节点
    public UnityEngine.UI.Text broadcastText; // 显示信息的 Text 文本

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
    [HideInInspector] public SkillData currentEnemySubSkill;

    // ==========================================
    // 战斗序列追踪 (Queue Tracking)
    // ==========================================
    [HideInInspector] public bool isPlayerAttackResolved;
    [HideInInspector] public bool isEnemyAttackResolved;

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

        // ==========================================
        // AI 决策层 2.0 (多阶段带权重双技能轮盘)
        // ==========================================
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

            var validMainSkills = new List<KeyValuePair<SkillData, int>>();
            var validSubSkills = new List<KeyValuePair<SkillData, int>>();

            if (phaseFound)
            {
                Debug.Log($"<color=#FFD700>[AI 决策] 血量 {hpPercentage:P0}，进入 AI 阶段 (阈值: {currentPhase.hpPercentageThreshold:P0})</color>");

                foreach (var skill in enemyEntity.runtimeSkills)
                {
                    if (skill == null) continue;
                    if (skill.staminaCost > enemyEntity.currentStamina) continue;

                    if (skill.skillType == SkillType.Attack || skill.skillType == SkillType.Defend || skill.skillType == SkillType.Dodge)
                    {
                        int weight = 10;
                        if (currentPhase.mainSkillWeights != null)
                        {
                            int idx = currentPhase.mainSkillWeights.FindIndex(w => w.skill != null && w.skill.skillName == skill.skillName);
                            if (idx >= 0) weight = currentPhase.mainSkillWeights[idx].weight;
                        }

                        if (weight > 0) validMainSkills.Add(new KeyValuePair<SkillData, int>(skill, weight));
                    }
                    else if (skill.skillType == SkillType.Special || (skill.skillType == SkillType.Item && skill.quantity > 0))
                    {
                        int weight = 10;
                        if (currentPhase.subSkillWeights != null)
                        {
                            int idx = currentPhase.subSkillWeights.FindIndex(w => w.skill != null && w.skill.skillName == skill.skillName);
                            if (idx >= 0) weight = currentPhase.subSkillWeights[idx].weight;
                        }

                        if (weight > 0) validSubSkills.Add(new KeyValuePair<SkillData, int>(skill, weight));
                    }
                }
            }

            currentEnemySkill = SelectSkillByWeight(validMainSkills);

            int mainSkillCost = currentEnemySkill != null ? currentEnemySkill.staminaCost : 0;
            int remainingStamina = enemyEntity.currentStamina - mainSkillCost;
            float subProb = phaseFound ? currentPhase.subSkillProbability : 0.5f;

            if (Random.value < subProb && remainingStamina > 0 && validSubSkills.Count > 0)
            {
                validSubSkills.RemoveAll(kvp => kvp.Key.staminaCost > remainingStamina);
                currentEnemySubSkill = SelectSkillByWeight(validSubSkills);
            }
        }

        string pMain = currentPlayerSkill != null ? currentPlayerSkill.skillName : "无";
        string pSub = currentPlayerSubSkill != null ? currentPlayerSubSkill.skillName : "无";
        string eMain = currentEnemySkill != null ? currentEnemySkill.skillName : "无";
        string eSub = currentEnemySubSkill != null ? currentEnemySubSkill.skillName : "无";

        Debug.Log($"<color=cyan>[Combat] 玩家: 主[{pMain}] 副[{pSub}] | 敌人: 主[{eMain}] 副[{eSub}]</color>");

        // Phase 1: 仅扣除体力 (必须瞬间结算，防止透支)
        if (currentPlayerSkill != null) playerEntity.ConsumeStamina(currentPlayerSkill.staminaCost);
        if (currentPlayerSubSkill != null) playerEntity.ConsumeStamina(currentPlayerSubSkill.staminaCost);
        if (currentEnemySkill != null) enemyEntity.ConsumeStamina(currentEnemySkill.staminaCost);
        if (currentEnemySubSkill != null) enemyEntity.ConsumeStamina(currentEnemySubSkill.staminaCost);

        // Phase 2: 把控制权交给专门的演出状态，开始播片！
        ChangeState(new ActionBroadcastState(this));
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

        // 1. 计算 2D 世界里的绝对坐标 (头顶 1.5 单位)
        Vector3 worldPos = targetPosition + Vector3.up * 1.5f;

        // 2. 【核心修复】：将世界坐标翻译为 UI 屏幕像素坐标！
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 3. 实例化预制体 (不要直接在 Instantiate 里传 worldPos)
        GameObject popupObj = Instantiate(prefabToSpawn, floatingTextCanvas.transform);

        // 4. 将转换后的屏幕坐标赋值给 UI
        // 强制把 Z 轴归零，防止 UI 深度计算错误导致隐形
        popupObj.transform.position = new Vector3(screenPos.x, screenPos.y, 0);

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

        // 【新增】：播放专属释放特效！
        if (skill.castEffectPrefab != null)
        {
            SpawnCastEffect(user.transform, skill.castEffectPrefab);
        }

        // 2. 遍历执行所有附加多态特效
        if (skill.effects != null && skill.effects.Count > 0)
        {
            foreach (var effect in skill.effects)
            {
                if (effect == null) continue;
                effect.Execute(user, target, this, skill.skillLevel);
            }
        }
    }

    /// <summary>
    /// 升级版：在固定锚点周围随机位置生成飘字，防止重叠
    /// </summary>
    public void SpawnDamagePopup(bool isPlayerTakingDamage, string textContent, int hitLevel)
    {
        GameObject prefabToSpawn = normalDamagePrefab;
        if (hitLevel == 0) prefabToSpawn = missPrefab;
        else if (hitLevel >= 2) prefabToSpawn = critDamagePrefab;

        if (prefabToSpawn == null) return;

        Transform targetAnchor = isPlayerTakingDamage ? playerDamageAnchor : enemyDamageAnchor;
        if (targetAnchor == null) return;

        // 1. 正常实例化
        GameObject popupObj = Instantiate(prefabToSpawn, targetAnchor);

        // 2. 【核心优化】：计算随机偏移量
        // 在半径 50 像素的圆圈内随机找个点 (适用于 UI 坐标)
        // 如果你觉得散得太开或太近，调整 50f 这个数值即可
        float randomRadius = 50f;
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * randomRadius;

        // 3. 应用偏移
        // 因为我们是直接作为 Anchor 的子物体，所以修改 localPosition 最准确
        popupObj.transform.localPosition = new Vector3(randomOffset.x, randomOffset.y, 0);

        DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
        if (popupScript != null) popupScript.Setup(textContent);
    }

    /// <summary>
    /// 在目标角色的 Effect Point 点播放击中特效
    /// </summary>
    public void SpawnHitEffect(Transform characterRoot)
    {
        if (hitEffectPrefab == null || characterRoot == null) return;

        // 1. 【精准定位】：寻找名为 "Effect Point" 的子节点
        Transform spawnPoint = characterRoot.Find("Effect Point");

        // 如果没找到（防呆处理），就回退到使用根节点
        if (spawnPoint == null) spawnPoint = characterRoot;

        // 2. 实例化特效
        GameObject effect = Instantiate(hitEffectPrefab, spawnPoint);
        effect.transform.localPosition = Vector3.zero;

        // 3. 【核心优化】：不需要新脚本，直接在这里指定几秒后销毁
        // 这样你就不用去特效预制体里挂脚本或者点动画事件了
        Destroy(effect, hitEffectLifeTime);
    }

    /// <summary>
    /// 在目标角色的 Effect Point 点播放指定的技能/道具特效
    /// </summary>
    public void SpawnCastEffect(Transform characterRoot, GameObject effectPrefab)
    {
        if (effectPrefab == null || characterRoot == null) return;

        // 精准寻找 Effect Point 节点
        Transform spawnPoint = characterRoot.Find("Effect Point");
        if (spawnPoint == null) spawnPoint = characterRoot;

        // 实例化并重置坐标
        GameObject effect = Instantiate(effectPrefab, spawnPoint);
        effect.transform.localPosition = Vector3.zero;

        // 自动销毁：给特效2秒的存活时间，防止内存泄漏（如果你的特效自带粒子自动销毁，这句也相当于加个双保险）
        Destroy(effect, 2f);
    }

    /// <summary>
    /// 轮盘赌算法：根据权重从池子中抽取技能
    /// </summary>
    private SkillData SelectSkillByWeight(List<KeyValuePair<SkillData, int>> weightedSkills)
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
        return weightedSkills[weightedSkills.Count - 1].Key; // 理论上不会走到这里，加个保底
    }
    // ==========================================
    // 战斗广播与停顿演出系统
    // ==========================================

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
        // （1）我方副技能
        if (currentPlayerSubSkill != null)
        {
            ShowBroadcast($"我方使用了【{currentPlayerSubSkill.skillName}】");
            yield return new WaitForSeconds(2f); // 停顿2秒
            ExecuteSecondaryAction(playerEntity, enemyEntity, currentPlayerSubSkill);
        }

        // （2）对方副技能
        if (currentEnemySubSkill != null)
        {
            ShowBroadcast($"对方使用了【{currentEnemySubSkill.skillName}】");
            yield return new WaitForSeconds(2f); // 停顿2秒
            ExecuteSecondaryAction(enemyEntity, playerEntity, currentEnemySubSkill);
        }

        // （3）我方防御/闪避
        if (currentPlayerSkill != null && (currentPlayerSkill.skillType == SkillType.Defend || currentPlayerSkill.skillType == SkillType.Dodge))
        {
            ShowBroadcast($"我方进行了防御，使用【{currentPlayerSkill.skillName}】");
            yield return new WaitForSeconds(1f); // 停顿1秒
            ApplyNonAttackSkills(playerEntity, currentPlayerSkill);
        }

        // （4）对方防御/闪避
        if (currentEnemySkill != null && (currentEnemySkill.skillType == SkillType.Defend || currentEnemySkill.skillType == SkillType.Dodge))
        {
            ShowBroadcast($"对方进行了防御，使用【{currentEnemySkill.skillName}】");
            yield return new WaitForSeconds(1f); // 停顿1秒
            ApplyNonAttackSkills(enemyEntity, currentEnemySkill);
        }

        HideBroadcast();

        // 准备进入攻击环节
        isPlayerAttackResolved = false;
        isEnemyAttackResolved = false;
        ProceedNextAttack();
    }

    /// <summary>
    /// 攻击行为轮转发牌器：保证双方都能把技能打完
    /// </summary>
    public void ProceedNextAttack()
    {
        // 【核心修复】：每次准备发牌前，先检查一下生死簿！
        // 如果有人已经倒下了（可能是刚被砍死，也可能是演出时被道具毒死），直接叫停，进入结算。
        if (playerEntity.currentBasicLife <= 0 || enemyEntity.currentBasicLife <= 0)
        {
            CheckBattleEndOrNextTurn();
            return;
        }

        // 5. 检查我方是否需要攻击
        if (!isPlayerAttackResolved && currentPlayerSkill != null && currentPlayerSkill.skillType == SkillType.Attack)
        {
            isPlayerAttackResolved = true;
            StartCoroutine(DelayAttackState(new HitBarActionState(this), $"我方发动了攻击，使用【{currentPlayerSkill.skillName}】"));
            return;
        }

        // 6. 检查对方是否需要攻击
        if (!isEnemyAttackResolved && currentEnemySkill != null && currentEnemySkill.skillType == SkillType.Attack)
        {
            isEnemyAttackResolved = true;
            // 注意：这里默认你有 EnemyActionState 这个脚本。如果没有，可能名字不一样，请对应修改。
            StartCoroutine(DelayAttackState(new EnemyActionState(this), $"对方发动了攻击，使用【{currentEnemySkill.skillName}】"));
            return;
        }

        // 如果双方都没出击，或者攻击均已结算完毕，才进行生死结算
        CheckBattleEndOrNextTurn();
    }

    private IEnumerator DelayAttackState(BattleState nextState, string msg)
    {
        ShowBroadcast(msg);
        yield return new WaitForSeconds(1f); // 呼出打击条前，停顿展示1秒
        HideBroadcast();
        ChangeState(nextState);
    }

    public void CheckBattleEndOrNextTurn()
    {
        HideBroadcast();

        // 在所有人的攻击都打完后，再结算生命值（完美支持同归于尽机制！）
        if (playerEntity.currentBasicLife <= 0 || enemyEntity.currentBasicLife <= 0)
        {
            ChangeState(new BattleEndState(this));
            return;
        }

        // 正常进入下一回合
        Debug.Log("[Combat] 双方回合结束。");
        playerEntity.RecoverStamina();
        enemyEntity.RecoverStamina();
        ChangeState(new PreparationState(this));
    }
}