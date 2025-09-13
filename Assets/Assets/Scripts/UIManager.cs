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

    [SerializeField] private Sprite difficultyOn;
    [SerializeField] private Sprite difficultyOff;

    Dictionary<Difficulty, string> difficultyTextMap = new();
    Dictionary<Difficulty, Toggle> difficultyToggleMap = new();
    Dictionary<Difficulty, Difficulty> nextDifficultyMap = new();
    Difficulty selectedDifficulty = Difficulty.Medium;

    
    [Header("Settings")]
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Button closeSettingsButton;
    
    [Header("Audio Settings")]
    [SerializeField] Toggle musicToggle;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Toggle effectsToggle;
    [SerializeField] Slider effectsVolumeSlider;
    [SerializeField] Sprite musicOn;
    [SerializeField] Sprite musicOff;
    [SerializeField] Sprite effectsOn;
    [SerializeField] Sprite effectsOff;
    
    
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

        difficultyTextMap = new()
        {
            { Difficulty.VeryEasy, "VERY EASY"},
            { Difficulty.Easy, "EASY" },
            { Difficulty.Medium, "MEDIUM" },
            { Difficulty.Hard, "HARD" },
            { Difficulty.VeryHard, "VERY HARD" }
        };
        
        // Setup difficulty toggles
        difficultyToggleMap = new Dictionary<Difficulty, Toggle>
        {
            { Difficulty.VeryEasy, veryEasyToggle },
            { Difficulty.Easy, easyToggle },
            { Difficulty.Medium, mediumToggle },
            { Difficulty.Hard, hardToggle },
            { Difficulty.VeryHard, veryHardToggle }
        };

        nextDifficultyMap = new()
        {
            { Difficulty.VeryEasy, Difficulty.Easy },
            { Difficulty.Easy, Difficulty.Medium },
            { Difficulty.Medium, Difficulty.Hard },
            { Difficulty.Hard, Difficulty.VeryHard },
            { Difficulty.VeryHard, Difficulty.VeryHard }
        };
        
        veryEasyToggle.onValueChanged.AddListener((isOn) => { OnDifficultyChanged(Difficulty.VeryEasy, isOn); });
        easyToggle.onValueChanged.AddListener((isOn) => { OnDifficultyChanged(Difficulty.Easy, isOn); });
        mediumToggle.onValueChanged.AddListener((isOn) => { OnDifficultyChanged(Difficulty.Medium, isOn); });
        hardToggle.onValueChanged.AddListener((isOn) => { OnDifficultyChanged(Difficulty.Hard, isOn); });
        veryHardToggle.onValueChanged.AddListener((isOn) => { OnDifficultyChanged(Difficulty.VeryHard, isOn); });
        
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
        gameplayPanel?.SetActive(false);
        settingsPanel.SetActive(false);
        levelCompletePanel?.SetActive(false);
        
        difficultyToggleMap[selectedDifficulty].isOn = true;
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
        if (difficultyText) difficultyText.text = $"Difficulty: {difficultyTextMap[selectedDifficulty]}";
        
        UpdateScoreUI(0);
        UpdateTurnsUI(0);
        
        Debug.Log($"Gameplay UI initialized - Difficulty: {difficultyTextMap[selectedDifficulty]}");
    }

    public void ShowDifficultyCompleteUI()
    {
        if (!levelCompletePanel) return;
        levelCompletePanel.SetActive(true);
            
        if (!levelCompleteText) return;
        levelCompleteText.text = $"{selectedDifficulty} Complete!";
    }

    public void ShowGameOverUI() => ShowDifficultyCompleteUI();

    void OnPlayButtonClicked()
    {
        Debug.Log($"Starting game with difficulty: {selectedDifficulty}");
        GameManager.Instance.StartGameWithDifficulty(selectedDifficulty);
    }
    
    void OnSettingsButtonClicked() => settingsPanel.SetActive(true);

    void OnCloseSettingsClicked() => settingsPanel.SetActive(false);

    void OnDifficultyChanged(Difficulty difficulty, bool isOn)
    {
        selectedDifficulty = difficulty;
        Debug.Log(isOn ? $"Difficulty changed to: {difficulty}" : $"Difficulty changed from: {difficulty}");
        (difficultyToggleMap[difficulty].targetGraphic as Image).sprite = isOn ? difficultyOn : difficultyOff;
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
        (musicToggle.targetGraphic as Image).sprite = isEnabled ? musicOn : musicOff;
    }
    
    void OnMusicVolumeChanged(float volume)
    {
        musicVolume = volume;
        UpdateMusicSettings();
        (musicToggle.targetGraphic as Image).sprite = volume > 0 ? musicOn : musicOff;
    }
    
    void OnEffectsToggleChanged(bool isEnabled)
    {
        effectsEnabled = isEnabled;
        UpdateEffectsSettings();
        (effectsToggle.targetGraphic as Image).sprite = isEnabled ? effectsOn :  effectsOff;
    }
    
    void OnEffectsVolumeChanged(float volume)
    {
        effectsVolume = volume;
        UpdateEffectsSettings();
        (effectsToggle.targetGraphic as Image).sprite = volume > 0 ? effectsOn : effectsOff;
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
        Difficulty nextDifficulty = nextDifficultyMap[selectedDifficulty];
        
        if (nextDifficulty != selectedDifficulty)
        {
            // Move to next difficulty
            selectedDifficulty = nextDifficulty;
            levelCompletePanel?.SetActive(false);
            
            // Show gameplay UI and start new game
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
    
    void UpdateDifficultyText(Difficulty difficulty) => difficultyText.text = $"Difficulty: {difficultyTextMap[difficulty]}";

    void OnGameWon() => GameManager.Instance.CompleteDifficulty();

    void OnGameStarted()
    {
        if (difficultyText) difficultyText.text = $"Difficulty: {difficultyTextMap[selectedDifficulty]}";
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
        if (difficultyText) difficultyText.text = $"Difficulty: {difficultyTextMap[selectedDifficulty]}";
    }

    // Public methods to get current audio settings
    public bool IsMusicEnabled() => musicEnabled;
    public float GetMusicVolume() => musicVolume;
    public bool AreEffectsEnabled() => effectsEnabled;
    public float GetEffectsVolume() => effectsVolume;
    void OnDestroy() => UnsubscribeFromGameEvents();
}