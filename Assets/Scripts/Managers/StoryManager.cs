using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls story image transitions, typewriter text, next/skip logic,
/// and notifies MainMenu when the story flow ends.
/// </summary>
public class StoryManager : SingletonMonobehaviour<StoryManager>
{
    //────────────────────────────────────────────────────
    // == Inspector Fields ==
    //────────────────────────────────────────────────────

    [Header("Story Data")]
    [SerializeField] private List<StoryDataSO> storyList = new();

    [Header("UI References")]
    [SerializeField] private Transform imageStackParent;
    [SerializeField] private Image storyImagePrefab;
    [SerializeField] private TextMeshProUGUI storyTextUI;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;

    [Header("Typewriter Settings")]
    [SerializeField] private float typeSpeed = 0.03f;


    //────────────────────────────────────────────────────
    // == Runtime Fields ==
    //────────────────────────────────────────────────────

    private readonly List<Image> stackedImages = new();
    private int currentIndex = 0;
    private bool isTyping = false;


    //────────────────────────────────────────────────────
    // == Unity Lifecycle ==
    //────────────────────────────────────────────────────

    private void Start()
    {
        // Hook up button events
        if (nextButton != null) nextButton.onClick.AddListener(OnClickNext);
        if (skipButton != null) skipButton.onClick.AddListener(SkipStory);

        // Initialize story flow
        SpawnImages();
        ArrangeSiblingOrder();
        ShowCurrentStory();
    }


    //────────────────────────────────────────────────────
    // == Story Initialization ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Spawns story images in the order they appear in storyList.
    /// </summary>
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

    /// <summary>
    /// Adjusts sibling order so storyList[0] is visually on top.
    /// </summary>
    private void ArrangeSiblingOrder()
    {
        int count = stackedImages.Count;

        for (int i = 0; i < count; i++)
        {
            int topIndex = count - 1 - i;
            stackedImages[i].transform.SetSiblingIndex(topIndex);
        }

        UpdateActiveImages();
    }

    /// <summary>
    /// Enables only the currently active image.
    /// </summary>
    private void UpdateActiveImages()
    {
        for (int i = 0; i < stackedImages.Count; i++)
            stackedImages[i].gameObject.SetActive(i >= currentIndex);
    }


    //────────────────────────────────────────────────────
    // == Display Story Text ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Shows the current story text with typewriter effect.
    /// </summary>
    private void ShowCurrentStory()
    {
        StopAllCoroutines();
        storyTextUI.text = "";
        StartCoroutine(Typewriter(storyList[currentIndex].storyText));
    }

    /// <summary>
    /// Reveals text character-by-character.
    /// </summary>
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
    // == Next Button Logic ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Handles Next button click: skip typing, move to next image, or end.
    /// </summary>
    private void OnClickNext()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            storyTextUI.text = storyList[currentIndex].storyText;
            isTyping = false;
            return;
        }

        bool isLastImage = currentIndex == stackedImages.Count - 1;

        if (isLastImage)
            MainMenu.Instance.StartStoryEndSequence();
        else
            SlideOutCurrentImage();
    }


    //────────────────────────────────────────────────────
    // == Skip Button Logic ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Instantly ends story flow without animations.
    /// </summary>
    public void SkipStory()
    {
        StopAllCoroutines();
        MainMenu.Instance.StartStoryEndSequence();
    }


    //────────────────────────────────────────────────────
    // == Reset Logic ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Resets story state so it can play again from the beginning.
    /// </summary>
    public void ResetStory()
    {
        StopAllCoroutines();

        // Remove old images
        foreach (var img in stackedImages)
            Destroy(img.gameObject);

        stackedImages.Clear();

        currentIndex = 0;
        isTyping = false;

        SpawnImages();
        ArrangeSiblingOrder();
        ShowCurrentStory();
    }


    //────────────────────────────────────────────────────
    // == Image Transition ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Slides the current image to the right and fades it out.
    /// </summary>
    private void SlideOutCurrentImage()
    {
        Image img = stackedImages[currentIndex];
        CanvasGroup cg = img.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = img.gameObject.AddComponent<CanvasGroup>();

        RectTransform rt = img.rectTransform;

        DOTween.Sequence()
            .Append(rt.DOAnchorPosX(800f, 1.2f))
            .Join(cg.DOFade(0f, 1.2f))
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
            });
    }


    //────────────────────────────────────────────────────
    // == End of Story ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Notifies MainMenu that the story flow has finished.
    /// </summary>
    private void EndStory()
    {
        MainMenu.Instance.StartStoryEndSequence();
    }
}
