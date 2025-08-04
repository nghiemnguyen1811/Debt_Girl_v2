using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Continuously loops through a set of background images,
/// displaying one at a time with a crossfade effect (next image fades in before current fades out completely).
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("Background Images")]
    [Tooltip("List of UI Image components to be used as backgrounds.")]
    [SerializeField] private Image[] backgroundImages;

    [Header("Timing Settings")]
    [Tooltip("Duration for each fade-in and fade-out transition.")]
    [SerializeField] private float fadeDuration = 1f;

    [Tooltip("Time each image stays fully visible before transitioning.")]
    [SerializeField] private float displayDuration = 3f;

    #endregion

    #region === Private Runtime Fields ===

    private int currentIndex = -1;
    private Image currentImage;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        InitializeImages();
        ShowInitialImage();
        StartCoroutine(LoopBackgrounds());
    }

    #endregion

    #region === Private Methods ===

    /// <summary>
    /// Sets all background images to transparent at start.
    /// </summary>
    private void InitializeImages()
    {
        foreach (var img in backgroundImages)
            img.color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// Randomly selects and shows the first image immediately (no fade-in).
    /// </summary>
    private void ShowInitialImage()
    {
        currentIndex = Random.Range(0, backgroundImages.Length);
        currentImage = backgroundImages[currentIndex];
        currentImage.color = new Color(1, 1, 1, 1);
        currentImage.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Coroutine that loops through background images with overlapping crossfade.
    /// </summary>
    private IEnumerator LoopBackgrounds()
    {
        // Wait before starting the transition loop
        yield return new WaitForSeconds(displayDuration);

        while (true)
        {
            // Start fade out current image
            currentImage.DOFade(0f, fadeDuration);

            // Wait until 75% of fade-out time (e.g. if fade = 1s, wait 0.75s)
            yield return new WaitForSeconds(fadeDuration * 0.65f);

            // Select next image (not same as current)
            int nextIndex;
            do
            {
                nextIndex = Random.Range(0, backgroundImages.Length);
            } while (nextIndex == currentIndex);

            currentIndex = nextIndex;
            currentImage = backgroundImages[currentIndex];
            currentImage.transform.SetAsLastSibling();

            // Start fade in new image
            currentImage.DOFade(1f, fadeDuration);

            // Wait until fade in is complete and full display time
            yield return new WaitForSeconds(fadeDuration + displayDuration * 0.35f);
            yield return new WaitForSeconds(displayDuration);
        }
    }

    #endregion
}
