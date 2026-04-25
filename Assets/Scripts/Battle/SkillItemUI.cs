using UnityEngine;
using UnityEngine.UI;
using System;

public class SkillItemUI : MonoBehaviour
{
    [Header("通用元素 (Common)")]
    public Text nameText;
    public Text levelText;
    public Text descriptionText;
    public Image skillIcon;
    public Button selectButton;

    [Header("展示节点 (Dynamic Nodes)")]
    public GameObject damageNode;
    public Text damageText;

    public GameObject defendNode;
    public Text defendText;

    public GameObject staminaPureNode; // 统一的所有技能体力节点
    public Text staminaPureText;

    public GameObject hitAmendIconNode; // 命中修正节点
    public Text hitAmendIconText;

    public GameObject durationNode;    // 持续时间节点
    public Text durationText;

    public GameObject quantityNode;    // 道具数量
    public Text quantityText;

    [Header("迷你打击条 (Mini Hit Bar)")]
    public GameObject miniHitBarRoot;
    public GameObject miniSectionPrefab;

    private SkillSlot boundSlot;
    private BattleEntity boundCaster;
    private BattleManager boundManager;
    private Action<SkillSlot> onSelectedCallback;

    public void Init(SkillSlot slot, BattleEntity caster, Action<SkillSlot> callback, BattleManager manager = null)
    {
        boundSlot = slot;
        boundCaster = caster;
        boundManager = manager;
        onSelectedCallback = callback;
        SkillData skillData = slot.skillData;

        if (nameText != null) nameText.text = skillData.skillName;
        if (descriptionText != null) descriptionText.text = skillData.description;
        if (skillIcon != null) skillIcon.sprite = skillData.skillIcon;

        if (levelText != null)
        {
            levelText.gameObject.SetActive(skillData.skillType != SkillType.Item);
            levelText.text = $"Lv.{slot.level}";
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClicked);

        var tooltipTrigger = GetComponent<SkillTooltipTrigger>();
        if (tooltipTrigger != null) tooltipTrigger.BindSkill(skillData);

        // 默认以修正模式刷新
        RefreshStats(showModified: true);
    }

    /// <summary>
    /// 刷新技能数值显示。showModified=true 时显示经过属性修正的数值
    /// </summary>
    public void RefreshStats(bool showModified)
    {
        HideAllDynamicNodes();

        switch (boundSlot.skillData.skillType)
        {
            case SkillType.Attack:  SetupAttackSkill(showModified); break;
            case SkillType.Defend:  SetupDefendSkill(showModified); break;
            case SkillType.Dodge:   SetupDodgeSkill(showModified); break;
            case SkillType.Special: SetupSpecialSkill(showModified); break;
            case SkillType.Item:    SetupItem(); break;
        }
    }

    public void SetAvailable(bool isAvailable)
    {
        if (selectButton != null) selectButton.interactable = isAvailable;
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = isAvailable ? 1f : 0.5f;
        cg.blocksRaycasts = isAvailable;
    }

    // ==========================================
    // Private Setup Methods
    // ==========================================

    private void SetupAttackSkill(bool showModified)
    {
        int baseDamage = Mathf.RoundToInt(boundSlot.skillData.GetBasicDamage(boundSlot.level));
        int staminaCost = Mathf.RoundToInt(boundSlot.skillData.GetStaminaCost(boundSlot.level));

        if (showModified && boundCaster != null)
        {
            // 修正伤害：(基础 + 力量×2) × 武器倍率，取整
            float weaponFactor = 1.0f;
            if (boundCaster.isPlayer && GameManager.Instance != null)
            {
                var equip = GameManager.Instance.playerProfile.equippedWeapon;
                if (equip != null) weaponFactor = equip.atkFactor;
            }
            else
            {
                var equip = boundCaster.roleData?.equippedWeapon;
                if (equip != null) weaponFactor = equip.atkFactor;
            }
            int modifiedDamage = Mathf.RoundToInt((baseDamage + boundCaster.GetFinalStrength() * 1) * weaponFactor);
            SetNodeText(damageNode, damageText, modifiedDamage.ToString());

            // 修正体力：走 GetActualSkillCost 逻辑
            if (boundManager != null)
                staminaCost = boundManager.GetActualSkillCost(boundCaster, boundSlot);
        }
        else
        {
            SetNodeText(damageNode, damageText, baseDamage.ToString());
        }

        SetNodeText(staminaPureNode, staminaPureText, staminaCost.ToString());
        DrawMiniHitBar(boundSlot);
    }

