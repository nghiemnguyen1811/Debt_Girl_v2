using UnityEngine;

[CreateAssetMenu(fileName = "ScenePreloadData", menuName = "Game/Scene Preload Data")]
public class ScenePreloadDataSO : ScriptableObject
{
    public int sceneIndex;                        // Scene sẽ load
    public string[] addressableKeysToPreload;    // Danh sách key (prefab, model...)
}