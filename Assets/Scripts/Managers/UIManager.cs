using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIManager : SingletonMonobehaviour<UIManager>
{
    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI debtText;
    [SerializeField] private TextMeshProUGUI statPointText;

    [Header("UI Panels")]
    [SerializeField] private GameObject postPanel;
    [SerializeField] private GameObject upgradePanel;

    [Header("UI Buttons")]
    [SerializeField] private GameObject payDebtButton;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.25f;

    private Tween moneyTween, debtTween;

    // ======================== Public Methods ========================

    public void UpdateMoney(double totalCoins, bool animate = true)
    {
        UpdateText(moneyText, totalCoins, animate, ref moneyTween);
    }

    public void UpdateDebt(double debtAmount, bool animate = true)
    {
        UpdateText(debtText, debtAmount, animate, ref debtTween);
    }

    public void UpdateStatPoints(int total)
    {
        if (statPointText != null)
            statPointText.text = $"Stat Points: {total}";
    }

    public void TogglePayDebtButton(bool show)
    {
        if (payDebtButton != null)
            payDebtButton.SetActive(show);
    }

    public void TogglePostPanel(bool show)
    {
        if (postPanel != null)
            postPanel.SetActive(show);
    }

    public void ToggleUpgradePanel(bool show)
    {
        if (upgradePanel == null) return;

        upgradePanel.SetActive(show);

        if (!show)
            StatUpgradeManager.Instance.CancelUpgrade();
    }

    // ======================== Private Methods ========================

    private void UpdateText(TextMeshProUGUI textMesh, double value, bool animate, ref Tween tween)
    {
        if (textMesh == null) return;

        textMesh.text = DoubleUtilities.ToIdleNotation(value);

        if (!animate) return;

        tween?.Kill();
        tween = textMesh.transform
            .DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 0.8f)
            .SetEase(Ease.OutBack);
    }
}