    private void SetupDefendSkill(bool showModified)
    {
        int baseDefend = Mathf.RoundToInt(boundSlot.skillData.GetBasicDefend(boundSlot.level));
        int staminaCost = Mathf.RoundToInt(boundSlot.skillData.GetStaminaCost(boundSlot.level));

        if (showModified && boundCaster != null)
        {
            // 修正防御：基础防御 + 耐力
            int modifiedDefend = baseDefend + boundCaster.GetFinalEndurance();
            SetNodeText(defendNode, defendText, modifiedDefend.ToString());

            if (boundManager != null)
                staminaCost = boundManager.GetActualSkillCost(boundCaster, boundSlot);
        }
        else
        {
            SetNodeText(defendNode, defendText, baseDefend.ToString());
        }

        SetNodeText(staminaPureNode, staminaPureText, staminaCost.ToString());
        SetNodeText(hitAmendIconNode, hitAmendIconText, $"+{boundSlot.skillData.GetHitAmend(boundSlot.level)}");
    }

    private void SetupDodgeSkill(bool showModified)
    {
        int staminaCost = boundSlot.skillData.GetStaminaCost(boundSlot.level);
        float hitAmend = boundSlot.skillData.GetHitAmend(boundSlot.level);

        if (showModified && boundCaster != null && boundManager != null)
        {
            staminaCost = boundManager.GetActualSkillCost(boundCaster, boundSlot);
            float agileBonus = boundCaster.activeStatuses.ContainsKey(StatusType.Agile) ? 6f : 0f;
            hitAmend = hitAmend - boundCaster.GetFinalMentality() - agileBonus;
        }

        SetNodeText(staminaPureNode, staminaPureText, staminaCost.ToString());
        SetNodeText(hitAmendIconNode, hitAmendIconText, hitAmend.ToString());
    }

    private void SetupSpecialSkill(bool showModified)
    {
        int staminaCost = boundSlot.skillData.GetStaminaCost(boundSlot.level);

        if (showModified && boundCaster != null && boundManager != null)
            staminaCost = boundManager.GetActualSkillCost(boundCaster, boundSlot);

        SetNodeText(staminaPureNode, staminaPureText, staminaCost.ToString());

        int baseDur = boundSlot.skillData.GetBaseDuration(boundSlot.level);
        int extraDur = boundCaster != null ? Mathf.FloorToInt(boundCaster.GetFinalMentality() / 6f) : 0;
        int totalDur = Mathf.Max(1, baseDur + extraDur);
        SetNodeText(durationNode, durationText, totalDur.ToString());
    }

    private void SetupItem()
    {
        SetNodeText(quantityNode, quantityText, $"x{boundSlot.quantity}");
    }

    private void HideAllDynamicNodes()
    {
        if (damageNode) damageNode.SetActive(false);
        if (defendNode) defendNode.SetActive(false);
        if (staminaPureNode) staminaPureNode.SetActive(false);
        if (hitAmendIconNode) hitAmendIconNode.SetActive(false);
        if (durationNode) durationNode.SetActive(false);
        if (quantityNode) quantityNode.SetActive(false);
        if (miniHitBarRoot) miniHitBarRoot.SetActive(false);
    }

    private void SetNodeText(GameObject node, Text textComp, string value)
    {
        if (node != null && textComp != null)
        {
            node.SetActive(true);
            textComp.text = value;
        }
    }

    private void DrawMiniHitBar(SkillSlot slot)
    {
        if (miniHitBarRoot == null || miniSectionPrefab == null) return;
        miniHitBarRoot.SetActive(true);
        foreach (Transform child in miniHitBarRoot.transform) { Destroy(child.gameObject); }

        HitBarConfig config = slot.skillData.GetLeveledHitBarConfig(slot.level);
        if (config.sections == null) return;

        float totalWidth = miniHitBarRoot.GetComponent<RectTransform>().rect.width;
        foreach (var section in config.sections)
        {
            GameObject go = Instantiate(miniSectionPrefab, miniHitBarRoot.transform);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2((section.width / 100f) * totalWidth, 0);
            rt.anchoredPosition = new Vector2((section.axisPosition / 100f) * totalWidth - (totalWidth / 2f), 0);

            Image img = go.GetComponent<Image>();
            if (img != null) img.color = GlobalBattleRules.GetSectionColor(section.level);
        }
    }

    private void OnSelectClicked() => onSelectedCallback?.Invoke(boundSlot);
}