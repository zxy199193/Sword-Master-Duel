using UnityEngine;
using UnityEngine.UI;
using System;

public class RoleSkillItemUI : MonoBehaviour
{
    [Header("基础节点")]
    public Text nameText;
    public Image iconImage;
    public Text descText;
    public Text levelText;

    [Header("操作节点")]
    public GameObject equippedBadge;
    public Button actionBtn;
    public Text actionBtnText;

    [Header("展示节点 (Dynamic Nodes)")]
    public GameObject damageNode;
    public Text damageText;

    public GameObject defendNode;
    public Text defendText;

    public GameObject staminaPureNode;
    public Text staminaPureText;

    public GameObject hitAmendIconNode;
    public Text hitAmendIconText;

    public GameObject durationNode;
    public Text durationText;

    public GameObject quantityNode;
    public Text quantityText;

    [Header("迷你打击条 (Mini Hit Bar)")]
    public GameObject miniHitBarRoot;
    public GameObject miniSectionPrefab;

    public void Setup(SkillSlot skillSlot, bool isEquipped, bool canUnequip, Action<SkillSlot> onActionClicked)
    {
        if (nameText) nameText.text = skillSlot.skillData.skillName;
        if (iconImage) iconImage.sprite = skillSlot.skillData.skillIcon;
        if (descText) descText.text = skillSlot.skillData.description;

        if (levelText)
        {
            levelText.gameObject.SetActive(skillSlot.skillData.skillType != SkillType.Item);
            levelText.text = "Lv." + skillSlot.level;
        }

        if (equippedBadge) equippedBadge.SetActive(isEquipped);

        HideAllDynamicNodes();
        switch (skillSlot.skillData.skillType)
        {
            case SkillType.Attack: SetupAttackSkill(skillSlot); break;
            case SkillType.Defend: SetupDefendSkill(skillSlot); break;
            case SkillType.Dodge: SetupDodgeSkill(skillSlot); break;
            case SkillType.Special: SetupSpecialSkill(skillSlot); break;
            case SkillType.Item: SetupItem(skillSlot); break;
        }

        DrawMiniHitBar(skillSlot);

        if (actionBtn != null)
        {
            actionBtn.onClick.RemoveAllListeners();
            if (isEquipped)
            {
                if (!canUnequip) actionBtn.gameObject.SetActive(false);
                else
                {
                    actionBtn.gameObject.SetActive(true);
                    if (actionBtnText) actionBtnText.text = "卸下";
                }
            }
            else
            {
                actionBtn.gameObject.SetActive(true);
                if (actionBtnText) actionBtnText.text = "装备";
            }
            actionBtn.onClick.AddListener(() => onActionClicked?.Invoke(skillSlot));
        }
    }

    private void SetupAttackSkill(SkillSlot slot)
    {
        SetNodeText(damageNode, damageText, slot.skillData.GetBasicDamage(slot.level).ToString());
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());
    }

    private void SetupDefendSkill(SkillSlot slot)
    {
        SetNodeText(defendNode, defendText, slot.skillData.GetBasicDefend(slot.level).ToString());
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());
        SetNodeText(hitAmendIconNode, hitAmendIconText, $"+{slot.skillData.GetHitAmend(slot.level)}");
    }

    private void SetupDodgeSkill(SkillSlot slot)
    {
        // 【核心修改】：闪避直接用通用的 Pure 体力节点
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());
        SetNodeText(hitAmendIconNode, hitAmendIconText, slot.skillData.GetHitAmend(slot.level).ToString());
    }

    private void SetupSpecialSkill(SkillSlot slot)
    {
        SetNodeText(staminaPureNode, staminaPureText, slot.skillData.GetStaminaCost(slot.level).ToString());

        int baseDur = slot.skillData.GetBaseDuration(slot.level);
        int extraDur = 0;
        if (GameManager.Instance != null && GameManager.Instance.playerProfile != null)
        {
            extraDur = Mathf.FloorToInt(GameManager.Instance.playerProfile.GetFinalMentality() / 6f);
        }
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

        if (slot.skillData.skillType != SkillType.Attack)
        {
            miniHitBarRoot.SetActive(false);
            return;
        }

        miniHitBarRoot.SetActive(true);

        foreach (Transform child in miniHitBarRoot.transform)
        {
            Destroy(child.gameObject);
        }

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
}