using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class DialogueManager : SingletonMonobehaviour<DialogueManager>
{
    //────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("References")]
    private PlayerControl playerControl;

    [Header("UI References")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject dialogueBox;

    [Header("Speaker Name UI (0 = Player, 1 = NPC)")]
    [SerializeField] private SpeakerNameUI[] speakerNameSlots = new SpeakerNameUI[2];

    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private float animOffset = 800f;

    [Header("UI Input")]
    [SerializeField] private Button dialogueClickArea;

    #endregion
    //────────────────────────────────────────────────────
    #region === Runtime State ===

    private DialogueSequence currentSequence;
    private int currentIndex;
    private string npcName;

    private bool isActive = false;
    private bool canProceed = false;
    private bool isTyping = false;

    private Coroutine typingCoroutine;

    // Cached RectTransforms & positions
    private RectTransform leftRect;
    private RectTransform rightRect;
    private RectTransform dialogueRect;

    private Vector2 leftDefaultPos;
    private Vector2 rightDefaultPos;
    private Vector2 dialogueDefaultPos;

    #endregion
    //────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    /// <summary>
    /// Initialize references and register button events.
    /// </summary>
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
    /// Start dialogue for a given NPC based on current player character type.
    /// </summary>
    public void StartNpcDialogue(NPCDialogueData npcData)
    {
        CharacterInfoSO currentPlayer = playerControl.CharacterProfile;
        npcName = npcData.npcName;

        foreach (var entry in npcData.dialogues)
        {
            if (entry.characterType == currentPlayer.characterType)
            {
                StartDialogue(entry.dialogueSequence);
                return;
            }
        }

        Debug.LogWarning($"No dialogue found for {currentPlayer.characterType} with NPC {npcData.npcName}");
    }

    /// <summary>
    /// Advance to the next line or instantly finish typing the current line.
    /// </summary>
    public void NextLine()
    {
        if (!isActive || !canProceed)
            return;

        // If still typing, finish the line immediately
        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            DialogueLine line = currentSequence.lines[currentIndex];
            dialogueText.text = line.dialogueText;
            isTyping = false;
            return;
        }

        // Move to next line
        currentIndex++;

        if (currentIndex < currentSequence.lines.Length)
            ShowCurrentLine();

        else EndDialogue();
    }

    #endregion
    //────────────────────────────────────────────────────
    #region === Dialogue Flow ===

    /// <summary>
    /// Start a dialogue sequence and play the intro animation.
    /// </summary>
    private void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null)
            return;

        PrepareDialogueState(sequence);
        PlayIntroAnimation();
    }

    /// <summary>
    /// Prepare internal state and UI for a new dialogue sequence.
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
    /// Display the current dialogue line and update speaker UI.
    /// </summary>
    private void ShowCurrentLine()
    {
        DialogueLine line = currentSequence.lines[currentIndex];
        CharacterInfoSO playerData = playerControl.CharacterProfile;
        bool isPlayer = (line.speakerType == playerData.characterType);

        // Update portraits and highlight the active speaker
        UpdatePortraits(line, isPlayer);

        // Hide all speaker name panels
        if (speakerNameSlots != null)
        {
            for (int i = 0; i < speakerNameSlots.Length; i++)
            {
                if (speakerNameSlots[i] != null)
                    speakerNameSlots[i].Hide();
            }
        }

        // 0 = Player, 1 = NPC
        int index = isPlayer ? 0 : 1;
        if (speakerNameSlots != null &&
            index >= 0 && index < speakerNameSlots.Length &&
            speakerNameSlots[index] != null)
        {
            string displayName = isPlayer ? playerData.characterName : npcName;
            speakerNameSlots[index].Show(displayName);
        }

        // Start typing effect for the dialogue text
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.dialogueText));
    }

    /// <summary>
    /// Finish the dialogue and restore normal gameplay.
    /// </summary>
    private void EndDialogue()
    {
        ResetDialogueState();
        playerControl.interactDetector.StopCurrentInteraction();
        UIManager.Instance.ToggleDialoguePanel(false);
    }

    /// <summary>
    /// Reset dialogue state and hide all speaker name panels.
    /// </summary>
    private void ResetDialogueState()
    {
        isActive = false;
        canProceed = false;
        currentSequence = null;
        npcName = null;

        if (speakerNameSlots != null)
        {
            for (int i = 0; i < speakerNameSlots.Length; i++)
            {
                if (speakerNameSlots[i] != null)
                    speakerNameSlots[i].Hide();
            }
        }
    }

    #endregion
    //────────────────────────────────────────────────────
    #region === UI & Animation ===

    /// <summary>
    /// Cache RectTransform references and their default anchored positions.
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
    /// Play entry animation for portraits and dialogue box.
    /// </summary>
    private void PlayIntroAnimation()
    {
        // Start from off-screen positions
        leftRect.anchoredPosition = leftDefaultPos + Vector2.left * animOffset;
        rightRect.anchoredPosition = rightDefaultPos + Vector2.right * animOffset;
        dialogueRect.anchoredPosition = dialogueDefaultPos + Vector2.down * animOffset;

        // Tween them into place
        Sequence introSeq = DOTween.Sequence();
        introSeq.Append(leftRect.DOAnchorPos(leftDefaultPos, animDuration).SetEase(Ease.OutBack));
        introSeq.Join(rightRect.DOAnchorPos(rightDefaultPos, animDuration).SetEase(Ease.OutBack));
        introSeq.Join(dialogueRect.DOAnchorPos(dialogueDefaultPos, animDuration).SetEase(Ease.OutBack));

        introSeq.OnComplete(() =>
        {
            canProceed = true;
            ShowCurrentLine();
        });
    }

    /// <summary>
    /// Clear dialogue text and hide all speaker name panels.
    /// </summary>
    private void ClearDialogueUI()
    {
        if (dialogueText != null)
        {
            dialogueText.DOFade(0, 0f);
            dialogueText.text = "";
            dialogueText.DOFade(1, 0.01f);
        }

        if (speakerNameSlots != null)
        {
            for (int i = 0; i < speakerNameSlots.Length; i++)
            {
                if (speakerNameSlots[i] != null)
                    speakerNameSlots[i].Hide();
            }
        }
    }

    /// <summary>
    /// Update portraits and dim the one who is not speaking.
    /// </summary>
    private void UpdatePortraits(DialogueLine line, bool isPlayer)
    {
        // Set sprites
        leftPortrait.sprite = line.portraits.playerPortrait;
        rightPortrait.sprite = line.portraits.npcPortrait;

        leftPortrait.gameObject.SetActive(true);
        rightPortrait.gameObject.SetActive(true);

        // Active speaker full alpha, the other is dimmed
        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        leftPortrait.color = isPlayer ? activeColor : inactiveColor;
        rightPortrait.color = isPlayer ? inactiveColor : activeColor;
    }

    #endregion
    //────────────────────────────────────────────────────
    #region === Typing Effect ===

    /// <summary>
    /// Coroutine that types the dialogue text character by character.
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
    [Header("Root Object (Panel containing name)")]
    public GameObject rootObject;      // UI panel that contains the speaker name

    [Header("Name Text")]
    public TMP_Text nameText;          // TextMeshProUGUI that displays the name

    /// <summary>
    /// Show this name panel and set the display text.
    /// </summary>
    public void Show(string displayName)
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (nameText != null)
            nameText.text = displayName;
    }

    /// <summary>
    /// Hide this name panel.
    /// </summary>
    public void Hide()
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }
}
