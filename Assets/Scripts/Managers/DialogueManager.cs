using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class DialogueManager : SingletonMonobehaviour<DialogueManager>
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("References")]
    private PlayerControl playerControl;

    [Header("UI References")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject dialogueBox;

    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private float animOffset = 800f;

    [Header("UI Input")]
    [SerializeField] private Button dialogueClickArea;

    // ─────────────────────────────────────────────────────
    // Runtime Data
    // ─────────────────────────────────────────────────────
    private DialogueSequence currentSequence;
    private int currentIndex;
    private string npcName;

    private bool isActive = false;
    private bool canProceed = false;
    private bool isTyping = false;

    private Coroutine typingCoroutine;

    // Cached RectTransforms & Positions
    private RectTransform leftRect, rightRect, dialogueRect;
    private Vector2 leftDefaultPos, rightDefaultPos, dialogueDefaultPos;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        playerControl = PlayerControl.Instance;
        CacheUIReferences();

        if (dialogueClickArea != null)
            dialogueClickArea.onClick.AddListener(NextLine);
    }

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Start dialogue with an NPC, matching dialogue entries with current player type.
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
    /// Move to the next line or skip typing if still in progress.
    /// </summary>
    public void NextLine()
    {
        if (!isActive || !canProceed) return;

        // Skip typing if still in progress
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            DialogueLine line = currentSequence.lines[currentIndex];
            dialogueText.text = line.dialogueText;
            isTyping = false;
            return;
        }

        // Otherwise move to the next line
        currentIndex++;

        if (currentIndex < currentSequence.lines.Length)
            ShowCurrentLine();

        else EndDialogue();
    }

    // ─────────────────────────────────────────────────────
    // Dialogue Flow
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Start a dialogue sequence and play intro animation.
    /// </summary>
    private void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null) return;

        PrepareDialogueState(sequence);
        PlayIntroAnimation();
    }

    /// <summary>
    /// Prepare state before dialogue starts.
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
    /// Display the current dialogue line.
    /// </summary>
    private void ShowCurrentLine()
    {
        DialogueLine line = currentSequence.lines[currentIndex];
        CharacterInfoSO playerData = playerControl.CharacterProfile;
        bool isPlayer = (line.speakerType == playerData.characterType);

        UpdatePortraits(line, isPlayer);
        nameText.text = isPlayer ? playerData.characterName : npcName;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.dialogueText));
    }

    /// <summary>
    /// End dialogue sequence.
    /// </summary>
    private void EndDialogue()
    {
        ResetDialogueState();
        playerControl.interactDetector.StopCurrentInteraction();
        UIManager.Instance.ToggleDialoguePanel(false);
    }

    /// <summary>
    /// Reset dialogue state.
    /// </summary>
    private void ResetDialogueState()
    {
        isActive = false;
        canProceed = false;
        currentSequence = null;
        npcName = null;
    }

    // ─────────────────────────────────────────────────────
    // UI & Animation
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Cache RectTransforms and default positions.
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
    /// Play intro animation for portraits and dialogue panel.
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
            canProceed = true;
            ShowCurrentLine();
        });
    }

    /// <summary>
    /// Clear name and dialogue text instantly.
    /// </summary>
    private void ClearDialogueUI()
    {
        nameText.DOFade(0, 0f);
        dialogueText.DOFade(0, 0f);
        nameText.text = "";
        dialogueText.text = "";
        nameText.DOFade(1, 0.01f);
        dialogueText.DOFade(1, 0.01f);
    }

    /// <summary>
    /// Update speaker and listener portraits.
    /// </summary>
    private void UpdatePortraits(DialogueLine line, bool isPlayer)
    {
        leftPortrait.sprite = line.portraits.playerPortrait;
        rightPortrait.sprite = line.portraits.npcPortrait;

        leftPortrait.gameObject.SetActive(true);
        rightPortrait.gameObject.SetActive(true);

        leftPortrait.color = isPlayer ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        rightPortrait.color = isPlayer ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
    }

    // ─────────────────────────────────────────────────────
    // Typing Effect
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Coroutine to display text with typing effect.
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
}
