using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatContainer : MonoBehaviour
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

        if (statData != null)
        {
            iconImage.sprite = statData.icon;
            statNameText.text = statData.statName;
            descriptionText.text = statData.description;
        }

        RefreshUI();
        UpdatePendingUI();
    }

    private void OnPlusClicked()
    {
        if (!StatUpgradeManager.Instance.TrySpendTempPoint()) return;

        pendingLevel++;
        UpdatePendingUI();
        StatUpgradeManager.Instance.UpdateStatUpgradeUI();
    }

    private void OnMinusClicked()
    {
        if (pendingLevel <= 0) return;

        pendingLevel--;
        StatUpgradeManager.Instance.RefundTempPoint();
        UpdatePendingUI();
        StatUpgradeManager.Instance.UpdateStatUpgradeUI();
    }

    private void UpdatePendingUI()
    {
        pendingLevelText.text = pendingLevel > 0 ? $"{pendingLevel}" : "0";
    }

    private void RefreshUI()
    {
        levelText.text = $"Level: {statData.level}";
    }

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
        plusButton.interactable = StatUpgradeManager.Instance.HasAvailablePoints();
        minusButton.interactable = pendingLevel > 0;
    }

    public int GetCurrentLevel() => statData != null ? statData.level : 0;
    public StatType GetStatType() => statData != null ? statData.statType : default;
    public StatDataSO GetStatData() => statData;
    public int GetPendingLevel() => pendingLevel;
}
