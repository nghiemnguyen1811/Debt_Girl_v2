using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using System.Threading.Tasks;

public class DialogueManager : SingletonMonobehaviour<DialogueManager>
{
    //────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Core References")]
    private PlayerControl playerControl;

    [Header("UI References")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject dialogueBox;

    [Header("Speaker Name UI")]
    [SerializeField] private SpeakerNameUI[] speakerNameSlots = new SpeakerNameUI[2];

    [Header("Configuration")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private float animOffset = 800f;

    [Header("Input")]
    [SerializeField] private Button dialogueClickArea;

    #endregion
    //────────────────────────────────────────────────────
    #region === Runtime State ===

    private DialogueSequence currentSequence;
    private int currentIndex;

    private string npcNameRaw;
    private string currentLocalizedText;

    private bool isActive = false;
    private bool canProceed = false;
    private bool isTyping = false;

    private Coroutine typingCoroutine;

    private RectTransform leftRect;
    private RectTransform rightRect;
    private RectTransform dialogueRect;

    private Vector2 leftDefaultPos;
    private Vector2 rightDefaultPos;
    private Vector2 dialogueDefaultPos;

    #endregion
    //────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    private void Start()
    {
        playerControl = PlayerControl.Instance;
        CacheUIReferences();

        if (dialogueClickArea != null)
            dialogueClickArea.onClick.AddListener(NextLine);
    }

    #endregion
    //────────────────────────────────────────────────────
    #region === Public API ===

    /// <summary>
    /// Initiates a dialogue session with a specific NPC by finding the correct sequence for the player's character type.
    /// </summary>
    public void StartNpcDialogue(NPCDialogueData npcData)
    {
        CharacterInfoSO currentPlayer = playerControl.CharacterProfile;
        npcNameRaw = npcData.npcName;

        foreach (var entry in npcData.dialogues)
        {
            if (entry.characterType == currentPlayer.characterType)
            {
                StartDialogue(entry.dialogueSequence);
                return;
            }
        }

        Debug.LogWarning($"[DialogueManager] No dialogue found for {currentPlayer.characterType} with NPC {npcData.npcName}");
    }

    /// <summary>
    /// Advances the dialogue to the next line or instantly finishes the typing effect if it is currently active.
    /// </summary>
    public void NextLine()
    {
        if (!isActive || !canProceed) return;

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = currentLocalizedText;
            isTyping = false;
            return;
        }

        currentIndex++;

        if (currentIndex < currentSequence.lines.Length)
            ShowCurrentLine();
        else
            EndDialogue();
    }

    #endregion
    //────────────────────────────────────────────────────
    #region === Dialogue Flow Logic ===

    /// <summary>
    /// Sets up the internal state and triggers the intro animation for the given sequence.
    /// </summary>
    private void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null) return;
        PrepareDialogueState(sequence);
        PlayIntroAnimation();
    }

    /// <summary>
    /// Resets variables and clears the UI to prepare for a new dialogue session.
    /// </summary>
    private void PrepareDialogueState(DialogueSequence sequence)
    {
        UIManager.Instance.ToggleDialoguePanel(true);
        currentSequence = sequence;
        currentIndex = 0;
        isActive = true;
        canProceed = false;
        ClearDialogueUI();
    }

    /// <summary>
    /// Asynchronously loads the localized dialogue content and updates the UI elements (portraits, names, and text) for the current line.
    /// </summary>
    private async void ShowCurrentLine()
    {
        canProceed = false;

        DialogueLine line = currentSequence.lines[currentIndex];
        CharacterInfoSO playerData = playerControl.CharacterProfile;
        bool isPlayer = (line.speakerType == playerData.characterType);

        UpdatePortraits(line, isPlayer);

        if (speakerNameSlots != null)
        {
            foreach (var slot in speakerNameSlots)
                if (slot != null) slot.Hide();
        }

        int index = isPlayer ? 0 : 1;
        if (speakerNameSlots != null && index >= 0 && index < speakerNameSlots.Length && speakerNameSlots[index] != null)
        {
            string displayName = isPlayer ? playerData.characterName : npcNameRaw;
            speakerNameSlots[index].Show(displayName);
        }

        dialogueText.text = "";

        // Load text from Localization Manager
        string translatedContent = await LocalizationManager.Instance.GetLocalizedString("Dialogue Labels", line.dialogueKey);

        currentLocalizedText = translatedContent;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        canProceed = true;
        typingCoroutine = StartCoroutine(TypeText(currentLocalizedText));
    }

    /// <summary>
    /// Cleans up the dialogue state, stops interactions, and closes the UI panel.
    /// </summary>
    private void EndDialogue()
    {
        ResetDialogueState();
        playerControl.interactDetector.StopCurrentInteraction();
        UIManager.Instance.ToggleDialoguePanel(false);
    }

    /// <summary>
    /// Resets all runtime variables to their default values.
    /// </summary>
    private void ResetDialogueState()
    {
        isActive = false;
        canProceed = false;
        currentSequence = null;
        npcNameRaw = null;
        currentLocalizedText = "";

        if (speakerNameSlots != null)
        {
            foreach (var slot in speakerNameSlots)
                if (slot != null) slot.Hide();
        }
    }

    #endregion
    //────────────────────────────────────────────────────
    #region === UI & Animation Support ===

    /// <summary>
    /// Caches the RectTransform components and their initial positions for animation purposes.
    /// </summary>
    private void CacheUIReferences()
    {
        leftRect = leftPortrait.GetComponent<RectTransform>();
        rightRect = rightPortrait.GetComponent<RectTransform>();
        dialogueRect = dialogueBox.GetComponent<RectTransform>();

        leftDefaultPos = leftRect.anchoredPosition;
        rightDefaultPos = rightRect.anchoredPosition;
        dialogueDefaultPos = dialogueRect.anchoredPosition;
    }

    /// <summary>
    /// Plays the entrance animation for the portraits and the dialogue box using DOTween.
    /// </summary>
    private void PlayIntroAnimation()
    {
        leftRect.anchoredPosition = leftDefaultPos + Vector2.left * animOffset;
        rightRect.anchoredPosition = rightDefaultPos + Vector2.right * animOffset;
        dialogueRect.anchoredPosition = dialogueDefaultPos + Vector2.down * animOffset;

        Sequence introSeq = DOTween.Sequence();
        introSeq.Append(leftRect.DOAnchorPos(leftDefaultPos, animDuration).SetEase(Ease.OutBack));
        introSeq.Join(rightRect.DOAnchorPos(rightDefaultPos, animDuration).SetEase(Ease.OutBack));
        introSeq.Join(dialogueRect.DOAnchorPos(dialogueDefaultPos, animDuration).SetEase(Ease.OutBack));

        introSeq.OnComplete(() =>
        {
            ShowCurrentLine();
        });
    }

    /// <summary>
    /// Clears the text and hides speaker name slots to prevent visual glitches.
    /// </summary>
    private void ClearDialogueUI()
    {
        if (dialogueText != null) dialogueText.text = "";
        if (speakerNameSlots != null)
        {
            foreach (var slot in speakerNameSlots)
                if (slot != null) slot.Hide();
        }
    }

    /// <summary>
    /// Updates the portrait sprites and dims the inactive speaker's image.
    /// </summary>
    private void UpdatePortraits(DialogueLine line, bool isPlayer)
    {
        leftPortrait.sprite = line.portraits.playerPortrait;
        rightPortrait.sprite = line.portraits.npcPortrait;
        leftPortrait.gameObject.SetActive(true);
        rightPortrait.gameObject.SetActive(true);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        leftPortrait.color = isPlayer ? activeColor : inactiveColor;
        rightPortrait.color = isPlayer ? inactiveColor : activeColor;
    }

    #endregion
    //────────────────────────────────────────────────────
    #region === Typing Effect ===

    /// <summary>
    /// Coroutine that types out the text character by character over time.
    /// </summary>
    private IEnumerator TypeText(string line)
    {
        dialogueText.text = "";
        isTyping = true;
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    #endregion
}

[System.Serializable]
public class SpeakerNameUI
{
    public GameObject rootObject;
    public TMP_Text nameText;

    /// <summary>
    /// Activates the panel and sets the speaker name text.
    /// </summary>
    public void Show(string displayName)
    {
        if (rootObject != null) rootObject.SetActive(true);
        if (nameText != null) nameText.text = displayName;
    }

    /// <summary>
    /// Deactivates the panel to hide the speaker name.
    /// </summary>
    public void Hide()
    {
        if (rootObject != null) rootObject.SetActive(false);
    }
}