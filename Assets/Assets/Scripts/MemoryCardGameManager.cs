using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MemoryCardGameManager : MonoBehaviour
{
    [Header("Game Components")]
    [SerializeField] GridManager gridManager;
    [SerializeField] AudioManager audioManager;
    [SerializeField] SaveLoadManager saveLoadManager;
    [SerializeField] ScoreManager scoreManager;

    [Header("Game Settings")]
    [SerializeField] float mismatchDelay = 1.5f;
    [SerializeField] int maxFlippedCards = 2;
    [SerializeField] bool allowContinuousFlipping = true;
    
    [Header("Preview Settings")]
    [SerializeField] float previewDuration = 2f;
    [SerializeField] bool enablePreview = true;

    [Header("Current Grid Settings")]
    [SerializeField] int currentRows = 3;
    [SerializeField] int currentColumns = 4;

    // Game State
    public int CurrentScore { get; private set; }
    public int CurrentTurns { get; private set; }
    public int MatchedPairs { get; private set; }
    public bool IsGameActive { get; private set; }

    // Card Management
    List<Card> flippedCards = new List<Card>();
    Queue<Card> cardClickQueue = new Queue<Card>();
    bool isProcessingMatch = false;
    Coroutine matchProcessingCoroutine;
    
    // Preview phase
    bool isInPreviewPhase = false;
    Coroutine previewCoroutine;

    // Events
    public Action<int> OnScoreChanged;
    public Action<int> OnTurnsChanged;
    public Action<int, int> OnPairsChanged; // matched, total
    public Action OnGameWon;
    public Action OnGameStarted;
    public Action OnPreviewStarted;
    public Action OnPreviewEnded;

    void Awake()
    {
        if (!gridManager) gridManager = GetComponent<GridManager>();
        if (!audioManager) audioManager = FindObjectOfType<AudioManager>();
        if (!saveLoadManager) saveLoadManager = GetComponent<SaveLoadManager>();
        if (!scoreManager) scoreManager = GetComponent<ScoreManager>();
    }

    void Start() => InitializeGame();

    public void InitializeGame()
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found!");
            return;
        }

        // Subscribe to grid events
        gridManager.OnCardClicked += HandleCardClick;

        // Try to load saved game, otherwise start new game
        if (saveLoadManager != null && saveLoadManager.HasSavedGame()) LoadGame();
        else StartNewGame(currentRows, currentColumns);
    }

    public void StartNewGame(int rows, int columns)
    {
        Debug.Log($"Starting new game with {rows}x{columns} grid");

        // Reset game state
        ResetGameState();

        // Store current grid size
        currentRows = rows;
        currentColumns = columns;

        // Generate new grid
        if (!GridManager.IsValidGridSize(rows, columns))
        {
            Debug.LogError($"Invalid grid size: {rows}x{columns}");
            return;
        }

        gridManager.GenerateGrid(rows, columns);

        // Notify listeners
        OnGameStarted?.Invoke();
        UpdateUI();

        // Start preview phase if enabled
        if (enablePreview)
        {
            StartPreviewPhase();
        }
        else
        {
            // Set game as active immediately if no preview
            IsGameActive = true;
        }

        Debug.Log($"New game started: {gridManager.TotalPairs} pairs to match");
    }

    void StartPreviewPhase()
    {
        Debug.Log("Starting preview phase - revealing all cards");
        
        isInPreviewPhase = true;
        IsGameActive = false; // Disable interaction during preview
        
        // Reveal all cards
        var allCards = gridManager.GetAllCards();
        foreach (Card card in allCards)
        {
            if (card != null)
            {
                card.FlipToFront(immediate: true);
                card.SetInteractable(false); // Disable clicking during preview
            }
        }
        
        OnPreviewStarted?.Invoke();
        
        // Start preview timer
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
        }
        previewCoroutine = StartCoroutine(PreviewPhaseCoroutine());
    }

    IEnumerator PreviewPhaseCoroutine()
    {
        // Wait for preview duration
        yield return new WaitForSeconds(previewDuration);
        
        EndPreviewPhase();
    }

    void EndPreviewPhase()
    {
        Debug.Log("Preview phase ended - hiding all cards");
        
        isInPreviewPhase = false;
        
        // Hide all cards
        var allCards = gridManager.GetAllCards();
        foreach (Card card in allCards)
        {
            if (card != null)
            {
                card.FlipToBack(immediate: true);
                card.SetInteractable(true); // Re-enable clicking after preview
            }
        }
        
        // Now enable game interaction
        IsGameActive = true;
        
        OnPreviewEnded?.Invoke();
        
        Debug.Log("Game is now active - players can start matching!");
    }

    void HandleCardClick(Card clickedCard)
    {
        if (!IsGameActive || clickedCard == null || isInPreviewPhase)
            return;

        // Add to queue for processing
        if (allowContinuousFlipping)
        {
            cardClickQueue.Enqueue(clickedCard);
            ProcessCardQueue();
        }
        else
        {
            // Traditional method: wait for current processing to finish
            if (!isProcessingMatch)
            {
                ProcessCardClick(clickedCard);
            }
        }
    }

    void ProcessCardQueue()
    {
        // Process cards from queue if we're not at the limit
        while (cardClickQueue.Count > 0 && flippedCards.Count < maxFlippedCards)
        {
            Card cardToFlip = cardClickQueue.Dequeue();
            
            // Validate card can be flipped
            if (CanFlipCard(cardToFlip))
            {
                ProcessCardClick(cardToFlip);
            }
        }
    }

    bool CanFlipCard(Card card)
    {
        return card != null && 
               card.CurrentState == CardState.FaceDown && 
               !flippedCards.Contains(card);
    }

    void ProcessCardClick(Card clickedCard)
    {
        if (!CanFlipCard(clickedCard)) return;

        // Flip the card
        clickedCard.FlipToFront();
        flippedCards.Add(clickedCard);

        // Play flip sound
        if (audioManager) audioManager.PlayCardFlip();

        Debug.Log($"Card flipped: ID {clickedCard.CardId}, Total flipped: {flippedCards.Count}");

        // Check if we have enough cards to evaluate
        if (flippedCards.Count >= maxFlippedCards) EvaluateFlippedCards();
    }

    void EvaluateFlippedCards()
    {
        if (flippedCards.Count < 2)
            return;

        // Increment turn counter (one turn = attempt to match 2 cards)
        CurrentTurns++;
        OnTurnsChanged?.Invoke(CurrentTurns);

        // Check if cards match
        bool isMatch = CheckForMatch();

        if (isMatch) HandleMatch();
        else HandleMismatch();
    }

    bool CheckForMatch()
    {
        if (flippedCards.Count < 2) return false;

        // For simplicity, check first two cards
        Card card1 = flippedCards[0];
        Card card2 = flippedCards[1];

        return card1.CardId == card2.CardId;
    }

    void HandleMatch()
    {
        Debug.Log("Match found!");

        // Play match sound
        if (audioManager) audioManager.PlayMatch();

        // Mark cards as matched
        foreach (Card card in flippedCards) card.SetMatched();

        // Update score through ScoreManager
        if (scoreManager != null)
        {
            scoreManager.AddMatch();
            CurrentScore = scoreManager.TotalScore;
        }
        else
        {
            CurrentScore++;
        }
        
        MatchedPairs++;

        // Clear flipped cards list
        flippedCards.Clear();

        // Update UI
        OnScoreChanged?.Invoke(CurrentScore);
        OnPairsChanged?.Invoke(MatchedPairs, gridManager.TotalPairs);

        // Process any queued cards
        if (allowContinuousFlipping) ProcessCardQueue();

        // Check win condition
        CheckWinCondition();

        Debug.Log($"Score: {CurrentScore}, Matched Pairs: {MatchedPairs}/{gridManager.TotalPairs}");
    }

    void HandleMismatch()
    {
        Debug.Log("Mismatch - cards will flip back");

        // Play mismatch sound
        if (audioManager) audioManager.PlayMismatch();

        // Notify score manager of mismatch (for combo reset)
        if (scoreManager) scoreManager.AddMismatch();

        // Notify score manager of mismatch (for combo reset)
        if (scoreManager) scoreManager.AddMismatch();

        // Start coroutine to flip cards back after delay
        if (matchProcessingCoroutine != null) StopCoroutine(matchProcessingCoroutine);

        matchProcessingCoroutine = StartCoroutine(FlipCardsBackAfterDelay());
    }

    IEnumerator FlipCardsBackAfterDelay()
    {
        isProcessingMatch = true;

        // Wait for the specified delay
        yield return new WaitForSeconds(mismatchDelay);

        // Flip cards back to face down
        List<Card> cardsToFlipBack = new List<Card>(flippedCards);
        flippedCards.Clear();

        foreach (Card card in cardsToFlipBack)
        {
            if (card && card.CurrentState == CardState.FaceUp) card.FlipToBack();
        }

        isProcessingMatch = false;

        // Process any queued cards after mismatch processing
        if (allowContinuousFlipping) ProcessCardQueue();

        Debug.Log("Cards flipped back, ready for next turn");
    }

    void CheckWinCondition()
    {
        if (gridManager.IsGridComplete())
        {
            Debug.Log("Game Won!");
            IsGameActive = false;

            // Play game over sound
            if (audioManager) audioManager.PlayGameOver();

            OnGameWon?.Invoke();

            // Auto-save completion
            if (saveLoadManager) saveLoadManager.ClearSavedGame(); // Clear since game is complete
        }
        else
        {
            // Auto-save progress
            if (saveLoadManager) SaveGame();
        }
    }

    void ResetGameState()
    {
        CurrentScore = 0;
        CurrentTurns = 0;
        MatchedPairs = 0;
        IsGameActive = false;
        isInPreviewPhase = false;

        // Reset score manager
        if (scoreManager) scoreManager.ResetScore();

        flippedCards.Clear();
        cardClickQueue.Clear();
        isProcessingMatch = false;

        // Stop any running coroutines
        if (matchProcessingCoroutine != null)
        {
            StopCoroutine(matchProcessingCoroutine);
            matchProcessingCoroutine = null;
        }
        
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }
    }

    void UpdateUI()
    {
        OnScoreChanged?.Invoke(CurrentScore);
        OnTurnsChanged?.Invoke(CurrentTurns);
        OnPairsChanged?.Invoke(MatchedPairs, gridManager?.TotalPairs ?? 0);
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game");
        StartNewGame(currentRows, currentColumns);
    }

    public void SaveGame()
    {
        if (!saveLoadManager || !IsGameActive) return;

        var gameData = new GameSaveData
        {
            score = CurrentScore,
            turns = CurrentTurns,
            matchedPairs = MatchedPairs,
            gridRows = currentRows,
            gridColumns = currentColumns,
            cardStates = GetCardStatesForSave()
        };

        saveLoadManager.SaveGame(gameData);
        Debug.Log("Game saved");
    }

    public void LoadGame()
    {
        if (!saveLoadManager || !saveLoadManager.HasSavedGame()) return;

        var gameData = saveLoadManager.LoadGame();
        if (gameData == null) return;

        Debug.Log("Loading saved game");

        // Restore game state
        CurrentScore = gameData.score;
        CurrentTurns = gameData.turns;
        MatchedPairs = gameData.matchedPairs;
        currentRows = gameData.gridRows;
        currentColumns = gameData.gridColumns;

        // Generate grid with saved dimensions
        gridManager.GenerateGrid(currentRows, currentColumns);

        // Restore card states if available
        if (gameData.cardStates != null && gameData.cardStates.Count > 0) RestoreCardStates(gameData.cardStates);

        IsGameActive = true;
        UpdateUI();
        OnGameStarted?.Invoke();

        Debug.Log($"Game loaded: Score {CurrentScore}, Turns {CurrentTurns}");
    }

    List<CardSaveData> GetCardStatesForSave()
    {
        var cardStates = new List<CardSaveData>();
        var allCards = gridManager.GetAllCards();

        for (int i = 0; i < allCards.Count; i++)
        {
            var card = allCards[i];
            cardStates.Add(new CardSaveData
            {
                cardIndex = i,
                cardId = card.CardId,
                state = card.CurrentState
            });
        }

        return cardStates;
    }

    void RestoreCardStates(List<CardSaveData> cardStates)
    {
        var allCards = gridManager.GetAllCards();

        foreach (var savedCard in cardStates)
        {
            if (savedCard.cardIndex >= 0 && savedCard.cardIndex < allCards.Count)
            {
                var card = allCards[savedCard.cardIndex];
                
                switch (savedCard.state)
                {
                    case CardState.FaceUp:
                        card.FlipToFront(immediate: true);
                        break;
                    case CardState.Matched:
                        card.FlipToFront(immediate: true);
                        card.SetMatched();
                        break;
                    case CardState.FaceDown:
                    default:
                        card.ResetCard();
                        break;
                }
            }
        }
    }

    public void SetGridSize(int rows, int columns)
    {
        currentRows = rows;
        currentColumns = columns;
    }

    public string GetCurrentGridSize() => $"{currentRows}x{currentColumns}";

    // Public getters for UI
    public int GetTotalPairs() => gridManager?.TotalPairs ?? 0;

    public bool HasActiveGame() => IsGameActive;

    public bool IsInPreviewPhase() => isInPreviewPhase;

    public void SetPreviewSettings(bool enabled, float duration)
    {
        enablePreview = enabled;
        previewDuration = Mathf.Max(0.5f, duration);
    }

    public float GetPreviewDuration() => previewDuration;

    void OnDestroy()
    {
        if (gridManager != null) gridManager.OnCardClicked -= HandleCardClick;
    }
}