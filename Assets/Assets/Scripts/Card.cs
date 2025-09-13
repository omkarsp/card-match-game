using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum CardState
{
    FaceDown,
    FaceUp,
    Matched,
    Flipping
}

public class Card : MonoBehaviour
{
    [Header("Card Components")]
    [SerializeField] Button cardButton;
    [SerializeField] Image cardBack;
    [SerializeField] Image cardFront;
    [SerializeField] CanvasGroup cardGroup;

    [Header("Animation Settings")]
    [SerializeField] float flipDuration = 0.3f;
    [SerializeField] AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Card Data")]
    [SerializeField] int cardId;
    [SerializeField] Sprite cardFrontSprite;

    public int CardId => cardId;
    public CardState CurrentState { get; private set; } = CardState.FaceDown;
    
    public Action<Card> OnCardClicked;
    
    bool isInteractable = true;
    Coroutine flipCoroutine;

    void Awake()
    {
        if (cardButton == null) cardButton = GetComponent<Button>();
        if (cardGroup == null) cardGroup = GetComponent<CanvasGroup>();
        
        cardButton.onClick.AddListener(OnCardButtonClicked);
    }

    public void Initialize(int id, Sprite frontSprite)
    {
        cardId = id;
        cardFrontSprite = frontSprite;
        cardFront.sprite = cardFrontSprite;
        
        // Start with card face down
        SetCardVisual(false);
        CurrentState = CardState.FaceDown;
        SetInteractable(true);
    }

    void OnCardButtonClicked()
    {
        if (!isInteractable || CurrentState != CardState.FaceDown) return;
        
        OnCardClicked?.Invoke(this);
    }

    public void FlipToFront(bool immediate = false)
    {
        if (CurrentState == CardState.FaceUp || CurrentState == CardState.Matched) return;
        
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);

        if (immediate)
        {
            SetCardVisual(true);
            CurrentState = CardState.FaceUp;
        }
        else
        {
            flipCoroutine = StartCoroutine(FlipCardCoroutine(true));
        }
    }

    public void FlipToBack(bool immediate = false)
    {
        if (CurrentState == CardState.FaceDown || CurrentState == CardState.Matched) return;
        
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);

        if (immediate)
        {
            SetCardVisual(false);
            CurrentState = CardState.FaceDown;
        }
        else
        {
            flipCoroutine = StartCoroutine(FlipCardCoroutine(false));
        }
    }

    public void SetMatched()
    {
        if (flipCoroutine != null)
        {
            StopCoroutine(flipCoroutine);
        }
        
        // Ensure card is showing front face when matched
        SetCardVisual(true);
        transform.localScale = Vector3.one; // Reset scale in case it was mid-flip
        
        Debug.Log($"Card {cardId} matched - forced to show front face");
        
        CurrentState = CardState.Matched;
        SetInteractable(false);
        
        // Optional: Add matched visual effect
        StartCoroutine(MatchedEffectCoroutine());
    }

    IEnumerator FlipCardCoroutine(bool faceUp)
    {
        CurrentState = CardState.Flipping;
        SetInteractable(false);
        
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        
        // Flip effect: scale down, change sprite, scale up
        while (elapsedTime < flipDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (flipDuration / 2);
            float curveValue = flipCurve.Evaluate(progress);
            
            transform.localScale = new Vector3(startScale.x * (1 - curveValue), startScale.y, startScale.z);
            yield return null;
        }
        
        // Change the visible sprite at the midpoint
        SetCardVisual(faceUp);
        
        // Scale back up
        elapsedTime = 0f;
        while (elapsedTime < flipDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (flipDuration / 2);
            float curveValue = flipCurve.Evaluate(progress);
            
            transform.localScale = new Vector3(startScale.x * curveValue, startScale.y, startScale.z);
            yield return null;
        }
        
        transform.localScale = startScale;
        
        CurrentState = faceUp ? CardState.FaceUp : CardState.FaceDown;
        
        // Only make interactable if face down and not matched
        if (CurrentState == CardState.FaceDown) SetInteractable(true);
        
        flipCoroutine = null;
    }

    IEnumerator MatchedEffectCoroutine()
    {
        // Simple pulse effect for matched cards
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        float elapsedTime = 0f;
        float effectDuration = 0.2f;
        
        // Scale up
        while (elapsedTime < effectDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / effectDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }
        
        // Scale back down
        elapsedTime = 0f;
        while (elapsedTime < effectDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / effectDuration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }

    void SetCardVisual(bool showFront)
    {
        cardFront.gameObject.SetActive(showFront);
        cardBack.gameObject.SetActive(!showFront);
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        cardButton.interactable = interactable;
        
        if (cardGroup) cardGroup.alpha = interactable ? 1f : 0.8f;
    }

    public void ResetCard()
    {
        if (flipCoroutine != null)
        {
            StopCoroutine(flipCoroutine);
            flipCoroutine = null;
        }
        
        transform.localScale = Vector3.one;
        SetCardVisual(false);
        CurrentState = CardState.FaceDown;
        SetInteractable(true);
    }

    void OnDestroy()
    {
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);
    }
}