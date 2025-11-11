using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(ScrollRect))]
public class InfiniteScrollRect : MonoBehaviour
{
    // ───────────────────────────────────────────────
    #region === Inspector Fields ===
    [Header("Scroll Reference")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Focus Effect Settings")]
    [SerializeField, Range(0f, 1f)] private float focusThreshold = 0.3f;
    [SerializeField] private float focusedScale = 1.15f;
    [SerializeField] private float tweenDuration = 0.5f;
    #endregion

    // ───────────────────────────────────────────────
    #region === Private Fields ===
    private RectTransform content;
    private List<RectTransform> cards = new();
    private float cardWidth;
    private float spacing;

    private readonly Dictionary<RectTransform, float> currentScales = new();
    #endregion

    // ───────────────────────────────────────────────
    #region === Unity Lifecycle ===
    private void Update()
    {
        if (cards.Count < 2)
            return;

        WrapContent();
        UpdateFocusEffect();
    }
    #endregion

    // ───────────────────────────────────────────────
    #region === Public Initialization ===
    /// <summary>
    /// Initializes the InfiniteScrollRect after all cards have been spawned.
    /// Should be called manually by GuideManager.
    /// </summary>
    public void InitializeScroll()
    {
        InitializeScrollRect();
        CacheCards();
        CalculateLayoutSpacing();
    }
    #endregion

    // ───────────────────────────────────────────────
    #region === Private Setup ===
    private void InitializeScrollRect()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        content = scrollRect.content;
    }

    private void CacheCards()
    {
        cards.Clear();
        currentScales.Clear();

        foreach (Transform child in content)
        {
            if (child is RectTransform rt)
            {
                cards.Add(rt);
                currentScales[rt] = 1f;
            }
        }

        if (cards.Count > 0)
            cardWidth = cards[0].rect.width;
    }

    private void CalculateLayoutSpacing()
    {
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        spacing = layout ? layout.spacing : 0f;
    }
    #endregion

    // ───────────────────────────────────────────────
    #region === Infinite Scroll Wrapping ===
    private void WrapContent()
    {
        List<RectTransform> toLeft = new();
        List<RectTransform> toRight = new();

        foreach (var card in cards)
        {
            float worldLeft = GetWorldLeft(card);
            float worldRight = GetWorldRight(card);

            if (worldLeft > GetWorldRightEdge() + cardWidth / 2f)
                toLeft.Add(card);
            else if (worldRight < GetWorldLeftEdge() - cardWidth / 2f)
                toRight.Add(card);
        }

        foreach (var card in toLeft)
            MoveToLeft(card);
        foreach (var card in toRight)
            MoveToRight(card);
    }
    #endregion

    // ───────────────────────────────────────────────
    #region === Focus Scale Effect ===
    private void UpdateFocusEffect()
    {
        Vector3 viewportCenter = scrollRect.viewport.TransformPoint(
            new Vector3(scrollRect.viewport.rect.width / 2f, 0f, 0f)
        );

        foreach (var card in cards)
        {
            Vector3 cardCenter = card.TransformPoint(new Vector3(card.rect.width / 2f, 0f, 0f));
            float distance = Mathf.Abs(viewportCenter.x - cardCenter.x);
            float normalized = distance / (scrollRect.viewport.rect.width / 2f);

            float targetScale = (normalized < focusThreshold) ? focusedScale : 1f;

            if (Mathf.Abs(currentScales[card] - targetScale) > 0.01f)
            {
                currentScales[card] = targetScale;
                card.DOScale(targetScale, tweenDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }
    }
    #endregion

    // ───────────────────────────────────────────────
    #region === Utility Methods ===
    private float GetWorldLeft(RectTransform card) => card.TransformPoint(card.rect.min).x;
    private float GetWorldRight(RectTransform card) => card.TransformPoint(card.rect.max).x;
    private float GetWorldLeftEdge() => scrollRect.viewport.TransformPoint(((RectTransform)scrollRect.viewport).rect.min).x;
    private float GetWorldRightEdge() => scrollRect.viewport.TransformPoint(((RectTransform)scrollRect.viewport).rect.max).x;

    private void MoveToLeft(RectTransform card)
    {
        RectTransform leftMost = cards[0];
        foreach (var c in cards)
            if (c.anchoredPosition.x < leftMost.anchoredPosition.x)
                leftMost = c;

        float newX = leftMost.anchoredPosition.x - (cardWidth + spacing);
        card.anchoredPosition = new Vector2(newX, card.anchoredPosition.y);

        cards.Remove(card);
        cards.Insert(0, card);
    }

    private void MoveToRight(RectTransform card)
    {
        RectTransform rightMost = cards[0];
        foreach (var c in cards)
            if (c.anchoredPosition.x > rightMost.anchoredPosition.x)
                rightMost = c;

        float newX = rightMost.anchoredPosition.x + (cardWidth + spacing);
        card.anchoredPosition = new Vector2(newX, card.anchoredPosition.y);

        cards.Remove(card);
        cards.Add(card);
    }
    #endregion
}
