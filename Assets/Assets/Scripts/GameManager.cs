using UnityEngine;

public enum GameState
{
    MainMenu,
    Gameplay,
    GameOver,
    DifficultyComplete,
    GamePaused
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] private GameState currentGameState;
    [SerializeField] private MemoryCardGameManager memoryCardGameManager;

    public GameState CurrentGameState
    {
        get { return currentGameState; }
        set { currentGameState = value; }
    }

    public MemoryCardGameManager MemoryGameManager => memoryCardGameManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame() => ChangeGameState(GameState.Gameplay);

    public void StartGameWithDifficulty(Difficulty difficulty)
    {
        if (memoryCardGameManager != null)
        {
            var gridSize = GetGridSizeForDifficulty(difficulty);
            memoryCardGameManager.StartNewGame(gridSize.rows, gridSize.columns);
        }
        ChangeGameState(GameState.Gameplay);
    }

    public void ReturnToMainMenu() => ChangeGameState(GameState.MainMenu);

    public void EndGame() => ChangeGameState(GameState.GameOver);

    public void CompleteDifficulty() => ChangeGameState(GameState.DifficultyComplete);

    public void PauseGame() => ChangeGameState(GameState.GamePaused);

    public void ResumeGame() => ChangeGameState(GameState.Gameplay);

    private (int rows, int columns) GetGridSizeForDifficulty(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.VeryEasy => (3, 4),  // 12 cards, 6 pairs
            Difficulty.Easy => (4, 4),      // 16 cards, 8 pairs
            Difficulty.Medium => (4, 5),    // 20 cards, 10 pairs
            Difficulty.Hard => (5, 6),      // 30 cards, 15 pairs
            Difficulty.VeryHard => (6, 6),  // 36 cards, 18 pairs
            _ => (3, 4)
        };
    }

    private void Start()
    {
        // Find MemoryCardGameManager if not assigned
        if (memoryCardGameManager == null)
        {
            memoryCardGameManager = FindObjectOfType<MemoryCardGameManager>();
        }

        // Start with the main menu
        ChangeGameState(GameState.MainMenu);
    }

    private void ChangeGameState(GameState newState)
    {
        currentGameState = newState;

        if (!UIManager.Instance) return;

        switch(newState){
            case GameState.MainMenu:
                UIManager.Instance.ShowMainMenu();
                break;
            case GameState.Gameplay:
                UIManager.Instance.HideMainMenu();
                UIManager.Instance.ShowGameplayUI();
                break;
            case GameState.GameOver:
                UIManager.Instance.ShowGameOverUI();
                break;
            case GameState.DifficultyComplete:
                UIManager.Instance.ShowDifficultyCompleteUI();
                break;
            case GameState.GamePaused:
                UIManager.Instance.ShowPauseUI();
                break;
            default:
                Debug.LogWarning($"Unhandled game state: {newState}");
                break;
        }
    }
}
