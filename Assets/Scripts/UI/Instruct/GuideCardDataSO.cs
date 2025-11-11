using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "GuideCardData", menuName = "Instruct/Guide Card Data", order = 0)]
public class GuideCardDataSO : ScriptableObject
{
    [Header("UI References")]
    public Sprite icon;                         // Icon for the guide card
    public string systemName;                   // Name of the system or feature
    [TextArea(2, 4)] public string description; // Short description for guidance
}
