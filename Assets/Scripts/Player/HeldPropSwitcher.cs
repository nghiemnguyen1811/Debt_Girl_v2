using UnityEngine;

/// <summary>
/// Sets props active/inactive by type.
/// When enabling, disables all others.
/// When disabling, only turns off the matching one.
/// </summary>
public class HeldPropSwitcher : MonoBehaviour
{
    [Header("List of Interaction Props")]
    [SerializeField] private InteractionProp[] interactionProps;

    private void Start()
    {
        DeactivateAllProps();
    }

    /// <summary>
    /// Enables or disables a prop by its type.
    /// </summary>
    /// <param name="propType">Prop type to target</param>
    /// <param name="isActive">True = show, False = hide</param>
    public void SetPropActiveByType(InteractionPropType propType, bool isActive)
    {
        bool found = false;

        foreach (var prop in interactionProps)
        {
            if (prop == null)
                continue;

            bool isMatch = prop.GetPropType() == propType;

            if (!isMatch) continue;

            prop.gameObject.SetActive(isActive);
            found = true;
        }

        if (!found) Debug.LogWarning($"[HeldPropSwitcher] No prop found for type: {propType}");
    }

    /// <summary>
    /// Deactivates all props.
    /// </summary>
    public void DeactivateAllProps()
    {
        foreach (var prop in interactionProps)
        {
            if (prop != null)
                prop.gameObject.SetActive(false);
        }
    }
}
