using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUIManager : MonoBehaviour
{
    [Header("UI Text")]
    public Text levelTitleText;

    [Header("Enemy Icons (Requires 3)")]
    public Image[] enemyIcons = new Image[3];
    public Text[] enemyNameTexts = new Text[3];
    public Text[] enemyDescTexts = new Text[3];

    [Header("Visual Feedback")]
    [Tooltip("Overlay for defeated enemies")]
    public GameObject[] defeatedOverlays = new GameObject[3];
    [Tooltip("Indicator for the current enemy node")]
    public GameObject[] currentNodeIndicators = new GameObject[3];

    [Header("UI References")]
    public Button openRolePanelBtn;
    public RoleUIManager roleUIManager;

    [Header("Action Buttons")]
    public Button startBattleBtn;

    // ==========================================
    // Unity Lifecycle
    // ==========================================

    private void Start()
    {
        startBattleBtn.onClick.AddListener(OnStartBattleClicked);
        
        if (openRolePanelBtn != null && roleUIManager != null)
        {
            openRolePanelBtn.onClick.AddListener(() =>
            {
                roleUIManager.ShowPanel();
            });
        }
    }

    // ==========================================
    // Public Methods
    // ==========================================

    public void UpdateAndShow(LevelData levelData, int nodeIndex, List<RoleData> currentEnemies)
    {
        gameObject.SetActive(true);

        if (levelTitleText != null)
        {
            levelTitleText.text = $"关卡 {levelData.levelTitle}";
        }

        for (int i = 0; i < 3; i++)
        {
            if (i >= currentEnemies.Count || currentEnemies[i] == null)
            {
                if (enemyIcons.Length > i && enemyIcons[i] != null) enemyIcons[i].gameObject.SetActive(false);
                if (enemyNameTexts.Length > i && enemyNameTexts[i] != null) enemyNameTexts[i].gameObject.SetActive(false);
                if (enemyDescTexts.Length > i && enemyDescTexts[i] != null) enemyDescTexts[i].gameObject.SetActive(false);
                continue;
            }

            if (enemyIcons.Length > i && enemyIcons[i] != null)
            {
                enemyIcons[i].sprite = currentEnemies[i].roleModel;
                enemyIcons[i].gameObject.SetActive(true);
            }

            if (enemyNameTexts.Length > i && enemyNameTexts[i] != null)
            {
                enemyNameTexts[i].text = currentEnemies[i].roleName;
                enemyNameTexts[i].gameObject.SetActive(true);
            }

            if (enemyDescTexts.Length > i && enemyDescTexts[i] != null)
            {
                enemyDescTexts[i].text = currentEnemies[i].roleDescription;
                enemyDescTexts[i].gameObject.SetActive(true);
            }

            if (defeatedOverlays.Length > i && defeatedOverlays[i] != null)
                defeatedOverlays[i].SetActive(i < nodeIndex);

            if (currentNodeIndicators.Length > i && currentNodeIndicators[i] != null)
                currentNodeIndicators[i].SetActive(i == nodeIndex);
        }
    }

    // ==========================================
    // Private Methods
    // ==========================================

    private void OnStartBattleClicked()
    {
        gameObject.SetActive(false);
        GameManager.Instance.StartCombatNode();
    }
}