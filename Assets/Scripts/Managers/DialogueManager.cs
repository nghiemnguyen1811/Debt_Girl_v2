using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image leftPortrait;   // Player
    [SerializeField] private Image rightPortrait;  // NPC
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    private DialogueSequence currentSequence;
    private int currentIndex;
    private bool isActive = false;
    private Coroutine typingCoroutine;
    private Sprite overrideNpcPortrait;

    void Update()
    {
        if (isActive && Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
    }

    public void StartDialogue(DialogueSequence sequence, Sprite npcPortraitOverride = null)
    {
        if (sequence == null) return;

        dialoguePanel.SetActive(true);
        currentSequence = sequence;
        currentIndex = 0;
        isActive = true;

        overrideNpcPortrait = npcPortraitOverride;
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = currentSequence.lines[currentIndex];

        CharacterProfileSO playerData = PlayerControl.Instance.CharacterProfile;
        bool isPlayer = (line.speaker == playerData);

        if (isPlayer)
        {
            leftPortrait.sprite = line.speaker.portrait;
            leftPortrait.gameObject.SetActive(true);
            rightPortrait.gameObject.SetActive(false);
        }

        else
        {
            rightPortrait.sprite = overrideNpcPortrait != null ? overrideNpcPortrait : line.speaker.portrait;
            rightPortrait.gameObject.SetActive(true);
            leftPortrait.gameObject.SetActive(false);
        }

        nameText.text = line.speaker.characterName;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    private IEnumerator TypeText(string line)
    {
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void NextLine()
    {
        currentIndex++;
        if (currentIndex < currentSequence.lines.Length)
            ShowCurrentLine();

        else EndDialogue();
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isActive = false;
        currentSequence = null;
        overrideNpcPortrait = null;
    }
}
