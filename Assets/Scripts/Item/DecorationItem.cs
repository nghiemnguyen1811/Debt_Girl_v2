using UnityEngine;

public class DecorationItem : MonoBehaviour
{
    [Header("Decoration Info")]
    [SerializeField] private int itemID;
    [SerializeField] private CharacterType owner;

    public int ItemID => itemID;
    public CharacterType Owner => owner;

    private void Start()
    {
        // auto register into manager
        if (DecorationManager.Instance != null)
            DecorationManager.Instance.RegisterDecoration(this);
    }

    private void OnDestroy()
    {
        if (DecorationManager.Instance != null)
            DecorationManager.Instance.UnregisterDecoration(this);
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
