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

    [Header("Global Level Requirement")]
    [SerializeField] private bool useLevelRequirement = true;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        // Subscribe to GameManager event when the level changes
        GameManager.Instance.OnLevelChanged += RefreshAll;

        // Initial refresh on startup
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
        int currentLevel = GameManager.Instance.CurrentLevel;

        foreach (var app in appButtons)
        {
            if (!app.appButton) continue;

            if (!useLevelRequirement)
            {
                app.appButton.interactable = true;

                if (app.requiredLevelText)
                    app.requiredLevelText.gameObject.SetActive(false);

                continue;
            }

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

    /// <summary>
    /// Enables or disables level requirements for all apps.
    /// </summary>
    public void SetLevelRequirementActive(bool active)
    {
        useLevelRequirement = active;
        RefreshAll();
    }

    /// <summary>
    /// Returns whether level requirement is currently active.
    /// </summary>
    public bool IsLevelRequirementActive()
    {
        return useLevelRequirement;
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

    //[Header("Unlock Requirement")]
    [Min(1)] public int requiredLevel = 1;
}
