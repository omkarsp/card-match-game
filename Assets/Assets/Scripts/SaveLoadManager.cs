using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public int score;
    public int turns;
    public int matchedPairs;
    public int gridRows;
    public int gridColumns;
    public List<CardSaveData> cardStates;
    public string saveTimestamp;

    public GameSaveData()
    {
        cardStates = new List<CardSaveData>();
        saveTimestamp = System.DateTime.Now.ToBinary().ToString();
    }
}

[System.Serializable]
public class CardSaveData
{
    public int cardIndex;
    public int cardId;
    public CardState state;
}

public class SaveLoadManager : MonoBehaviour
{
    private const string SAVE_KEY = "MemoryCardGame_SaveData";
    private const string HAS_SAVE_KEY = "MemoryCardGame_HasSave";

    [Header("Save Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private bool debugMode = false;

    private GameSaveData currentSaveData;

    private void Awake()
    {
        // Ensure we have a save data instance
        if (currentSaveData == null)
        {
            currentSaveData = new GameSaveData();
        }
    }

    public void SaveGame(GameSaveData gameData)
    {
        try
        {
            // Update timestamp
            gameData.saveTimestamp = System.DateTime.Now.ToBinary().ToString();
            
            // Serialize to JSON
            string jsonData = JsonUtility.ToJson(gameData, true);
            
            // Save to PlayerPrefs
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.SetInt(HAS_SAVE_KEY, 1);
            PlayerPrefs.Save();
            
            // Store current save data
            currentSaveData = gameData;
            
            if (debugMode)
            {
                Debug.Log($"Game saved successfully:\n{jsonData}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    public GameSaveData LoadGame()
    {
        try
        {
            if (!HasSavedGame())
            {
                Debug.LogWarning("No saved game found");
                return null;
            }

            string jsonData = PlayerPrefs.GetString(SAVE_KEY, "");
            
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning("Save data is empty");
                return null;
            }

            GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(jsonData);
            
            if (loadedData == null)
            {
                Debug.LogError("Failed to deserialize save data");
                return null;
            }

            currentSaveData = loadedData;

            if (debugMode)
            {
                Debug.Log($"Game loaded successfully:\n{jsonData}");
            }

            return loadedData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return null;
        }
    }

    public bool HasSavedGame()
    {
        return PlayerPrefs.GetInt(HAS_SAVE_KEY, 0) == 1 && 
               !string.IsNullOrEmpty(PlayerPrefs.GetString(SAVE_KEY, ""));
    }

    public void ClearSavedGame()
    {
        try
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.DeleteKey(HAS_SAVE_KEY);
            PlayerPrefs.Save();
            
            currentSaveData = new GameSaveData();
            
            Debug.Log("Saved game cleared");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to clear saved game: {e.Message}");
        }
    }

    public GameSaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }

    public string GetSaveTimestamp()
    {
        if (currentSaveData == null || string.IsNullOrEmpty(currentSaveData.saveTimestamp))
            return "No save data";

        try
        {
            long timeBinary = System.Convert.ToInt64(currentSaveData.saveTimestamp);
            System.DateTime saveTime = System.DateTime.FromBinary(timeBinary);
            return saveTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch
        {
            return "Invalid timestamp";
        }
    }

    public bool ValidateSaveData(GameSaveData data)
    {
        if (data == null)
            return false;

        // Basic validation
        if (data.score < 0 || data.turns < 0 || data.matchedPairs < 0)
            return false;

        if (data.gridRows <= 0 || data.gridColumns <= 0)
            return false;

        // Validate grid size is even for pairing
        if ((data.gridRows * data.gridColumns) % 2 != 0)
            return false;

        // Validate score doesn't exceed possible pairs
        int totalPairs = (data.gridRows * data.gridColumns) / 2;
        if (data.score > totalPairs || data.matchedPairs > totalPairs)
            return false;

        return true;
    }

    // Utility method for debugging
    public void LogSaveData()
    {
        if (HasSavedGame())
        {
            GameSaveData data = LoadGame();
            if (data != null)
            {
                Debug.Log($"=== SAVE DATA ===");
                Debug.Log($"Score: {data.score}");
                Debug.Log($"Turns: {data.turns}");
                Debug.Log($"Matched Pairs: {data.matchedPairs}");
                Debug.Log($"Grid Size: {data.gridRows}x{data.gridColumns}");
                Debug.Log($"Card States: {data.cardStates?.Count ?? 0}");
                Debug.Log($"Save Time: {GetSaveTimestamp()}");
            }
        }
        else
        {
            Debug.Log("No saved game data found");
        }
    }

    // Auto-save functionality
    public void EnableAutoSave(bool enable)
    {
        autoSave = enable;
    }

    public bool IsAutoSaveEnabled()
    {
        return autoSave;
    }

    // For testing purposes
    public void CreateTestSave()
    {
        var testData = new GameSaveData
        {
            score = 5,
            turns = 12,
            matchedPairs = 5,
            gridRows = 4,
            gridColumns = 4,
            cardStates = new List<CardSaveData>()
        };

        // Create some test card states
        for (int i = 0; i < 16; i++)
        {
            testData.cardStates.Add(new CardSaveData
            {
                cardIndex = i,
                cardId = i / 2,
                state = i < 10 ? CardState.Matched : CardState.FaceDown
            });
        }

        SaveGame(testData);
        Debug.Log("Test save created");
    }

    // Menu integration methods
    public void OnApplicationPause(bool pauseStatus)
    {
        if (autoSave && pauseStatus)
        {
            // Auto-save when application is paused (mobile)
            var gameManager = FindObjectOfType<MemoryCardGameManager>();
            if (gameManager != null && gameManager.HasActiveGame())
            {
                gameManager.SaveGame();
            }
        }
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        if (autoSave && !hasFocus)
        {
            // Auto-save when application loses focus
            var gameManager = FindObjectOfType<MemoryCardGameManager>();
            if (gameManager != null && gameManager.HasActiveGame())
            {
                gameManager.SaveGame();
            }
        }
    }

    private void OnDestroy()
    {
        if (autoSave)
        {
            // Final auto-save on destroy
            var gameManager = FindObjectOfType<MemoryCardGameManager>();
            if (gameManager != null && gameManager.HasActiveGame())
            {
                gameManager.SaveGame();
            }
        }
    }
}