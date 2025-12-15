using UnityEngine;
using System;
using System.IO; // Required for file operations

public class GameManager : SingletonMonobehaviour<GameManager>
{
    public event Action OnLevelChanged;

    [Header("Level Limit")]
    [SerializeField] private int maxLevel = 99;

    [Header("Player Progress")]
    [SerializeField]
    private int currentLevel = 1;

    // ─────────────────────────────────────────────────────
    // DEBUG SETTINGS
    // ─────────────────────────────────────────────────────
    [Header("Debug Settings")]
    [Tooltip("Enable this to use keyboard shortcuts (L, M, R)")]
    [SerializeField] private bool enableDebugKeys = true;

    [Tooltip("Global switch to enable/disable level requirements for Apps")]
    [SerializeField] private bool enableLevelRequirements = true;

    public int CurrentLevel => currentLevel;
    public bool EnableLevelRequirements => enableLevelRequirements;

    protected override void Awake()
    {
        base.Awake();
        DataManager.Instance.LoadCachedSaveData();
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(1);

        DataManager.Instance.NotifySceneReady();
        NotifyLevelChanged();
    }

    // ─────────────────────────────────────────────────────
    // KEYBOARD INPUT LISTENER (Update Loop)
    // ─────────────────────────────────────────────────────
    private void Update()
    {
        // Only run in Editor and if Debug Keys are enabled
        if (Application.isEditor && enableDebugKeys)
        {
            // Press 'L' -> Level Up
            if (Input.GetKeyDown(KeyCode.L))
            {
                IncreaseLevel();
                Debug.Log($"[Debug Key] Level Up! Current Level: {currentLevel}");
            }

            // Press 'M' -> Add Money
            if (Input.GetKeyDown(KeyCode.M))
            {
                DebugAddMoney();
            }

            // Press 'R' -> Reset Save Data (Faster than Context Menu)
            if (Input.GetKeyDown(KeyCode.R))
            {
                DeleteSaveFile();
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    public void IncreaseLevel()
    {
        if (currentLevel >= maxLevel) return;

        currentLevel++;
        NotifyLevelChanged();
        AutoSave();
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, maxLevel);
        NotifyLevelChanged();
        AutoSave();
    }

    public bool CheckMaxLevel() => currentLevel >= maxLevel;

    private void NotifyLevelChanged()
    {
        OnLevelChanged?.Invoke();
    }

    protected void AutoSave()
    {
        if (SaveManager.Data != null)
        {
            SaveManager.Data.playerLevel = currentLevel;
            SaveManager.SaveGame();
        }
    }

    public void ImportSaveData(SaveData saveData)
    {
        currentLevel = Mathf.Max(1, saveData.playerLevel);
        NotifyLevelChanged();
    }

    private void DebugAddMoney()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.ChangeMoneys(1000);
            Debug.Log("[Debug Key] Added 1000 Money");
        }
    }

    // ─────────────────────────────────────────────────────
    // CONTEXT MENU TOOLS
    // ─────────────────────────────────────────────────────

    [ContextMenu("Debug: Toggle Level Lock")]
    public void ToggleLevelLock()
    {
        enableLevelRequirements = !enableLevelRequirements;
        Debug.Log($"[Debug] Level Requirements set to: {enableLevelRequirements}");
        NotifyLevelChanged();
    }

    [ContextMenu("Debug: Reset Save Data")]
    public void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, "save.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.LogWarning($"[RESET] Save file deleted at: {path}");
        }

        PlayerPrefs.DeleteAll();
        currentLevel = 1;

        if (Application.isPlaying)
        {
            SaveManager.ClearSave();
            NotifyLevelChanged();
            AutoSave();
            Debug.Log("[RESET] Level reset to 1. Check UI!");
        }
    }
}