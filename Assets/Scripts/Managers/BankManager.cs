using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Handles player's debt logic, payment, and animated bank UI feedback.
/// </summary>
public class BankManager : SingletonMonobehaviour<BankManager>
{
    //─────────────────────────────────────────────
    // === Events ===
    //─────────────────────────────────────────────
    public event Action OnDebtPaid;

    //─────────────────────────────────────────────
    // === Inspector Settings ===
    //─────────────────────────────────────────────
    [Header("Debt Settings")]
    [SerializeField] private double initialDebt = 100;
    [SerializeField] private float debtMultiplier = 1.3f;
    [SerializeField] private float earlyRate = 1.15f;
    [SerializeField] private float lateRate = 1.05f;
    [SerializeField] private float smoothRange = 50f;

    [Header("Pay Debt Buttons")]
    [SerializeField] private Button payDebtButtonEnabled;
    [SerializeField] private Button payDebtButtonDisabled;

    [Header("UI Animation Elements")]
    [SerializeField] private CanvasGroup needToPayGroup;
    [SerializeField] private CanvasGroup congratsGroup;
    [SerializeField] private Image fillBar;
    [SerializeField] private Transform[] piggyTransforms;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI debtRemainingText;

    [Header("Animation Timings")]
    [SerializeField] private float fillDuration = 2f;
    [SerializeField] private float holdDuration = 1.5f;

    //─────────────────────────────────────────────
    // === Runtime Data ===
    //─────────────────────────────────────────────
    private double currentDebt;
    private bool isAnimating = false;

    //─────────────────────────────────────────────
    // === Unity Lifecycle ===
    //─────────────────────────────────────────────

    // [FIX] Changed logic to refresh UI immediately upon enabling
    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnLevelChanged += RecalculateDebtFromLevel;
        GameManager.Instance.OnLevelChanged += UpdateLevelUI;

