using TMPro;
using UnityEngine;

public class MoneyParticle : MonoBehaviour
{
    [Header(" UI ")]
    [SerializeField] private TextMeshPro moneyText;

    public void Configure(int carrotMultiplier)
    {
        moneyText.text = "+" + carrotMultiplier;
    }
}
