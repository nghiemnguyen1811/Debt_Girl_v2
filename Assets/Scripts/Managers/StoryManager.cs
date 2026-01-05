using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls story image transitions, typewriter text, next/skip logic,
/// and notifies MainMenu when the story ends.
/// Integration: Uses LocalizationManager to fetch localized text dynamically.
/// </summary>
public class StoryManager : SingletonMonobehaviour<StoryManager>
{
    //────────────────────────────────────────────────────
    // == Story Data ==
    //────────────────────────────────────────────────────
    [Header("Story Data")]
    [SerializeField] private List<StoryDataSO> storyList = new();

    [Tooltip("The name of the Localization Table that contains the story text keys.")]
    [SerializeField] private string localizationTableName = "Story Labels";

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

    // Cache the localized string to handle "Instant Finish" typing or Language changes
    private string currentLocalizedString = "";

    //────────────────────────────────────────────────────
    // == Unity Lifecycle ==
    //────────────────────────────────────────────────────

    private void Start()
    {
        InitButtons();
        SpawnImages();
        ArrangeStackOrder();

        // Start showing the story immediately
        ShowCurrentStory();

        // Register for language change events to update text dynamically
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.RegisterForGlobalRefresh(OnLanguageChanged);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Unregister to prevent memory leaks or errors when the object is destroyed
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.UnregisterForGlobalRefresh(OnLanguageChanged);
        }
    }

    //────────────────────────────────────────────────────
    // == Initialization Helpers ==
    //────────────────────────────────────────────────────

    private void InitButtons()
    {
        nextButton.onClick.AddListener(OnClickNext);
        skipButtonEnable.onClick.AddListener(SkipStory);
    }

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

    private void ArrangeStackOrder()
    {
        int count = stackedImages.Count;
        // Stack images so the first one (index 0) is rendered on top (last sibling)
        for (int i = 0; i < count; i++)
        {
            int topIndex = count - 1 - i;
            stackedImages[i].transform.SetSiblingIndex(topIndex);
        }

        UpdateActiveImages();
    }

    private void UpdateActiveImages()
    {
        for (int i = 0; i < stackedImages.Count; i++)
            stackedImages[i].gameObject.SetActive(i >= currentIndex);
    }

    //────────────────────────────────────────────────────
    // == Story Text & Typewriter ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the localized text for the current page and starts the typewriter effect.
    /// </summary>
    private async void ShowCurrentStory()
    {
        StopAllCoroutines();

        // Reset text alpha and content
        Color c = storyTextUI.color;
        c.a = 1f;
        storyTextUI.color = c;
        storyTextUI.text = "";

        if (currentIndex < storyList.Count)
        {
            string key = storyList[currentIndex].storyTextKey;

            // Fetch text from LocalizationManager asynchronously
            if (LocalizationManager.Instance != null)
            {
                currentLocalizedString = await LocalizationManager.Instance.GetLocalizedString(localizationTableName, key);
            }
            else
            {
                currentLocalizedString = "Error: LocalizationManager not found.";
            }

            // Start typing effect with the fetched text
            StartCoroutine(Typewriter(currentLocalizedString));
        }
    }

    private IEnumerator Typewriter(string text)
    {
        isTyping = true;
        storyTextUI.text = "";

        foreach (char c in text)
        {
            storyTextUI.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    //────────────────────────────────────────────────────
    // == Localization Handler ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Triggered automatically when the user changes language settings.
    /// Updates the currently displayed text immediately.
    /// </summary>
    private async void OnLanguageChanged()
    {
        // If story is finished or index is invalid, do nothing
        if (currentIndex >= storyList.Count) return;

        // 1. Stop the typewriter if it's running
        StopAllCoroutines();
        isTyping = false;

        // 2. Get the key for the current page
        string key = storyList[currentIndex].storyTextKey;

        // 3. Fetch the new localized string
        if (LocalizationManager.Instance != null)
        {
            currentLocalizedString = await LocalizationManager.Instance.GetLocalizedString(localizationTableName, key);
        }

        // 4. Update the text UI immediately (don't type it out again to avoid annoyance)
        if (storyTextUI != null)
        {
            storyTextUI.text = currentLocalizedString;

            // Ensure alpha is visible (in case it was fading out)
            Color c = storyTextUI.color;
            c.a = 1f;
            storyTextUI.color = c;
        }
    }

    //────────────────────────────────────────────────────
    // == Next Button Logic ==
    //────────────────────────────────────────────────────

    private void OnClickNext()
    {
        if (!canClickNext)
            return;

        canClickNext = false;
        nextButton.interactable = false;

        // If text is still typing, finish it instantly
        if (isTyping)
        {
            StopAllCoroutines();
            storyTextUI.text = currentLocalizedString; // Show full cached text
            isTyping = false;

            StartCoroutine(EnableNextButtonDelayed());
            return;
        }

        // Otherwise, move to the next page
        bool isLastPage = currentIndex == stackedImages.Count - 1;

        if (isLastPage) EndStory();
        else SlideOutCurrentImage();
    }

    private IEnumerator EnableNextButtonDelayed()
    {
        yield return new WaitForSeconds(antiSpamDelay);
        nextButton.interactable = true;
        canClickNext = true;
    }

    //────────────────────────────────────────────────────
    // == Image Transition ==
    //────────────────────────────────────────────────────

    private void SlideOutCurrentImage()
    {
        Image img = stackedImages[currentIndex];

        // Ensure CanvasGroup exists for fading
        CanvasGroup cg = img.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = img.gameObject.AddComponent<CanvasGroup>();

        RectTransform rt = img.rectTransform;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayInteractSound(16);

        // Animate: Slide Right + Fade Out Image + Fade Out Text
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
                ShowCurrentStory(); // Load next text and start typing
            }
            else
            {
                EndStory();
            }

            StartCoroutine(EnableNextButtonDelayed());
        });
    }

    //────────────────────────────────────────────────────
    // == Skip & Reset ==
    //────────────────────────────────────────────────────

    private void SkipStory()
    {
        StopAllCoroutines();
        EndStory();
    }

    /// <summary>
    /// Resets the story to the beginning. Useful for replaying.
    /// </summary>
    public void ResetStory()
    {
        StopAllCoroutines();

        // Clean up existing image instances
        foreach (var img in stackedImages)
        {
            if (img != null) Destroy(img.gameObject);
        }

        stackedImages.Clear();

        currentIndex = 0;
        isTyping = false;
        currentLocalizedString = "";

        // Reset Text Alpha
        Color c = storyTextUI.color;
        c.a = 1f;
        storyTextUI.color = c;

        // Re-spawn and start
        SpawnImages();
        ArrangeStackOrder();
        ShowCurrentStory();

        canClickNext = true;
        nextButton.interactable = true;
    }

    //────────────────────────────────────────────────────
    // == End of Story ==
    //────────────────────────────────────────────────────

    private void EndStory()
    {
        if (MainMenu.Instance != null)
            MainMenu.Instance.StartStoryEndSequence();
    }

    //────────────────────────────────────────────────────
    // == External Controls ==
    //────────────────────────────────────────────────────

    public void SetSkipInteractable(bool state)
    {
        skipButtonEnable.gameObject.SetActive(state);
        skipButtonDisable.gameObject.SetActive(!state);
    }
}