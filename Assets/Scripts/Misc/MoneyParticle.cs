using TMPro;
using UnityEngine;

public class MoneyParticle : MonoBehaviour
{
    [Header(" UI ")]
    [SerializeField] private TextMeshPro moneyText;

    public void Configure(double carrotMultiplier)
    {
        moneyText.text = "+" + DoubleUtilities.ToIdleNotation(carrotMultiplier);
    }
}
