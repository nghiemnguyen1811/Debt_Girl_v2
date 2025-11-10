using UnityEngine;

/// <summary>
/// Holds all global managers (Audio, Data, Save, etc.)
/// Should only exist once in the very first scene (e.g., Bootstrap or MainMenu).
/// Keeps itself alive across scene loads using DontDestroyOnLoad.
/// </summary>
public class GlobalManagers : SingletonMonobehaviour<GlobalManagers>
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}
