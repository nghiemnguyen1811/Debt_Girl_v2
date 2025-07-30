using System.Collections;
using UnityEngine;

/// <summary>
/// Handles screen fade in/out using CanvasGroup.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Fader : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Coroutine currentActionFade;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        FadeOutImmediate();
        StartCoroutine(FadeInCo(2f));
    }

    /// <summary>
    /// Instantly sets screen to black.
    /// </summary>
    private void FadeOutImmediate()
    {
        canvasGroup.alpha = 1f;
        StartCoroutine(FadeInCo(1f));
    }

    /// <summary>
    /// Coroutine for fade out.
    /// </summary>
    public IEnumerator FadeOutCo(float time) => Fade(1, time);

    /// <summary>
    /// Coroutine for fade in.
    /// </summary>
    public IEnumerator FadeInCo(float time) => Fade(0, time);

    /// <summary>
    /// Base fade coroutine (0 = visible, 1 = black).
    /// </summary>
    private IEnumerator Fade(float target, float time)
    {
        if (currentActionFade != null)
            StopCoroutine(currentActionFade);

        currentActionFade = StartCoroutine(FadeRoutine(target, time));
        yield return currentActionFade;
    }

    /// <summary>
    /// Smoothly fades the canvas alpha.
    /// </summary>
    private IEnumerator FadeRoutine(float target, float time)
    {
        while (!Mathf.Approximately(canvasGroup.alpha, target))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.deltaTime / time);
            yield return null;
        }

        if (target == 0)
            gameObject.SetActive(false);
    }
}
