using DG.Tweening;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using URandom = UnityEngine.Random;

/// <summary>
/// Core manager for global game state, including player level, save/load coordination, 
/// and system-wide UI notifications (warnings).
/// </summary>
public class GameManager : SingletonMonobehaviour<GameManager>
{
    /// <summary>Event triggered when the player's level changes.</summary>
    public event Action OnLevelChanged;

    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields: Settings ===

    [Header("Level Configuration")]
    [SerializeField] private int maxLevel = 99;
    [SerializeField] private int currentLevel = 1;

    [Header("Debug Configuration")]
    [Tooltip("Master switch. If false, debug keys and warning texts are disabled.")]
    [SerializeField] private bool debugMode = true;

    [Tooltip("If true, certain interactions require a specific level.")]
    [SerializeField] private bool enableLevelRequirements = true;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields: UI & Messages ===

    [Header("Global UI References")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private float warningFadeDuration = 2f;

    [Header("System Messages - Mood")]
    [TextArea]
    [SerializeField]
    private string[] moodWarningMessages = {
        "기분이 부족합니다.",
        "너무 기분이 다운돼서 할 수 없습니다.",
        "먼저 기분을 회복하는 게 좋습니다.",
        "기분이 너무 낮습니다."
    };

    [Header("System Messages - Baking")]
    [TextArea]
    [SerializeField]
    private string[] plateFullMessages = {
        "접시가 모두 가득 찼습니다!",
        "빈 접시가 없습니다!",
        "지금은 굽을 수 없습니다 — 모든 트레이가 사용 중입니다.",
        "빈 접시가 필요합니다!",
        "이런! 그 케이크를 놓을 공간이 없습니다.",
        "굽기 전에 접시를 비우세요."
    };

    [Header("System Messages - Cooldown")]
    [TextArea]
    [SerializeField]
    private string[] cooldownMessages = {
        "아직은 쉴 때가 아니에요.",
        "조금만 기다렸다가 다시 쉬어요.",
        "너무 빨라요, 잠시 후에 다시 시도해요.",
        "아직 준비 중이에요. 잠깐만요!"
    };

    [Header("System Messages - Energy")]
    [TextArea]
    [SerializeField]
    private string[] energyMessages = {
        "에너지가 부족합니다.",
        "너무 피곤해서 할 수 없습니다.",
        "먼저 쉬는 게 좋습니다.",
        "이 행동은 더 많은 에너지가 필요합니다."
    };

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Internal State ===

    private Sequence warningSequence;

    public int CurrentLevel => currentLevel;
    public bool EnableLevelRequirements => enableLevelRequirements;
    public bool DebugMode => debugMode; // Public accessor if other scripts need to know

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Events ===

    protected override void Awake()
    {
        base.Awake();
        DataManager.Instance.LoadCachedSaveData();

        // Ensure warning text is hidden at start
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
            warningText.alpha = 0f;
        }
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(1);

        DataManager.Instance.NotifySceneReady();
        NotifyLevelChanged();
    }

    private void Update()
    {
        HandleDebugInput();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Public API: Warning System ===

    /// <summary>Displays a random warning related to low mood.</summary>
    public void ShowMoodWarning() => ShowRandomWarning(moodWarningMessages);

    /// <summary>Displays a random warning related to full plates.</summary>
    public void ShowPlateFullWarning() => ShowRandomWarning(plateFullMessages);

    /// <summary>Displays a random warning related to interaction cooldowns.</summary>
    public void ShowCooldownWarning() => ShowRandomWarning(cooldownMessages);

    /// <summary>Displays a random warning related to low energy.</summary>
    public void ShowEnergyWarning() => ShowRandomWarning(energyMessages);

    /// <summary>
    /// Selects a random message from the provided array and displays it.
    /// </summary>
    private void ShowRandomWarning(string[] messages)
    {
        if (messages == null || messages.Length == 0) return;
        ShowWarningText(messages[URandom.Range(0, messages.Length)]);
    }

    /// <summary>
    /// Displays the specific text with a bounce animation and fade out.
    /// Only works if DebugMode is enabled.
    /// </summary>
    public void ShowWarningText(string message)
    {
        // 1. Check if Debug Mode is ON
        if (!debugMode) return;

        if (warningText == null) return;

        // Reset existing animation
        if (warningSequence != null && warningSequence.IsActive())
            warningSequence.Kill();

        // Setup initial state
        warningText.gameObject.SetActive(true);
        warningText.text = message;
        warningText.transform.localScale = Vector3.one;
        warningText.alpha = 1f;

        // Play animation sequence
        warningSequence = DOTween.Sequence()
            .Append(warningText.transform.DOScale(Vector3.one * 1.2f, 0.1f).SetEase(Ease.OutBack))
            .Append(warningText.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack))
            .AppendInterval(0.5f)
            .Append(warningText.DOFade(0f, warningFadeDuration).SetEase(Ease.InOutQuad))
            .OnComplete(() => warningText.gameObject.SetActive(false));
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Public API: Level Management ===

    /// <summary>Increases player level by 1 if below max.</summary>
    public void IncreaseLevel()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;
        NotifyLevelChanged();
        AutoSave();
    }

