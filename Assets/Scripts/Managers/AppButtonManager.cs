using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all app buttons in the UI. 
/// Handles unlocking apps based on the player's current level.
/// </summary>
public class AppButtonManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("App Buttons")]
    [SerializeField] private AppButtonData[] appButtons;

    // [REMOVED] Local 'useLevelRequirement' bool. 
    // Now using GameManager global setting.

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelChanged -= RefreshAll;
    }

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes all app buttons based on the player's current level.
    /// </summary>
    public void RefreshAll()
    {
        if (GameManager.Instance == null) return;

        int currentLevel = GameManager.Instance.CurrentLevel;

        // [UPDATED] Check the global debug setting from GameManager
        bool useLevelRequirement = GameManager.Instance.EnableLevelRequirements;

        foreach (var app in appButtons)
        {
            if (!app.appButton) continue;

            // If global requirement is disabled, unlock everything
            if (!useLevelRequirement)
            {
                app.appButton.interactable = true;

                if (app.requiredLevelText)
                    app.requiredLevelText.gameObject.SetActive(false);

                continue;
            }

            // Normal logic: Check if level meets requirement
            bool unlocked = currentLevel >= app.requiredLevel;

            app.appButton.interactable = unlocked;

            if (app.requiredLevelText)
            {
                app.requiredLevelText.gameObject.SetActive(!unlocked);

                if (!unlocked)
                    app.requiredLevelText.text = $"Lv. {app.requiredLevel}";
            }
        }
    }
}

/// <summary>
/// Represents a single app button with its UI references and unlock requirement.
/// </summary>
[Serializable]
public class AppButtonData
{
    [Header("App Info")]
    public AppType appType;

    [Header("UI References")]
    public Button appButton;
    public TextMeshProUGUI requiredLevelText;

    // Level required to unlock this app
    [Min(1)] public int requiredLevel = 1;
}