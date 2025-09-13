using UnityEngine;
using UnityEngine.UI;
using System;

public class CharacterTabButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image avatarIcon;
    [SerializeField] private Button button;
    [SerializeField] private GameObject outline; 

    [Header("Data")]
    [SerializeField] private CharacterType characterType;

    public static event Action<CharacterType> OnTabSelected;

    public CharacterType CharacterType => characterType;

    private void Start()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);

        if (outline != null)
            outline.SetActive(false);
    }

    public void Configure(Sprite avatar, CharacterType type)
    {
        avatarIcon.sprite = avatar;
        characterType = type;
    }

    private void HandleClick()
    {
        OnTabSelected?.Invoke(characterType);
    }

    public void SetSelected(bool selected)
    {
        if (outline != null)
            outline.SetActive(selected);
    }
}
