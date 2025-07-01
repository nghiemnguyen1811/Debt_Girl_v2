using TMPro;
using UnityEngine;

public class MoneyParticle : MonoBehaviour
{
    [Header(" UI ")]
    [SerializeField] private TextMeshPro moneyText;

    public void Configure(double carrotMultiplier)
    {
        moneyText.text = (carrotMultiplier >= 0 ? "+" : "-") + DoubleUtilities.ToIdleNotation(Mathf.Abs((float)carrotMultiplier));
        moneyText.color = carrotMultiplier >= 0 ? Color.white : Color.red;
    }
}