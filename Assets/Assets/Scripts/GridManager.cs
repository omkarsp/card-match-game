using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private float gridPadding = 20f;
    [SerializeField] private float cardSpacing = 10f;
    [SerializeField] private float minCardSize = 60f;
    [SerializeField] private float maxCardSize = 150f;

    [Header("Card Sprites")]
    [SerializeField] private Sprite[] cardFrontSprites;
    [SerializeField] private Sprite cardBackSprite;

    public int Rows { get; private set; }
    public int Columns { get; private set; }
    public int TotalCards { get; private set; }
    public int TotalPairs { get; private set; }

    private List<Card> allCards = new List<Card>();
    private Dictionary<int, List<Card>> cardPairs = new Dictionary<int, List<Card>>();
    
    public System.Action<Card> OnCardClicked;

    public void GenerateGrid(int rows, int columns)
    {
        // Clear existing grid
        ClearGrid();
        
        // Validate grid size
        if (rows <= 0 || columns <= 0)
        {
            Debug.LogError("Invalid grid size: rows and columns must be positive");
            return;
        }
        
        int totalCards = rows * columns;
        if (totalCards % 2 != 0)
        {
            Debug.LogError("Total cards must be even for pairing");
            return;
        }

        Rows = rows;
        Columns = columns;
        TotalCards = totalCards;
        TotalPairs = totalCards / 2;

        // Calculate card size and positioning
        Vector2 cardSize = CalculateCardSize(rows, columns);
        
        // Generate card pairs
        List<int> cardIds = GenerateCardIds();
        
        // Shuffle the card IDs
        ShuffleList(cardIds);
        
        // Create and position cards
        CreateCards(cardIds, cardSize);
        
        Debug.Log($"Generated {rows}x{columns} grid with {TotalCards} cards ({TotalPairs} pairs)");
    }

    private void ClearGrid()
    {
        foreach (Card card in allCards)
        {
            if (card != null)
            {
                DestroyImmediate(card.gameObject);
            }
        }
        
        allCards.Clear();
        cardPairs.Clear();
    }

    private Vector2 CalculateCardSize(int rows, int columns)
    {
        if (gridContainer == null)
        {
            Debug.LogError("Grid container not assigned");
            return Vector2.one * 100f;
        }

        // Get available space
        RectTransform containerRect = gridContainer;
        float availableWidth = containerRect.rect.width - (gridPadding * 2);
        float availableHeight = containerRect.rect.height - (gridPadding * 2);
        
        // Calculate maximum card size based on available space
        float maxCardWidth = (availableWidth - (cardSpacing * (columns - 1))) / columns;
        float maxCardHeight = (availableHeight - (cardSpacing * (rows - 1))) / rows;
        
        // Use the smaller dimension to maintain square cards
        float cardSize = Mathf.Min(maxCardWidth, maxCardHeight);
        
        // Clamp to min/max size limits
        cardSize = Mathf.Clamp(cardSize, minCardSize, maxCardSize);
        
        return new Vector2(cardSize, cardSize);
    }

    private List<int> GenerateCardIds()
    {
        List<int> cardIds = new List<int>();
        
        // Create pairs of card IDs
        for (int i = 0; i < TotalPairs; i++)
        {
            // Add each ID twice to create pairs
            cardIds.Add(i);
            cardIds.Add(i);
        }
        
        return cardIds;
    }

    private void CreateCards(List<int> cardIds, Vector2 cardSize)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Card prefab not assigned");
            return;
        }

        // Calculate starting position (top-left of grid)
        float startX = -((Columns - 1) * (cardSize.x + cardSpacing)) / 2f;
        float startY = ((Rows - 1) * (cardSize.y + cardSpacing)) / 2f;

        int cardIndex = 0;
        
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                // Calculate position
                float xPos = startX + col * (cardSize.x + cardSpacing);
                float yPos = startY - row * (cardSize.y + cardSpacing);
                
                // Create card
                Card newCard = Instantiate(cardPrefab, gridContainer);
                
                // Set position and size
                RectTransform cardRect = newCard.GetComponent<RectTransform>();
                cardRect.anchoredPosition = new Vector2(xPos, yPos);
                cardRect.sizeDelta = cardSize;
                
                // Get card ID and sprite
                int cardId = cardIds[cardIndex];
                Sprite cardSprite = GetCardSprite(cardId);
                
                // Initialize card
                newCard.Initialize(cardId, cardSprite);
                newCard.OnCardClicked += HandleCardClick;
                
                // Add to tracking collections
                allCards.Add(newCard);
                
                // Track pairs
                if (!cardPairs.ContainsKey(cardId))
                {
                    cardPairs[cardId] = new List<Card>();
                }
                cardPairs[cardId].Add(newCard);
                
                cardIndex++;
            }
        }
    }

    private Sprite GetCardSprite(int cardId)
    {
        if (cardFrontSprites == null || cardFrontSprites.Length == 0)
        {
            Debug.LogWarning("No card front sprites assigned, using default");
            return null;
        }
        
        // Cycle through available sprites
        int spriteIndex = cardId % cardFrontSprites.Length;
        return cardFrontSprites[spriteIndex];
    }

    private void HandleCardClick(Card clickedCard)
    {
        OnCardClicked?.Invoke(clickedCard);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public List<Card> GetMatchingCards(int cardId)
    {
        return cardPairs.ContainsKey(cardId) ? cardPairs[cardId] : new List<Card>();
    }

    public void ResetAllCards()
    {
        foreach (Card card in allCards)
        {
            if (card != null)
            {
                card.ResetCard();
            }
        }
    }

    public int GetMatchedPairsCount()
    {
        int matchedPairs = 0;
        
        foreach (var pair in cardPairs)
        {
            bool allMatched = true;
            foreach (Card card in pair.Value)
            {
                if (card.CurrentState != CardState.Matched)
                {
                    allMatched = false;
                    break;
                }
            }
            
            if (allMatched)
            {
                matchedPairs++;
            }
        }
        
        return matchedPairs;
    }

    public bool IsGridComplete()
    {
        return GetMatchedPairsCount() == TotalPairs;
    }

    // Utility method to get grid size as string
    public string GetGridSizeString()
    {
        return $"{Rows}x{Columns}";
    }

    // Method to validate if a grid size is valid
    public static bool IsValidGridSize(int rows, int columns)
    {
        return rows > 0 && columns > 0 && (rows * columns) % 2 == 0;
    }

    // Get all cards (for save/load purposes)
    public List<Card> GetAllCards()
    {
        return new List<Card>(allCards);
    }

    private void OnValidate()
    {
        // Ensure minimum values in inspector
        if (gridPadding < 0) gridPadding = 0;
        if (cardSpacing < 0) cardSpacing = 0;
        if (minCardSize < 10) minCardSize = 10;
        if (maxCardSize < minCardSize) maxCardSize = minCardSize;
    }
}