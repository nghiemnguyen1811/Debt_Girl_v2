using UnityEngine;

[DisallowMultipleComponent]
public class InteractionProp : MonoBehaviour
{
    #region === Inspector Fields ===

    [Tooltip("Defines the type of this interaction prop (e.g., Broom, Pan, etc).")]
    [SerializeField] private InteractionPropType propType = InteractionPropType.None;

    #endregion

    #region === Public API ===

    /// <summary>
    /// Gets the interaction type of this prop.
    /// </summary>
    public InteractionPropType GetPropType()
    {
        return propType;
    }

    #endregion
}
