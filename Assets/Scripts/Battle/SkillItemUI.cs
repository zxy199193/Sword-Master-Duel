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
    private Action<SkillSlot> onSelectedCallback;

    public void Init(SkillSlot slot, BattleEntity caster, Action<SkillSlot> callback)
    {
        boundSlot = slot;
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

        HideAllDynamicNodes();

        switch (skillData.skillType)
        {
            case SkillType.Attack: SetupAttackSkill(slot); break;
            case SkillType.Defend: SetupDefendSkill(slot); break;
            case SkillType.Dodge: SetupDodgeSkill(slot); break;
            case SkillType.Special: SetupSpecialSkill(slot, caster); break;
            case SkillType.Item: SetupItem(slot); break;
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

    private void SetupAttackSkill(SkillSlot slot)
    {
        SetNodeText(damageNode, damageText, slot.skillData.GetBasicDamage(slot.level).ToString());
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());
        DrawMiniHitBar(slot);
    }

    private void SetupDefendSkill(SkillSlot slot)
    {
        SetNodeText(defendNode, defendText, slot.skillData.GetBasicDefend(slot.level).ToString());
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());
        SetNodeText(hitAmendIconNode, hitAmendIconText, $"+{slot.skillData.GetHitAmend(slot.level)}");
    }

    private void SetupDodgeSkill(SkillSlot slot)
    {
        // 闪避也直接用通用的 Pure 体力节点
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());
        SetNodeText(hitAmendIconNode, hitAmendIconText, slot.skillData.GetHitAmend(slot.level).ToString());
    }

    private void SetupSpecialSkill(SkillSlot slot, BattleEntity caster)
    {
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());
        int baseDur = slot.skillData.GetBaseDuration(slot.level);
        int extraDur = Mathf.FloorToInt(caster.roleData.mentality / 6f);
        int totalDur = Mathf.Max(1, baseDur + extraDur);
        SetNodeText(durationNode, durationText, totalDur.ToString());
    }

    private void SetupItem(SkillSlot slot)
    {
        SetNodeText(quantityNode, quantityText, $"x{slot.quantity}");
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