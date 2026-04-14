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

    public GameObject staminaPureNode; // 攻击/防御/特殊用的纯数字体力
    public Text staminaPureText;

    public GameObject staminaIconNode; // 闪避用的带图标体力
    public Text staminaIconText;

    public GameObject hitAmendIconNode; // 统一后的命中修正节点 (带图标)
    public Text hitAmendIconText;

    public GameObject durationNode;    // 持续时间节点
    public Text durationText;

    public GameObject quantityNode;    // 道具数量
    public Text quantityText;

    [Header("迷你打击条 (Mini Hit Bar)")]
    public GameObject miniHitBarRoot;
    public GameObject miniSectionPrefab;

    private SkillData boundSkill;
    private Action<SkillData> onSelectedCallback;

    // ==========================================
    // 公共接口
    // ==========================================

    /// <summary>
    /// 初始化技能子项，现在需要传入 caster 来计算动态数值
    /// </summary>
    public void Init(SkillData skill, BattleEntity caster, Action<SkillData> callback)
    {
        boundSkill = skill;
        onSelectedCallback = callback;

        if (nameText != null) nameText.text = skill.skillName;
        if (descriptionText != null) descriptionText.text = skill.description;
        if (skillIcon != null) skillIcon.sprite = skill.skillIcon;

        if (levelText != null)
        {
            levelText.gameObject.SetActive(skill.skillType != SkillType.Item);
            levelText.text = $"Lv.{skill.skillLevel}";
        }

        HideAllDynamicNodes();

        // 根据类型装配，传入 caster 用于公式计算
        switch (skill.skillType)
        {
            case SkillType.Attack: SetupAttackSkill(skill); break;
            case SkillType.Defend: SetupDefendSkill(skill); break;
            case SkillType.Dodge: SetupDodgeSkill(skill); break;
            case SkillType.Special: SetupSpecialSkill(skill, caster); break;
            case SkillType.Item: SetupItem(skill); break;
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClicked);
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
    // 内部装配逻辑 (严格匹配需求说明)
    // ==========================================

    private void SetupAttackSkill(SkillData skill)
    {
        SetNodeText(damageNode, damageText, skill.basicDamage.ToString());
        SetNodeText(staminaPureNode, staminaPureText, skill.staminaCost.ToString());
        DrawMiniHitBar(skill);
    }

    private void SetupDefendSkill(SkillData skill)
    {
        SetNodeText(defendNode, defendText, skill.basicDefend.ToString());
        SetNodeText(staminaPureNode, staminaPureText, skill.staminaCost.ToString());
        // (6) 命中修正：统一使用带图标节点
        SetNodeText(hitAmendIconNode, hitAmendIconText, $"+{skill.hitAmend}");
    }

    private void SetupDodgeSkill(SkillData skill)
    {
        SetNodeText(staminaIconNode, staminaIconText, skill.staminaCost.ToString());
        SetNodeText(hitAmendIconNode, hitAmendIconText, skill.hitAmend.ToString());
    }

    private void SetupSpecialSkill(SkillData skill, BattleEntity caster)
    {
        // 显示体力消耗
        SetNodeText(staminaPureNode, staminaPureText, skill.staminaCost.ToString());

        // 【核心修正】：通过 skill 内部逻辑直接抓取 BaseDuration 并进行公式计算
        int baseDur = skill.GetBaseDuration();

        // 计算公式：基础持续时间 + 向下取整(精神/6)，保底1回合
        int extraDur = Mathf.FloorToInt(caster.roleData.mentality / 6f);
        int totalDur = Mathf.Max(1, baseDur + extraDur);

        SetNodeText(durationNode, durationText, totalDur.ToString());
    }

    private void SetupItem(SkillData skill)
    {
        SetNodeText(quantityNode, quantityText, $"x{skill.quantity}");
    }

    private void HideAllDynamicNodes()
    {
        if (damageNode) damageNode.SetActive(false);
        if (defendNode) defendNode.SetActive(false);
        if (staminaPureNode) staminaPureNode.SetActive(false);
        if (staminaIconNode) staminaIconNode.SetActive(false);
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

    private void DrawMiniHitBar(SkillData skill)
    {
        if (miniHitBarRoot == null || miniSectionPrefab == null) return;
        miniHitBarRoot.SetActive(true);
        foreach (Transform child in miniHitBarRoot.transform) { Destroy(child.gameObject); }

        HitBarConfig config = skill.GetLeveledHitBarConfig();
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

    private void OnSelectClicked() => onSelectedCallback?.Invoke(boundSkill);
}