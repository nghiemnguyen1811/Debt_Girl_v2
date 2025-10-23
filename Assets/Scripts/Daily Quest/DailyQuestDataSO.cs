using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Quests/Daily Quest Template", fileName = "NewDailyQuest")]
public class DailyQuestDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // QUEST SETTINGS
    // ─────────────────────────────────────────────────────
    [Header("Quest Settings")]
    public DailyQuestType questType;

    [TextArea(2, 4)]
    [ShowIf("@questType != DailyQuestType.Interact")]
    public string description;

    // ─────────────────────────────────────────────────────
    // TARGET & REWARD SETTINGS
    // ─────────────────────────────────────────────────────
    [Header("Target & Reward")]
    [MinValue(1)] public int minTarget = 1;
    [MinValue(1)] public int maxTarget = 3;
    [MinValue(1)] public int rewardDiamond = 1;

    // ─────────────────────────────────────────────────────
    // UNLOCK CONDITION
    // ─────────────────────────────────────────────────────
    [Header("Unlock Condition")]
    [ShowIf("@questType != DailyQuestType.Interact")]
    [MinValue(1)] public int requiredLevel = 1;

    // ─────────────────────────────────────────────────────
    // INTERACT ACTIVITY REQUIREMENTS (ODIN CONDITIONAL)
    // ─────────────────────────────────────────────────────
    [ShowIf("@questType == DailyQuestType.Interact")]
    [BoxGroup("Daily Activity Requirement")]
    [LabelText("Interact Activities")]
    public DailyActivityRequirement[] activityRequirements;
}

[System.Serializable]
public class DailyActivityRequirement
{
    [LabelText("Daily Activity Type")]
    public DailyActivity activity;

    [LabelText("Description")]
    [TextArea(1, 3)]
    public string description;

    [LabelText("Required Level")]
    [MinValue(1)]
    public int requiredLevel = 1;
}