    /// <summary>Sets the player level directly (clamped).</summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, maxLevel);
        NotifyLevelChanged();
        AutoSave();
    }

    /// <summary>Checks if player has reached the maximum level.</summary>
    public bool CheckMaxLevel() => currentLevel >= maxLevel;

    private void NotifyLevelChanged() => OnLevelChanged?.Invoke();

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Save & Load ===

    protected void AutoSave()
    {
        if (SaveManager.Data != null)
        {
            SaveManager.Data.playerLevel = currentLevel;
            SaveManager.SaveGame();
        }
    }

    public void ImportSaveData(SaveData d)
    {
        currentLevel = Mathf.Max(1, d.playerLevel);
        NotifyLevelChanged();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Debugging Tools ===

    private void HandleDebugInput()
    {
        // Only run if Editor AND DebugMode is active
        if (Application.isEditor && debugMode)
        {
            // L -> Level Up
            if (Input.GetKeyDown(KeyCode.L))
            {
                IncreaseLevel();
                Debug.Log($"[Debug] Level Up! New Level: {currentLevel}");
            }

            // M -> Money
            if (Input.GetKeyDown(KeyCode.M))
            {
                DebugAddMoney();
            }

            // R -> Reset
            if (Input.GetKeyDown(KeyCode.R))
            {
                DeleteSaveFile();
            }

            // F -> Fill Mood (New)
            if (Input.GetKeyDown(KeyCode.F))
            {
                DebugFillMood();
            }
        }
    }

    private void DebugAddMoney()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.ChangeMoneys(1000);
            Debug.Log("[Debug] Added 1000 Money.");
        }
    }

    /// <summary>
    /// Sets the player's mood to maximum (100).
    /// </summary>
    [ContextMenu("Debug: Fill Max Mood")]
    public void DebugFillMood()
    {
        if (PlayerControl.Instance != null)
        {
            // Assumes 'stats.mood.current' is accessible or has a setter method.
            // Using ApplyStatChange with a large number is usually the safest way to hit max cap
            // without accessing internal setters directly, unless direct access is public.
            PlayerControl.Instance.stats.ApplyStatChange(StatType.Mood, 1000f);

            // Force visual update if needed
            MoodManager.Instance.SetCurrentMoodVisual();

            Debug.Log("[Debug] Mood set to MAX.");
        }
    }

    [ContextMenu("Debug: Toggle Level Lock")]
    public void ToggleLevelLock()
    {
        enableLevelRequirements = !enableLevelRequirements;
        Debug.Log($"[Debug] Level Requirements set to: {enableLevelRequirements}");
        NotifyLevelChanged();
    }

    [ContextMenu("Debug: Reset Save")]
    public void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, "save.json");
        if (File.Exists(path)) File.Delete(path);

        PlayerPrefs.DeleteAll();
        currentLevel = 1;

        if (Application.isPlaying)
        {
            SaveManager.ClearSave();
            NotifyLevelChanged();
            AutoSave();
            Debug.Log("[Debug] Save file deleted & reset.");
        }
    }

    #endregion
}