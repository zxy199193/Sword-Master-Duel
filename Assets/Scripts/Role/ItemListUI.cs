using UnityEngine;
using UnityEngine.UI;
using System;

public class ItemListUI : MonoBehaviour
{
    public Transform contentRoot;
    public GameObject bagItemPrefab;
    public Button closeBtn;

    // 【修改点】：泛型改为 SkillSlot
    private Action<SkillSlot> onEquipAction;
    private Action onUnequipAction;

    private void Awake()
    {
        if (closeBtn) closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
    }

    // 【修改点】：参数改为 SkillSlot
    public void OpenList(SkillSlot currentEquipped, Action<SkillSlot> onEquip, Action onUnequip)
    {
        gameObject.SetActive(true);
        onEquipAction = onEquip;
        onUnequipAction = onUnequip;

        foreach (Transform child in contentRoot) Destroy(child.gameObject);

        PlayerProfile profile = GameManager.Instance.playerProfile;

        if (currentEquipped != null && currentEquipped.skillData != null)
        {
            CreateItemNode(currentEquipped, true);
        }

        foreach (var slot in profile.storageSkillsAndItems)
        {
            // 【修改点】：通过 slot.skillData 去判断类型
            if (slot != null && slot.skillData != null && slot.skillData.skillType == SkillType.Item)
            {
                CreateItemNode(slot, false);
            }
        }
    }

    private void CreateItemNode(SkillSlot slot, bool isEquipped)
    {
        GameObject go = Instantiate(bagItemPrefab, contentRoot);
        BagItemUI itemUI = go.GetComponent<BagItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(slot, isEquipped, (clickedSlot) =>
            {
                if (isEquipped) onUnequipAction?.Invoke();
                else onEquipAction?.Invoke(clickedSlot);

                gameObject.SetActive(false);
            });
        }
    }
}