using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Stores runtime progress and handles localized descriptions for daily quests.
/// </summary>
[System.Serializable]
public class DailyQuestData
{
    #region Quest Reference

    public string questID;
    public DailyQuestDataSO questTemplate;

    #endregion

    #region Progress Data

    public int targetCount;
    public int currentCount;
    public bool isCompleted;
    public bool hasClaimedReward;

    #endregion

    #region Interact Quest Data

    public DailyActivity selectedActivity = DailyActivity.None;
    public int savedActivityInt;

    #endregion

    #region Cached Data

    private string cachedLocalizedDesc;

    #endregion

    #region Localization

    /// <summary>
    /// Returns the localized quest description.  
    /// Supports both normal quests and interact-type quests.
    /// </summary>
    public async Task<string> GetLocalizedDescriptionAsync()
    {
        if (questTemplate == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(cachedLocalizedDesc))
            return cachedLocalizedDesc;

        string localized = string.Empty;

        // Interact quest → get key from the selected activity
        if (questTemplate.questType == DailyQuestType.Interact)
        {
            foreach (var req in questTemplate.activityRequirements)
            {
                if (req.activity == selectedActivity &&
                    !string.IsNullOrEmpty(req.descriptionKey))
                {
                    localized = await LocalizationManager.Instance
                        .GetLocalizedString("Daily Labels", req.descriptionKey);
                    break;
                }
            }
        }
        // Normal quest → use template key
        else if (!string.IsNullOrEmpty(questTemplate.descriptionKey))
        {
            localized = await LocalizationManager.Instance
                .GetLocalizedString("Daily Labels", questTemplate.descriptionKey);
        }

        if (string.IsNullOrEmpty(localized))
            localized = "Missing description.";

        // Insert target number into {0} placeholder
        cachedLocalizedDesc = localized.Contains("{0}")
            ? string.Format(localized, targetCount)
            : localized;

        return cachedLocalizedDesc;
    }

    /// <summary>
    /// Clears cached localization so the text reloads after language change.
    /// </summary>
    public void ClearCachedDescription()
    {
        cachedLocalizedDesc = null;
    }

    #endregion

    #region Progress Logic

    /// <summary>
    /// Increases quest progress and marks completion when target is reached.
    /// </summary>
    public void AddProgress()
    {
        if (isCompleted)
            return;

        currentCount++;

        if (currentCount >= targetCount)
            isCompleted = true;
    }

    #endregion
}
