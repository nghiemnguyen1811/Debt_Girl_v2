using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatContainer : MonoBehaviour, ILocalizableContainer
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI pendingLevelText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    private StatDataSO statData;
    private int pendingLevel;

    private void Start()
    {
        plusButton.onClick.AddListener(OnPlusClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
    }

    public void Configure(StatDataSO data)
    {
        statData = data;
        pendingLevel = 0;

        if (statData == null) return;

        iconImage.sprite = statData.icon;
        RefreshLocalizedTexts();
        RefreshUI();
        UpdatePendingUI();
    }

    //─────────────────────────────────────────────
    // Localization
    //─────────────────────────────────────────────
    public void RefreshLocalizedTexts()
    {
        if (statData == null) return;

        // Use "Stat Labels" as main table
        LocalizationManager.Instance.SetLocalizedText(statNameText, "Stat Labels", statData.statNameKey);
        LocalizationManager.Instance.SetLocalizedText(descriptionText, "Stat Labels", statData.statDescriptionKey);
    }

    //─────────────────────────────────────────────
    // Button Events
    //─────────────────────────────────────────────
    private void OnPlusClicked()
    {
        if (!StatUpgradeManager.Instance.TrySpendTempPoint()) return;

        pendingLevel++;
        UpdatePendingUI();
        StatUpgradeManager.Instance.UpdateStatUpgradeUI();
        AudioManager.Instance.PlayInteractSound(8);
    }

    private void OnMinusClicked()
    {
        if (pendingLevel <= 0) return;

        pendingLevel--;
        StatUpgradeManager.Instance.RefundTempPoint();
        UpdatePendingUI();
        StatUpgradeManager.Instance.UpdateStatUpgradeUI();
        AudioManager.Instance.PlayInteractSound(8);
    }

    //─────────────────────────────────────────────
    // UI Updates
    //─────────────────────────────────────────────
    private void UpdatePendingUI()
    {
        if (this == null || gameObject == null) return;
        if (pendingLevelText == null || pendingLevelText.gameObject == null) return;

        pendingLevelText.text = pendingLevel > 0 ? $"{pendingLevel}" : "0";
    }

    private void RefreshUI()
    {
        levelText.text = $"{statData.level}";
    }

    //─────────────────────────────────────────────
    // Logic
    //─────────────────────────────────────────────
    public void CommitPendingLevel()
    {
        if (pendingLevel <= 0) return;

        statData.level += pendingLevel;
        pendingLevel = 0;
        RefreshUI();
        UpdatePendingUI();
    }

    public void ResetPendingLevel()
    {
        pendingLevel = 0;
        UpdatePendingUI();
    }

    public void UpdateButtonStates()
    {
        if (this == null || gameObject == null) return;

        if (plusButton == null || plusButton.gameObject == null) return;
        if (minusButton == null || minusButton.gameObject == null) return;

        plusButton.interactable = StatUpgradeManager.Instance != null &&
                                  StatUpgradeManager.Instance.HasAvailablePoints();

        minusButton.interactable = pendingLevel > 0;
    }

    public void SyncFromData()
    {
        if (this == null || gameObject == null) return;

        pendingLevel = 0;
        RefreshUI();
        UpdatePendingUI();
        UpdateButtonStates();
    }

    //─────────────────────────────────────────────
    // Accessors
    //─────────────────────────────────────────────
    public int GetCurrentLevel() => statData != null ? statData.level : 1;
    public StatType GetStatType() => statData != null ? statData.statType : default;
    public int GetPendingLevel() => pendingLevel;
}
