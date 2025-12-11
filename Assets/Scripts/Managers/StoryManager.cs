using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls story image transitions, typewriter text, next/skip logic,
/// and notifies MainMenu when the story ends.
/// </summary>
public class StoryManager : SingletonMonobehaviour<StoryManager>
{
    //────────────────────────────────────────────────────
    // == Story Data ==
    //────────────────────────────────────────────────────
    [Header("Story Data")]
    [SerializeField] private List<StoryDataSO> storyList = new();

    //────────────────────────────────────────────────────
    // == UI References ==
    //────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private Transform imageStackParent;
    [SerializeField] private Image storyImagePrefab;
    [SerializeField] private TextMeshProUGUI storyTextUI;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButtonEnable;
    [SerializeField] private Button skipButtonDisable;

    //────────────────────────────────────────────────────
    // == Settings ==
    //────────────────────────────────────────────────────
    [Header("Settings")]
    [SerializeField] private float typeSpeed = 0.03f;
    [SerializeField] private float slideDuration = 1.2f;
    [SerializeField] private float antiSpamDelay = 0.05f;

    //────────────────────────────────────────────────────
    // == Runtime State ==
    //────────────────────────────────────────────────────
    private readonly List<Image> stackedImages = new();
    private int currentIndex = 0;
    private bool isTyping = false;
    private bool canClickNext = true;

    //────────────────────────────────────────────────────
    // == Unity Events ==
    //────────────────────────────────────────────────────
    /// <summary>Initializes UI and shows the first story page.</summary>
    private void Start()
    {
        InitButtons();
        SpawnImages();
        ArrangeStackOrder();
        ShowCurrentStory();
    }

    //────────────────────────────────────────────────────
    // == Initialization Helpers ==
    //────────────────────────────────────────────────────
    /// <summary>Registers listeners for next/skip buttons.</summary>
    private void InitButtons()
    {
        nextButton.onClick.AddListener(OnClickNext);
        skipButtonEnable.onClick.AddListener(SkipStory);
    }

    /// <summary>Instantiates all story images from StoryData list.</summary>
    private void SpawnImages()
    {
        stackedImages.Clear();

        foreach (var data in storyList)
        {
            Image img = Instantiate(storyImagePrefab, imageStackParent);
            img.sprite = data.illustration;
            img.color = Color.white;
            stackedImages.Add(img);
        }
    }

    /// <summary>Sets sibling order so the first page appears on top.</summary>
    private void ArrangeStackOrder()
    {
        int count = stackedImages.Count;

        for (int i = 0; i < count; i++)
        {
            int topIndex = count - 1 - i;
            stackedImages[i].transform.SetSiblingIndex(topIndex);
        }

        UpdateActiveImages();
    }

    /// <summary>Activates images from currentIndex onward, hides previous ones.</summary>
    private void UpdateActiveImages()
    {
        for (int i = 0; i < stackedImages.Count; i++)
            stackedImages[i].gameObject.SetActive(i >= currentIndex);
    }

    //────────────────────────────────────────────────────
    // == Story Text & Typewriter ==
    //────────────────────────────────────────────────────
    /// <summary>Prepares and starts typewriter for the current story page.</summary>
    private void ShowCurrentStory()
    {
        StopAllCoroutines();

        // Ensure text is fully visible for new page
        Color c = storyTextUI.color;
        c.a = 1f;
        storyTextUI.color = c;

        storyTextUI.text = "";
        StartCoroutine(Typewriter(storyList[currentIndex].storyText));
    }

    /// <summary>Reveals story text character by character.</summary>
    private IEnumerator Typewriter(string text)
    {
        isTyping = true;

        foreach (char c in text)
        {
            storyTextUI.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    //────────────────────────────────────────────────────
    // == Next Button Logic ==
    //────────────────────────────────────────────────────
    /// <summary>Handles next button: skip typing or go to next page.</summary>
    private void OnClickNext()
    {
        if (!canClickNext)
            return;

        canClickNext = false;
        nextButton.interactable = false;

        // If text is still typing → reveal instantly
        if (isTyping)
        {
            StopAllCoroutines();
            storyTextUI.text = storyList[currentIndex].storyText;
            isTyping = false;

            StartCoroutine(EnableNextButtonDelayed());
            return;
        }

        bool isLastPage = currentIndex == stackedImages.Count - 1;

        if (isLastPage) EndStory();
        else SlideOutCurrentImage();
    }

    /// <summary>Prevents button spam by delaying re-enable.</summary>
    private IEnumerator EnableNextButtonDelayed()
    {
        yield return new WaitForSeconds(antiSpamDelay);
        nextButton.interactable = true;
        canClickNext = true;
    }

    //────────────────────────────────────────────────────
    // == Image Transition ==
    //────────────────────────────────────────────────────
    /// <summary>
    /// Slides current image to the right and fades out both image and text.
    /// New text starts only after the image has fully faded.
    /// </summary>
    private void SlideOutCurrentImage()
    {
        Image img = stackedImages[currentIndex];

        CanvasGroup cg = img.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = img.gameObject.AddComponent<CanvasGroup>();

        RectTransform rt = img.rectTransform;

        AudioManager.Instance?.PlayInteractSound(16);

        // Slide image + fade image + fade current text together
        Sequence seq = DOTween.Sequence()
            .Append(rt.DOAnchorPosX(800f, slideDuration))
            .Join(cg.DOFade(0f, slideDuration))
            .Join(storyTextUI.DOFade(0f, slideDuration));

        seq.OnComplete(() =>
        {
            img.gameObject.SetActive(false);
            currentIndex++;

            if (currentIndex < stackedImages.Count)
            {
                UpdateActiveImages();
                ShowCurrentStory();
            }

            else EndStory();

            StartCoroutine(EnableNextButtonDelayed());
        });
    }

    //────────────────────────────────────────────────────
    // == Skip & Reset ==
    //────────────────────────────────────────────────────
    /// <summary>Immediately ends the story sequence.</summary>
    private void SkipStory()
    {
        StopAllCoroutines();
        EndStory();
    }

    /// <summary>Resets story state so it can be played from the beginning.</summary>
    public void ResetStory()
    {
        StopAllCoroutines();

        foreach (var img in stackedImages)
            Destroy(img.gameObject);

        stackedImages.Clear();

        currentIndex = 0;
        isTyping = false;

        // Restore text alpha
        Color c = storyTextUI.color;
        c.a = 1f;
        storyTextUI.color = c;

        SpawnImages();
        ArrangeStackOrder();
        ShowCurrentStory();

        canClickNext = true;
        nextButton.interactable = true;
    }

    //────────────────────────────────────────────────────
    // == End of Story ==
    //────────────────────────────────────────────────────
    /// <summary>Notifies MainMenu that the story has finished.</summary>
    private void EndStory()
    {
        MainMenu.Instance.StartStoryEndSequence();
    }

    //────────────────────────────────────────────────────
    // == External Controls ==
    //────────────────────────────────────────────────────
    /// <summary>Shows or hides the active skip button variant.</summary>
    public void SetSkipInteractable(bool state)
    {
        skipButtonEnable.gameObject.SetActive(state);
        skipButtonDisable.gameObject.SetActive(!state);
    }
}
