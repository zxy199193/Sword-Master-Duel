using UnityEngine;
using UnityEngine.UI;
using System;

public class RoleSkillItemUI : MonoBehaviour
{
    [Header("UI 节点 - 基础信息")]
    public Text nameText;
    public Image iconImage;
    public Text descText;
    public Text levelText;

    [Header("UI 节点 - 操作")]
    public GameObject equippedBadge;
    public Button actionBtn;
    public Text actionBtnText;

    [Header("UI 节点 - 商店专属")]
    public GameObject priceNode;
    public Text priceText;

    [Header("UI 节点 - 动态属性展示")]
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

    [Header("迷你打击条配置")]
    public GameObject miniHitBarRoot;
    public GameObject miniSectionPrefab;

    // ==========================================
    // Public Methods - Setup
    // ==========================================

    public void Setup(SkillSlot skillSlot, bool isEquipped, bool canUnequip, Action<SkillSlot> onActionClicked)
    {
        PopulateBasicInfo(skillSlot);

        var tooltipTrigger = GetComponent<SkillTooltipTrigger>();
        if (tooltipTrigger != null) tooltipTrigger.BindSkill(skillSlot.skillData);

        if (equippedBadge) equippedBadge.SetActive(isEquipped);
        if (priceNode) priceNode.SetActive(false);

        if (actionBtn != null)
        {
            actionBtn.interactable = true;
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

    public void SetupForShop(SkillSlot skillSlot, int price, bool canAfford, string btnText, Action<SkillSlot> onActionClicked)
    {
        PopulateBasicInfo(skillSlot);

        var tooltipTrigger = GetComponent<SkillTooltipTrigger>();
        if (tooltipTrigger != null) tooltipTrigger.BindSkill(skillSlot.skillData);

        if (equippedBadge) equippedBadge.SetActive(false);

        if (priceNode) priceNode.SetActive(true);
        if (priceText)
        {
            priceText.text = price.ToString();
            priceText.color = canAfford ? Color.white : Color.red;
        }

        if (actionBtn != null)
        {
            actionBtn.gameObject.SetActive(true);
            actionBtn.interactable = canAfford;
            if (actionBtnText) actionBtnText.text = btnText;

            actionBtn.onClick.RemoveAllListeners();
            actionBtn.onClick.AddListener(() => onActionClicked?.Invoke(skillSlot));
        }
    }

    // ==========================================
    // Private Methods - UI Generation
    // ==========================================

    private void PopulateBasicInfo(SkillSlot skillSlot)
    {
        if (nameText) nameText.text = skillSlot.skillData.skillName;
        if (iconImage) iconImage.sprite = skillSlot.skillData.skillIcon;
        if (descText) descText.text = skillSlot.skillData.description;

        if (levelText)
        {
            levelText.gameObject.SetActive(skillSlot.skillData.skillType != SkillType.Item);
            levelText.text = "Lv." + skillSlot.level;
        }

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
            
        SetNodeText(durationNode, durationText, Mathf.Max(1, baseDur + extraDur).ToString());
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