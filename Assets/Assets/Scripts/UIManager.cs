using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Difficulty
{
    VeryEasy,
    Easy,
    Medium,
    Hard,
    VeryHard
}

public class UIManager : MonoBehaviour
{
    [Header("Main Menu UI")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] Button playButton;
    [SerializeField] Button settingsButton;
    
    [Header("Gameplay UI")]
    [SerializeField] GameObject gameplayPanel;
    [SerializeField] Button homeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button settingsButtonHUD;
    [SerializeField] TextMeshProUGUI difficultyText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI turnsText;
    
    [Header("Level Complete UI")]
    [SerializeField] GameObject levelCompletePanel;
    [SerializeField] Button continueButton;
    [SerializeField] Button menuButton;
    [SerializeField] TextMeshProUGUI levelCompleteText;
    
    [Header("Difficulty Selection")]
    [SerializeField] ToggleGroup difficultyToggleGroup;
    [SerializeField] Toggle veryEasyToggle;
    [SerializeField] Toggle easyToggle;
    [SerializeField] Toggle mediumToggle;
    [SerializeField] Toggle hardToggle;
    [SerializeField] Toggle veryHardToggle;
    
    [Header("Settings")]
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Button closeSettingsButton;
    
    [Header("Audio Settings")]
    [SerializeField] Toggle musicToggle;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Toggle effectsToggle;
    [SerializeField] Slider effectsVolumeSlider;
    
    Difficulty selectedDifficulty = Difficulty.Medium;

    Dictionary<Difficulty, Toggle> difficultyToggleMap = new();
    
    // Audio settings
    bool musicEnabled = true;
    float musicVolume = 0.8f;
    bool effectsEnabled = true;
    float effectsVolume = 0.8f;
    
    public static UIManager Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start() => SetupUI();

