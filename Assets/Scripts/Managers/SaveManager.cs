using UnityEngine;
using System.IO;

public static class SaveManager
{
    // Path to save file (platform dependent: Windows, Mac, Android, iOS, etc.)
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    // Current save data stored in memory (RAM)
    private static SaveData currentData;

    /// <summary>
    /// Public accessor for current save data.
    /// If not loaded yet, automatically load from file.
    /// </summary>
    public static SaveData Data
    {
        get
        {
            if (currentData == null)
                currentData = LoadGame();

            return currentData;
        }
    }

    /// <summary>
    /// Save the current data to JSON file.
    /// </summary>
    public static void SaveGame()
    {
        string json = JsonUtility.ToJson(Data, true); // Convert SaveData object to JSON
        File.WriteAllText(SavePath, json);           // Write JSON string to file
        Debug.Log("[SaveManager] Game saved to: " + SavePath);
    }

    /// <summary>
    /// Load save data from JSON file.
    /// If file does not exist, create a new SaveData.
    /// </summary>
    public static SaveData LoadGame()
    {
        Debug.Log("[SaveManager] Looking for save file at: " + SavePath);

        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save file found, creating new SaveData");
            currentData = new SaveData(); // create empty save
            return currentData;
        }

        string json = File.ReadAllText(SavePath);              // Read JSON string
        currentData = JsonUtility.FromJson<SaveData>(json);    // Deserialize back to SaveData object
        Debug.Log("[SaveManager] Save file loaded from: " + SavePath);
        return currentData;
    }

    /// <summary>
    /// Delete the save file and reset to new SaveData (useful for debugging).
    /// </summary>
    public static void ClearSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] Save file deleted at: " + SavePath);
        }
        currentData = new SaveData();
    }
}
