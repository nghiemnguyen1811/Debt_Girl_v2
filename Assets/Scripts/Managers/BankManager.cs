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
    // === 🔔 Events ===
    //─────────────────────────────────────────────
    public event Action OnDebtPaid;

    //─────────────────────────────────────────────
    // === ⚙️ Inspector Settings ===
    //─────────────────────────────────────────────
    [Header("Debt Settings")]
    [SerializeField] private double initialDebt = 100;
    //[SerializeField] private float debtMultiplier = 1.5f;
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

    [Header("Animation Timings")]
    [SerializeField] private float fillDuration = 2f;
    [SerializeField] private float holdDuration = 1.5f;

    //─────────────────────────────────────────────
    // === 💾 Runtime Data ===
    //─────────────────────────────────────────────
    private double currentDebt;
    private bool isAnimating = false;

    //─────────────────────────────────────────────
    // === 🌿 Unity Lifecycle ===
    //─────────────────────────────────────────────
    private void Start()
    {
        SetupListeners();
        SubscribeToGameManagerEvents();
        InitializeCanvasGroups();
        RefreshAllUI();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelChanged -= RecalculateDebtFromLevel;
            GameManager.Instance.OnLevelChanged -= UpdateLevelUI;
        }
    }

    //─────────────────────────────────────────────
    // === 🧩 Initialization Helpers ===
    //─────────────────────────────────────────────
    /// <summary>Subscribe to GameManager events.</summary>
    private void SubscribeToGameManagerEvents()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnLevelChanged += RecalculateDebtFromLevel;
        GameManager.Instance.OnLevelChanged += UpdateLevelUI;
    }

    /// <summary>Initialize CanvasGroup visibility on start.</summary>
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

    /// <summary>Force initial UI refresh (debt + level).</summary>
    private void RefreshAllUI()
    {
        RecalculateDebtFromLevel();
        UpdateLevelUI();
    }

    /// <summary>Bind listener for the Pay Debt button.</summary>
    private void SetupListeners()
    {
        if (payDebtButtonEnabled != null)
            payDebtButtonEnabled.onClick.AddListener(TryPayDebt);
    }

    //─────────────────────────────────────────────
    // === 🌍 Public Methods ===
    //─────────────────────────────────────────────

    /// <summary>Attempt to pay debt if enough money is available.</summary>
    public void TryPayDebt()
    {
        if (isAnimating) return;

        if (MoneyManager.Instance.HasEnoughMoney(currentDebt))
        {
            MoneyManager.Instance.ChangeMoneys(-currentDebt);
            StatUpgradeManager.Instance.AddStatPoint();
            IncreaseDebt();
            AudioManager.Instance.PlayInteractSound(1);

            StopAllCoroutines();
            StartCoroutine(PlayPayAnimation());

            OnDebtPaid?.Invoke();
        }
        else Debug.Log("Not enough coins to pay the debt!");
    }

    /// <summary>Refresh button states based on player's money.</summary>
    public void RefreshPayButton()
    {
        TogglePayDebtButton(MoneyManager.Instance.HasEnoughMoney(currentDebt));
    }

    /// <summary>Toggle between active/inactive pay buttons.</summary>
    public void TogglePayDebtButton(bool canPay)
    {
        if (payDebtButtonEnabled != null && payDebtButtonDisabled != null)
        {
            payDebtButtonEnabled.gameObject.SetActive(canPay);
            payDebtButtonDisabled.gameObject.SetActive(!canPay);
        }
    }

    /// <summary>Recalculate debt according to player level.</summary>
    public void RecalculateDebtFromLevel()
    {
        int level = GameManager.Instance.CurrentLevel;
        float growthFactor = Mathf.Lerp(earlyRate, lateRate, Mathf.Clamp01(level / smoothRange));
        currentDebt = Math.Round(initialDebt * Mathf.Pow(growthFactor, level - 1), 2);
        UpdateDebtUI();
    }

    //─────────────────────────────────────────────
    // === 🔒 Private Methods ===
    //─────────────────────────────────────────────

    /// <summary>Increase player level and trigger debt update.</summary>
    private void IncreaseDebt()
    {
        GameManager.Instance.IncreaseLevel();
    }

    /// <summary>Update UI text & buttons to reflect current debt.</summary>
    private void UpdateDebtUI()
    {
        UIManager.Instance?.UpdateDebt(currentDebt);
        RefreshPayButton();
    }

    /// <summary>Refresh level text and play pop animation.</summary>
    private void UpdateLevelUI()
    {
        if (levelText == null) return;

        int level = GameManager.Instance.CurrentLevel;
        levelText.text = $"{level}";

        levelText.transform.DOKill(true);
        levelText.transform.localScale = Vector3.one;
        levelText.transform.DOScale(1.3f, 0.25f).SetLoops(2, LoopType.Yoyo);
        levelText.DOFade(0.3f, 0.15f).From(1f).SetLoops(2, LoopType.Yoyo);
    }

    //─────────────────────────────────────────────
    // === 🧭 CanvasGroup Helpers ===
    //─────────────────────────────────────────────

    /// <summary>Fade in and activate a CanvasGroup.</summary>
    private void ShowGroup(CanvasGroup group, float duration)
    {
        if (group == null) return;

        group.DOKill();
        group.gameObject.SetActive(true);
        group.alpha = 0f;
        group.DOFade(1f, duration);
    }

    /// <summary>Fade out and deactivate a CanvasGroup.</summary>
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
    // === 💫 Animation Sequence ===
    //─────────────────────────────────────────────

    /// <summary>Play the full debt payment visual sequence.</summary>
    private IEnumerator PlayPayAnimation()
    {
        isAnimating = true;
        TogglePayDebtButton(false);

        // Step 1: hide “You need to pay”
        HideGroup(needToPayGroup, 0f);

        // Step 2: fill the yellow bar
        fillBar.fillAmount = 0;
        fillBar.DOFillAmount(1f, fillDuration).SetEase(Ease.InOutCubic);
        yield return new WaitForSeconds(fillDuration);

        // Step 3: piggy bounce animation
        foreach (var piggy in piggyTransforms)
        {
            if (piggy != null)
                piggy.DOScale(1.1f, 0.3f).SetLoops(2, LoopType.Yoyo);
        }

        yield return new WaitForSeconds(0.3f);

        // Step 4: show congratulations
        ShowGroup(congratsGroup, 0.8f);
        yield return new WaitForSeconds(holdDuration);

        // Step 5: reset UI
        fillBar.fillAmount = 0;
        HideGroup(congratsGroup, 0.3f);
        yield return new WaitForSeconds(0.3f);
        ShowGroup(needToPayGroup, 0.5f);

        // Step 6: refresh UI after animation
        yield return new WaitForSeconds(0.5f);
        UpdateDebtUI();

        isAnimating = false;
    }
}
