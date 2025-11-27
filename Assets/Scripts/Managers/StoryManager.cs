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
    private void Start()
    {
        InitButtons();
        SpawnImages();
        ArrangeStackOrder();
        ShowCurrentStory();
    }

    //────────────────────────────────────────────────────
    // == Initialization ==
    //────────────────────────────────────────────────────
    /// <summary>Assigns button listeners.</summary>
    private void InitButtons()
    {
        nextButton.onClick.AddListener(OnClickNext);
        skipButtonEnable.onClick.AddListener(SkipStory);
    }

    /// <summary>Creates all images for story pages.</summary>
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

    /// <summary>Reorders images so page 0 appears on top.</summary>
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

    /// <summary>Enables only images from currentIndex onward.</summary>
    private void UpdateActiveImages()
    {
        for (int i = 0; i < stackedImages.Count; i++)
            stackedImages[i].gameObject.SetActive(i >= currentIndex);
    }

    //────────────────────────────────────────────────────
    // == Story Text ==
    //────────────────────────────────────────────────────
    /// <summary>Displays story text with typewriter effect.</summary>
    private void ShowCurrentStory()
    {
        StopAllCoroutines();
        storyTextUI.text = "";
        StartCoroutine(Typewriter(storyList[currentIndex].storyText));
    }

    /// <summary>Reveals text character-by-character.</summary>
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
    /// <summary>Main click handler: skip typing or move to next page.</summary>
    private void OnClickNext()
    {
        if (!canClickNext)
            return;

        // Hard prevent spam
        canClickNext = false;
        nextButton.interactable = false;

        // If still typing → skip to full text
        if (isTyping)
        {
            StopAllCoroutines();
            storyTextUI.text = storyList[currentIndex].storyText;
            isTyping = false;

            StartCoroutine(EnableNextButtonDelayed());
            return;
        }

        bool isLastPage = currentIndex == stackedImages.Count - 1;

        if (isLastPage)
        {
            EndStory();
        }
        else
        {
            SlideOutCurrentImage();
        }
    }

    /// <summary>Re-enables next button with small delay (anti-spam).</summary>
    private IEnumerator EnableNextButtonDelayed()
    {
        yield return new WaitForSeconds(antiSpamDelay);
        nextButton.interactable = true;
        canClickNext = true;
    }

    //────────────────────────────────────────────────────
    // == Image Transition ==
    //────────────────────────────────────────────────────
    /// <summary>Slide current page to the right and fade out.</summary>
    private void SlideOutCurrentImage()
    {
        Image img = stackedImages[currentIndex];

        CanvasGroup cg = img.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = img.gameObject.AddComponent<CanvasGroup>();

        RectTransform rt = img.rectTransform;

        AudioManager.Instance?.PlayInteractSound(16);

        DOTween.Sequence()
            .Append(rt.DOAnchorPosX(800f, slideDuration))
            .Join(cg.DOFade(0f, slideDuration))
            .OnComplete(() =>
            {
                img.gameObject.SetActive(false);
                currentIndex++;

                if (currentIndex < stackedImages.Count)
                {
                    UpdateActiveImages();
                    ShowCurrentStory();
                }
                else
                {
                    EndStory();
                }

                StartCoroutine(EnableNextButtonDelayed());
            });
    }

    //────────────────────────────────────────────────────
    // == Skip Logic ==
    //────────────────────────────────────────────────────
    /// <summary>Skips story instantly.</summary>
    public void SkipStory()
    {
        StopAllCoroutines();
        EndStory();
    }

    //────────────────────────────────────────────────────
    // == Reset Logic ==
    //────────────────────────────────────────────────────
    /// <summary>Allows story to replay from beginning.</summary>
    public void ResetStory()
    {
        StopAllCoroutines();

        foreach (var img in stackedImages)
            Destroy(img.gameObject);

        stackedImages.Clear();

        currentIndex = 0;
        isTyping = false;

        SpawnImages();
        ArrangeStackOrder();
        ShowCurrentStory();

        canClickNext = true;
        nextButton.interactable = true;
    }

    //────────────────────────────────────────────────────
    // == End of Story ==
    //────────────────────────────────────────────────────
    /// <summary>Notify MainMenu when story ends.</summary>
    private void EndStory()
    {
        MainMenu.Instance.StartStoryEndSequence();
    }

    //────────────────────────────────────────────────────
    // == External Controls ==
    //────────────────────────────────────────────────────
    /// <summary>Enable/disable skip button (controlled by MainMenu).</summary>
    public void SetSkipInteractable(bool state)
    {
        skipButtonEnable.gameObject.SetActive(state);
        skipButtonDisable.gameObject.SetActive(!state);
    }
}
