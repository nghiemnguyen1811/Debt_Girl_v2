using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization.Settings;

/// <summary>
/// Controls story image transitions, typewriter text, next/skip logic,
/// and notifies MainMenu when the story ends.
/// Integration: Fetches localized story text asynchronously (PAD-safe).
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

    // Cache current localized string (used for instant-finish typing)
    private string currentLocalizedString = "";

    // Guards against async race & scene/object lifecycle issues
    private int _requestVersion = 0;
    private bool _isDestroyed = false;

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
            LocalizationManager.Instance.RegisterForGlobalRefresh(OnLanguageChanged);
    }

    protected override void OnDestroy()
    {
        _isDestroyed = true;

        // Unregister to prevent leaks/errors
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.UnregisterForGlobalRefresh(OnLanguageChanged);

        base.OnDestroy();
    }

    //────────────────────────────────────────────────────
    // == Initialization Helpers ==
    //────────────────────────────────────────────────────

    private void InitButtons()
    {
        if (nextButton) nextButton.onClick.AddListener(OnClickNext);
        if (skipButtonEnable) skipButtonEnable.onClick.AddListener(SkipStory);
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
    // == Localization Fetch (PAD-safe) ==
    //────────────────────────────────────────────────────

    private async Task<string> FetchStoryTextAsync(string table, string key)
    {
        try
        {
            // Ensure localization system is initialized (async)
            await LocalizationSettings.InitializationOperation.Task;

            // Fetch directly from StringDatabase (avoid handle.Result usage)
            string result = await LocalizationSettings.StringDatabase
                .GetLocalizedStringAsync(table, key).Task;

            return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StoryManager] Failed to fetch localized story text. Table='{table}', Key='{key}'. {ex.Message}");
            return string.Empty;
        }
    }

    //────────────────────────────────────────────────────
    // == Story Text & Typewriter ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Fetches localized text for the current page and starts the typewriter effect.
    /// Protected against async race conditions and destroyed objects.
    /// </summary>
    private async void ShowCurrentStory()
    {
        StopAllCoroutines();

        // Reset text alpha and content
        if (storyTextUI)
        {
            Color c = storyTextUI.color;
            c.a = 1f;
            storyTextUI.color = c;
            storyTextUI.text = "";
        }

        if (currentIndex < 0 || currentIndex >= storyList.Count)
            return;

        int version = ++_requestVersion;

        string key = storyList[currentIndex].storyTextKey;

        // Fetch localized string
        currentLocalizedString = await FetchStoryTextAsync(localizationTableName, key);

        // Abort if state changed while awaiting
        if (_isDestroyed) return;
        if (version != _requestVersion) return;
        if (currentIndex < 0 || currentIndex >= storyList.Count) return;
        if (!storyTextUI) return;

        // Extra: settle TMP for first-frame weirdness
        storyTextUI.text = "";
        storyTextUI.ForceMeshUpdate(true, true);

        StartCoroutine(Typewriter(currentLocalizedString));
    }

    private IEnumerator Typewriter(string text)
    {
        isTyping = true;
        if (storyTextUI) storyTextUI.text = "";

        foreach (char c in text)
        {
            if (!storyTextUI) break;
            storyTextUI.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    //────────────────────────────────────────────────────
    // == Localization Handler ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Triggered when the language changes.
    /// Updates the currently displayed text immediately.
    /// </summary>
    private async void OnLanguageChanged()
    {
        if (_isDestroyed) return;
        if (currentIndex < 0 || currentIndex >= storyList.Count) return;

        StopAllCoroutines();
        isTyping = false;

        int version = ++_requestVersion;

        string key = storyList[currentIndex].storyTextKey;

        currentLocalizedString = await FetchStoryTextAsync(localizationTableName, key);

        if (_isDestroyed) return;
        if (version != _requestVersion) return;
        if (!storyTextUI) return;

        storyTextUI.text = currentLocalizedString;

        Color c = storyTextUI.color;
        c.a = 1f;
        storyTextUI.color = c;

        storyTextUI.ForceMeshUpdate(true, true);
    }

    //────────────────────────────────────────────────────
    // == Next Button Logic ==
    //────────────────────────────────────────────────────

    private void OnClickNext()
    {
        if (!canClickNext)
            return;

        canClickNext = false;
        if (nextButton) nextButton.interactable = false;

        // If text is still typing, finish it instantly
        if (isTyping)
        {
            StopAllCoroutines();
            if (storyTextUI) storyTextUI.text = currentLocalizedString;
            isTyping = false;

            StartCoroutine(EnableNextButtonDelayed());
            return;
        }

        bool isLastPage = currentIndex == stackedImages.Count - 1;

        if (isLastPage) EndStory();
        else SlideOutCurrentImage();
    }

    private IEnumerator EnableNextButtonDelayed()
    {
        yield return new WaitForSeconds(antiSpamDelay);

        if (nextButton) nextButton.interactable = true;
        canClickNext = true;
    }

    //────────────────────────────────────────────────────
    // == Image Transition ==
    //────────────────────────────────────────────────────

    private void SlideOutCurrentImage()
    {
        if (currentIndex < 0 || currentIndex >= stackedImages.Count)
        {
            EndStory();
            return;
        }

        Image img = stackedImages[currentIndex];

        CanvasGroup cg = img.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = img.gameObject.AddComponent<CanvasGroup>();

        RectTransform rt = img.rectTransform;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayInteractSound(16);

        Sequence seq = DOTween.Sequence()
            .Append(rt.DOAnchorPosX(800f, slideDuration))
            .Join(cg.DOFade(0f, slideDuration))
            .Join(storyTextUI ? storyTextUI.DOFade(0f, slideDuration) : null);

        seq.OnComplete(() =>
        {
            if (img) img.gameObject.SetActive(false);
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

        foreach (var img in stackedImages)
        {
            if (img != null) Destroy(img.gameObject);
        }

        stackedImages.Clear();

        currentIndex = 0;
        isTyping = false;
        currentLocalizedString = "";

        if (storyTextUI)
        {
            Color c = storyTextUI.color;
            c.a = 1f;
            storyTextUI.color = c;
            storyTextUI.text = "";
            storyTextUI.ForceMeshUpdate(true, true);
        }

        SpawnImages();
        ArrangeStackOrder();
        ShowCurrentStory();

        canClickNext = true;
        if (nextButton) nextButton.interactable = true;
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
        if (skipButtonEnable) skipButtonEnable.gameObject.SetActive(state);
        if (skipButtonDisable) skipButtonDisable.gameObject.SetActive(!state);
    }
}
