using UnityEngine;
using TMPro;
using DG.Tweening;

public class MoneyManager : SingletonMonobehaviour<MoneyManager>
{
    [Header("Coin Settings")]
    private int totalCoins = 0;

    [Header("DoTween Settings")]
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField] private float punchDuration = 0.3f;

    private Tween punchTween;

    [Header("UI & Display")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Particle Effect")]
    [SerializeField] private GameObject moneyParticlePrefab;

    void Start()
    {
        UpdateCoinUI(immediate: true);
    }

    public void AddCoins(int amount, Vector3 worldPosition)
    {
        totalCoins += amount;
        UpdateCoinUI();

        // Spawn coin particle
        if (moneyParticlePrefab != null)
        {
            MoneyParticle moneyParticle = (MoneyParticle)PoolManager.Instance.
                ReuseComponent(moneyParticlePrefab, worldPosition, Quaternion.identity);

            moneyParticle.Configure(amount);
            moneyParticle.gameObject.SetActive(true);
        }
    }

    public void SetCoins(int value)
    {
        totalCoins = value;
        UpdateCoinUI();
    }

    public int GetCoins()
    {
        return totalCoins;
    }

    private void UpdateCoinUI(bool immediate = false)
    {
        if (moneyText == null) return;

        moneyText.text = totalCoins.ToString("N0");

        // Animate punch scale
        if (!immediate)
            punchTween?.Kill();
        punchTween = moneyText.transform
            .DOPunchScale(Vector3.one * punchScale, punchDuration, vibrato: 5, elasticity: 0.8f)
            .SetEase(Ease.OutBack);
    }
}