        // Force update UI in case level changed while this panel was disabled
        RefreshAllUI();
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnLevelChanged -= RecalculateDebtFromLevel;
        GameManager.Instance.OnLevelChanged -= UpdateLevelUI;
    }

    private void Start()
    {
        SetupListeners();
        InitializeCanvasGroups();
        // RefreshAllUI is already called in OnEnable, but calling here is safe too
        RefreshAllUI();
    }

    private void Update()
    {
        // Continuously check to update button state if money changes from other sources
        if (!isAnimating)
        {
            RefreshPayButton();
        }
    }

    //─────────────────────────────────────────────
    // === Initialization Helpers ===
    //─────────────────────────────────────────────

    /// <summary>Set initial visibility of CanvasGroups.</summary>
    private void InitializeCanvasGroups()
    {
        if (needToPayGroup != null)
        {
            needToPayGroup.gameObject.SetActive(true);
            needToPayGroup.alpha = 1f;
        }

        if (congratsGroup != null)
        {
            congratsGroup.gameObject.SetActive(false);
            congratsGroup.alpha = 0f;
        }
    }

    /// <summary>Force UI refresh for debt and level.</summary>
    private void RefreshAllUI()
    {
        RecalculateDebtFromLevel();
        UpdateLevelUI();
    }

    /// <summary>Bind click listener for pay button.</summary>
    private void SetupListeners()
    {
        if (payDebtButtonEnabled != null)
        {
            payDebtButtonEnabled.onClick.RemoveAllListeners();
            payDebtButtonEnabled.onClick.AddListener(TryPayDebt);
        }
    }

    //─────────────────────────────────────────────
    // === Public Methods ===
    //─────────────────────────────────────────────

    /// <summary>Try paying debt if the player has enough money.</summary>
    public void TryPayDebt()
    {
        if (isAnimating) return;

        if (MoneyManager.Instance.HasEnoughMoney(currentDebt))
        {
            MoneyManager.Instance.ChangeMoneys(-currentDebt);

            // Safe null check for StatUpgradeManager
            if (StatUpgradeManager.Instance != null)
                StatUpgradeManager.Instance.AddStatPoint();

            // NOTE: Do NOT call IncreaseDebt() here.
            // It is called inside the Coroutine after the animation finishes.

            AudioManager.Instance.PlayInteractSound(1);

            StopAllCoroutines();
            StartCoroutine(PlayPayAnimation());

            OnDebtPaid?.Invoke();
        }
        else Debug.Log("Not enough coins to pay the debt!");
    }

    /// <summary>Update button state based on money availability.</summary>
    public void RefreshPayButton()
    {
        if (MoneyManager.Instance != null)
            TogglePayDebtButton(MoneyManager.Instance.HasEnoughMoney(currentDebt));
    }

    /// <summary>Switch between enabled and disabled pay buttons.</summary>
    public void TogglePayDebtButton(bool canPay)
    {
        if (payDebtButtonEnabled != null && payDebtButtonDisabled != null)
        {
            payDebtButtonEnabled.gameObject.SetActive(canPay);
            payDebtButtonDisabled.gameObject.SetActive(!canPay);
        }
    }

    /// <summary>Recalculate debt based on current level.</summary>
    public void RecalculateDebtFromLevel()
    {
        if (GameManager.Instance.CheckMaxLevel()) return;

        // If animating, do not update debt UI yet to avoid jumping numbers
        if (isAnimating) return;

        int level = GameManager.Instance.CurrentLevel;

        float growthFactor = Mathf.Lerp(earlyRate, lateRate, Mathf.Clamp01(level / smoothRange));
        float finalGrowth = growthFactor * debtMultiplier;

        currentDebt = Math.Round(initialDebt * Mathf.Pow(finalGrowth, level - 1), 2);
        UpdateDebtUI();
    }

    //─────────────────────────────────────────────
    // === Private Logic Methods ===
    //─────────────────────────────────────────────

    /// <summary>Increase level — triggers new debt calculation.</summary>
    private void IncreaseDebt()
    {
        GameManager.Instance.IncreaseLevel();
    }

    /// <summary>Update UI text and refresh pay button.</summary>
    private void UpdateDebtUI()
    {
        UIManager.Instance?.UpdateDebt(currentDebt);

        // Update display text if assigned
        if (debtRemainingText != null)
            debtRemainingText.text = DoubleUtilities.ToIdleNotation(currentDebt); // Directly use DoubleUtilities

        RefreshPayButton();
    }

    /// <summary>Refresh level label and play pop animation.</summary>
    private void UpdateLevelUI()
    {
        if (levelText == null) return;

        int level = GameManager.Instance.CurrentLevel;
        levelText.text = $"{level}";

        // Only play bounce animation if not inside the main payment sequence
        if (!isAnimating)
        {
            levelText.transform.DOKill(true);
            levelText.transform.localScale = Vector3.one;
            levelText.transform.DOScale(1.3f, 0.25f).SetLoops(2, LoopType.Yoyo);
            levelText.DOFade(0.3f, 0.15f).From(1f).SetLoops(2, LoopType.Yoyo);
        }
    }

    //─────────────────────────────────────────────
    // === CanvasGroup Helpers ===
    //─────────────────────────────────────────────

    /// <summary>Fade in and enable a CanvasGroup.</summary>
    private void ShowGroup(CanvasGroup group, float duration)
    {
        if (group == null) return;

        group.DOKill();
        group.gameObject.SetActive(true);
        group.alpha = 0f;
        group.DOFade(1f, duration);
    }

    /// <summary>Fade out and disable a CanvasGroup.</summary>
    private void HideGroup(CanvasGroup group, float duration)
    {
        if (group == null) return;

        group.DOKill();
        group.DOFade(0f, duration).OnComplete(() =>
        {
            group.gameObject.SetActive(false);
        });
    }

    //─────────────────────────────────────────────
    // === Payment Animation Sequence ===
    //─────────────────────────────────────────────

    /// <summary>Full animation sequence for paying debt.</summary>
    private IEnumerator PlayPayAnimation()
    {
        isAnimating = true;
        TogglePayDebtButton(false);

        // Hide "need to pay"
        HideGroup(needToPayGroup, 0.2f); // Slightly faster
        if (debtRemainingText != null) debtRemainingText.gameObject.SetActive(false);

        // Fill bar
        fillBar.fillAmount = 0;
        fillBar.DOFillAmount(1f, fillDuration).SetEase(Ease.InOutCubic);
        yield return new WaitForSeconds(fillDuration);

        // Piggy bounce
        foreach (var piggy in piggyTransforms)
        {
            if (piggy != null)
                piggy.DOScale(1.1f, 0.3f).SetLoops(2, LoopType.Yoyo);
        }
        yield return new WaitForSeconds(0.3f);

        // Max Level Case
        if (GameManager.Instance.CheckMaxLevel())
        {
            ShowGroup(congratsGroup, 0.8f);
            isAnimating = false;
            yield break;
        }

        // Normal Flow
        ShowGroup(congratsGroup, 0.8f);
        yield return new WaitForSeconds(holdDuration);

        fillBar.fillAmount = 0;
        HideGroup(congratsGroup, 0.3f);
        yield return new WaitForSeconds(0.3f);

        // Increase Level here (After congrats animation finishes)
        isAnimating = false; // Disable flag temporarily so RecalculateDebtFromLevel can run
        IncreaseDebt();
        isAnimating = true;  // Re-enable flag to handle the rest of UI reveal

        ShowGroup(needToPayGroup, 0.5f);
        if (debtRemainingText != null) debtRemainingText.gameObject.SetActive(true);

        // Final UI update to match new data
        UpdateLevelUI();

        yield return new WaitForSeconds(0.5f);

        isAnimating = false;
        RefreshPayButton();
    }
}