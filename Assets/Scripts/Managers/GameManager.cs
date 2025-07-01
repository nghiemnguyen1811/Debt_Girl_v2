using UnityEngine;

public class GameManager : SingletonMonobehaviour<GameManager>
{
    [Header("Player Progress")]
    private int currentLevel = 1;

    public int CurrentLevel => currentLevel;

    public void IncreaseLevel()
    {
        currentLevel++;
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
    }
}
