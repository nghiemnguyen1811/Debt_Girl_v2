using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles spawning, refreshing, and linking guide cards with their data.
/// </summary>
public class GuideManager : MonoBehaviour
{
    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Guide Data Settings")]
    [SerializeField] private List<GuideCardDataSO> guideCardDataList = new(); // Data list for all guide cards

    [Header("UI References")]
    [SerializeField] private Transform cardParent;            // Parent container (ScrollView content)
    [SerializeField] private GuideCardContainer cardPrefab;   // Prefab template for each guide card
    [SerializeField] private InfiniteScrollRect infiniteScroll; // Infinite scroll controller reference

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Private Runtime Fields ===

    private readonly List<GuideCardContainer> spawnedCards = new(); // Runtime list of spawned cards

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    private void Start()
    {
        SpawnAllCards();

        // Initialize InfiniteScrollRect only after cards are spawned
        if (infiniteScroll != null)
            infiniteScroll.InitializeScroll();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Spawn & Clear Logic ===

    /// <summary>
    /// Spawns guide cards based on the data list.
    /// </summary>
    private void SpawnAllCards()
    {
        ClearExistingCards();

        foreach (var data in guideCardDataList)
        {
            if (data == null || cardPrefab == null || cardParent == null)
                continue;

            var card = Instantiate(cardPrefab, cardParent);
            card.Configure(data);
            spawnedCards.Add(card);
        }
    }

    /// <summary>
    /// Clears all currently spawned guide cards.
    /// </summary>
    private void ClearExistingCards()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        spawnedCards.Clear();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Update / Refresh ===

    /// <summary>
    /// Refreshes all existing guide cards when data changes (e.g. localization).
    /// </summary>
    public void RefreshCards()
    {
        foreach (var (card, data) in GetCardPairs())
        {
            if (card != null && data != null)
                card.Configure(data);
        }
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Utility ===

    /// <summary>
    /// Pairs each spawned card with its corresponding data entry.
    /// </summary>
    private IEnumerable<(GuideCardContainer card, GuideCardDataSO data)> GetCardPairs()
    {
        int count = Mathf.Min(spawnedCards.Count, guideCardDataList.Count);

        for (int i = 0; i < count; i++)
            yield return (spawnedCards[i], guideCardDataList[i]);
    }

    #endregion
}