    void SetupUI()
    {
        // Setup button listeners
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
        
        // Gameplay UI listeners
        if (homeButton) homeButton.onClick.AddListener(OnHomeButtonClicked);
        if (restartButton) restartButton.onClick.AddListener(OnRestartButtonClicked);
        if (settingsButtonHUD) settingsButtonHUD.onClick.AddListener(OnSettingsButtonClicked);
        if (continueButton) continueButton.onClick.AddListener(OnContinueButtonClicked);
        if (menuButton) menuButton.onClick.AddListener(OnHomeButtonClicked);
        
        // Setup audio controls
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        effectsToggle.onValueChanged.AddListener(OnEffectsToggleChanged);
        effectsVolumeSlider.onValueChanged.AddListener(OnEffectsVolumeChanged);
        
        // Setup difficulty toggles
        veryEasyToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnDifficultyChanged(Difficulty.VeryEasy); });
        easyToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnDifficultyChanged(Difficulty.Easy); });
        mediumToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnDifficultyChanged(Difficulty.Medium); });
        hardToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnDifficultyChanged(Difficulty.Hard); });
        veryHardToggle.onValueChanged.AddListener((isOn) => { if (isOn) OnDifficultyChanged(Difficulty.VeryHard); });
        
        difficultyToggleMap = new Dictionary<Difficulty, Toggle>
        {
            { Difficulty.VeryEasy, veryEasyToggle },
            { Difficulty.Easy, easyToggle },
            { Difficulty.Medium, mediumToggle },
            { Difficulty.Hard, hardToggle },
            { Difficulty.VeryHard, veryHardToggle }
        };

        // Set default difficulty
        mediumToggle.isOn = true;
        
        // Initialize audio settings
        InitializeAudioSettings();
        
        // Initialize UI state
        ShowMainMenu();
    }
    
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        levelCompletePanel?.SetActive(false);
    }
    
    public void HideMainMenu() => mainMenuPanel.SetActive(false);

    public void ShowGameplayUI()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        levelCompletePanel?.SetActive(false);
        
        if (gameplayPanel) gameplayPanel.SetActive(true);
        
        SubscribeToGameEvents();
        
        InitializeGameplayUI();
    }
    
    void InitializeGameplayUI()
    {
        if (difficultyText) difficultyText.text = $"Difficulty: {selectedDifficulty}";
        
        UpdateScoreUI(0);
        UpdateTurnsUI(0);
        
        Debug.Log($"Gameplay UI initialized - Difficulty: {selectedDifficulty}");
    }

    public void ShowDifficultyCompleteUI()
    {
        if (!levelCompletePanel) return;
        levelCompletePanel.SetActive(true);
            
        if (!levelCompleteText) return;
        levelCompleteText.text = $"{selectedDifficulty} Complete!";
    }

    public void ShowGameOverUI()
    {
        // For now, treat game over same as difficulty complete
        ShowDifficultyCompleteUI();
    }

    public void ShowPauseUI()
    {
        // Implementation for pause UI if needed
        Debug.Log("Game paused");
    }
    
    void OnPlayButtonClicked()
    {
        Debug.Log($"Starting game with difficulty: {selectedDifficulty}");
        GameManager.Instance.StartGameWithDifficulty(selectedDifficulty);
    }
    
    void OnSettingsButtonClicked() => settingsPanel.SetActive(true);

    void OnCloseSettingsClicked() => settingsPanel.SetActive(false);

    void OnDifficultyChanged(Difficulty difficulty)
    {
        selectedDifficulty = difficulty;
        Debug.Log($"Difficulty changed to: {difficulty}");
    }
    
    public Difficulty GetSelectedDifficulty() => selectedDifficulty;

    void InitializeAudioSettings()
    {
        // Load saved settings from PlayerPrefs
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        effectsEnabled = PlayerPrefs.GetInt("EffectsEnabled", 1) == 1;
        effectsVolume = PlayerPrefs.GetFloat("EffectsVolume", 0.8f);
        
        // Update UI elements to reflect loaded settings
        musicToggle.isOn = musicEnabled;
        musicVolumeSlider.value = musicVolume;
        effectsToggle.isOn = effectsEnabled;
        effectsVolumeSlider.value = effectsVolume;
        
        Debug.Log($"Audio settings loaded: Music={musicEnabled}({musicVolume:F2}), Effects={effectsEnabled}({effectsVolume:F2})");
        
        // Apply initial settings to AudioManager
        UpdateMusicSettings();
        UpdateEffectsSettings();
    }
    
    void OnMusicToggleChanged(bool isEnabled)
    {
        musicEnabled = isEnabled;
        UpdateMusicSettings();
    }
    
    void OnMusicVolumeChanged(float volume)
    {
        musicVolume = volume;
        UpdateMusicSettings();
    }
    
    void OnEffectsToggleChanged(bool isEnabled)
    {
        effectsEnabled = isEnabled;
        UpdateEffectsSettings();
    }
    
    void OnEffectsVolumeChanged(float volume)
    {
        effectsVolume = volume;
        UpdateEffectsSettings();
    }
    
    void UpdateMusicSettings()
    {
        float finalVolume = musicEnabled ? musicVolume : 0f;
        
        // Apply to AudioManager if available
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(musicEnabled);
            AudioManager.Instance.SetMusicVolume(musicVolume);
            Debug.Log($"Music settings applied to AudioManager: Enabled={musicEnabled}, Volume={finalVolume:F2}");
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance is null - music settings not applied");
        }
        
        // Save settings to PlayerPrefs
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }
    
    void UpdateEffectsSettings()
    {
        float finalVolume = effectsEnabled ? effectsVolume : 0f;
        
        // Apply to AudioManager if available
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetEffectsEnabled(effectsEnabled);
            AudioManager.Instance.SetEffectsVolume(effectsVolume);
            Debug.Log($"Effects settings applied to AudioManager: Enabled={effectsEnabled}, Volume={finalVolume:F2}");
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance is null - effects settings not applied");
        }
        
        // Save settings to PlayerPrefs
        PlayerPrefs.SetInt("EffectsEnabled", effectsEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("EffectsVolume", effectsVolume);
    }
    
    // Gameplay button handlers
    void OnHomeButtonClicked()
    {
        UnsubscribeFromGameEvents();
        GameManager.Instance.ReturnToMainMenu();
    }

    void OnRestartButtonClicked()
    {
        if (GameManager.Instance.MemoryGameManager) GameManager.Instance.MemoryGameManager.RestartGame();
    }

    void OnContinueButtonClicked()
    {
        // Progress to next difficulty
        Difficulty nextDifficulty = GetNextDifficulty(selectedDifficulty);
        
        if (nextDifficulty != selectedDifficulty)
        {
            // Move to next difficulty
            selectedDifficulty = nextDifficulty;
            UpdateDifficultyToggle();
            levelCompletePanel?.SetActive(false);
            GameManager.Instance.StartGameWithDifficulty(selectedDifficulty);
        }
        else
        {
            // All difficulties complete - show play again option
            if (levelCompleteText) levelCompleteText.text = "All Difficulties Complete!";
            
            if (continueButton) continueButton.gameObject.SetActive(false);
            
            // Add Play Again button functionality (reuse continue button)
            var buttonText = continueButton?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText) buttonText.text = "Play Again";
        }
    }

    Difficulty GetNextDifficulty(Difficulty current)
    {
        return current switch
        {
            Difficulty.VeryEasy => Difficulty.Easy,
            Difficulty.Easy => Difficulty.Medium,
            Difficulty.Medium => Difficulty.Hard,
            Difficulty.Hard => Difficulty.VeryHard,
            Difficulty.VeryHard => Difficulty.VeryHard, // Stay at max
            _ => Difficulty.VeryEasy
        };
    }

    void UpdateDifficultyToggle()
    {
        foreach (var _toggle in difficultyToggleMap.Values) _toggle.isOn = false;
        if (difficultyToggleMap.TryGetValue(selectedDifficulty, out Toggle toggle)) toggle.isOn = true;
    }

    // Game event subscription
    void SubscribeToGameEvents()
    {
        var gameManager = GameManager.Instance?.MemoryGameManager;
        if (gameManager != null)
        {
            gameManager.OnScoreChanged += UpdateScoreUI;
            gameManager.OnTurnsChanged += UpdateTurnsUI;
            gameManager.OnGameWon += OnGameWon;
            gameManager.OnGameStarted += OnGameStarted;
            gameManager.OnPreviewStarted += OnPreviewStarted;
            gameManager.OnPreviewEnded += OnPreviewEnded;
        }
    }

    void UnsubscribeFromGameEvents()
    {
        var gameManager = GameManager.Instance?.MemoryGameManager;
        if (gameManager != null)
        {
            gameManager.OnScoreChanged -= UpdateScoreUI;
            gameManager.OnTurnsChanged -= UpdateTurnsUI;
            gameManager.OnGameWon -= OnGameWon;
            gameManager.OnGameStarted -= OnGameStarted;
            gameManager.OnPreviewStarted -= OnPreviewStarted;
            gameManager.OnPreviewEnded -= OnPreviewEnded;
        }
    }

    // Game event handlers
    void UpdateScoreUI(int score) => scoreText.text = $"Score: {score}";

    void UpdateTurnsUI(int turns) => turnsText.text = $"Turns: {turns}";

    void OnGameWon() => GameManager.Instance.CompleteDifficulty();

    void OnGameStarted()
    {
        if (difficultyText) difficultyText.text = $"Difficulty: {selectedDifficulty}";
        UpdateScoreUI(0);
        UpdateTurnsUI(0);
    }

    void OnPreviewStarted()
    {
        if (difficultyText) difficultyText.text = $"Memorize the cards...";
    }

    void OnPreviewEnded()
    {
        // Restore normal difficulty display
        if (difficultyText) difficultyText.text = $"Difficulty: {selectedDifficulty}";
    }

    // Public methods to get current audio settings
    public bool IsMusicEnabled() => musicEnabled;
    public float GetMusicVolume() => musicVolume;
    public bool AreEffectsEnabled() => effectsEnabled;
    public float GetEffectsVolume() => effectsVolume;
    void OnDestroy() => UnsubscribeFromGameEvents();
}