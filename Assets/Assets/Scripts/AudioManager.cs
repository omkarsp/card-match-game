using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource effectsSource;
    [SerializeField] int audioSourcePoolSize = 5;

    [Header("Sound Effects - Required")]
    [SerializeField] AudioClip cardFlipSound;
    [SerializeField] AudioClip matchSound;
    [SerializeField] AudioClip mismatchSound;
    [SerializeField] AudioClip gameOverSound;

    [Header("Volume Settings")]
    [SerializeField] float masterVolume = 1f;
    [SerializeField] float effectsVolume = 0.8f;
    [SerializeField] float musicVolume = 0.6f;

    [Header("Audio Settings")]
    [SerializeField] bool effectsEnabled = true;
    [SerializeField] bool musicEnabled = true;

    // Audio Source Pool for overlapping sounds
    Queue<AudioSource> audioSourcePool;
    List<AudioSource> activeAudioSources;

    // Singleton pattern
    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        // Singleton setup
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Load audio settings from UIManager if available
        LoadAudioSettings();
    }

    void InitializeAudioSystem()
    {
        // Initialize audio source pool
        audioSourcePool = new Queue<AudioSource>();
        activeAudioSources = new List<AudioSource>();

        // Create pooled audio sources
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioSourceObj = new GameObject($"PooledAudioSource_{i}");
            audioSourceObj.transform.SetParent(transform);
            
            AudioSource audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = effectsVolume;
            
            audioSourcePool.Enqueue(audioSource);
        }

        // Setup main effects source if not assigned
        if (!effectsSource)
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
        }

        // Setup music source if not assigned
        if (!musicSource)
        {
            GameObject musicSourceObj = new GameObject("MusicSource");
            musicSourceObj.transform.SetParent(transform);
            musicSource = musicSourceObj.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        // Apply initial settings
        UpdateAudioSettings();

        Debug.Log("Audio system initialized with pooled audio sources");
    }

    void LoadAudioSettings()
    {
        // First load from PlayerPrefs (fallback values)
        effectsEnabled = PlayerPrefs.GetInt("EffectsEnabled", 1) == 1;
        effectsVolume = PlayerPrefs.GetFloat("EffectsVolume", 0.8f);
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        
        Debug.Log($"Audio settings loaded from PlayerPrefs: Effects={effectsEnabled}({effectsVolume:F2}), Music={musicEnabled}({musicVolume:F2})");
        
        // Then try to get current settings from UIManager if available
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager)
        {
            effectsEnabled = uiManager.AreEffectsEnabled();
            effectsVolume = uiManager.GetEffectsVolume();
            musicEnabled = uiManager.IsMusicEnabled();
            musicVolume = uiManager.GetMusicVolume();
            
            Debug.Log($"Audio settings updated from UIManager: Effects={effectsEnabled}({effectsVolume:F2}), Music={musicEnabled}({musicVolume:F2})");
        }
        
        UpdateAudioSettings();
    }

    #region Required Sound Effects

    public void PlayCardFlip() => PlaySoundEffect(cardFlipSound);

    public void PlayMatch() => PlaySoundEffect(matchSound);

    public void PlayMismatch() => PlaySoundEffect(mismatchSound);

    public void PlayGameOver() => PlaySoundEffect(gameOverSound);

    #endregion

    #region Sound Effect System

    void PlaySoundEffect(AudioClip clip)
    {
        if (!effectsEnabled || !clip) return;

        // Try to get pooled audio source
        AudioSource source = GetPooledAudioSource();
        
        if (source)
        {
            source.clip = clip;
            source.volume = effectsVolume * masterVolume;
            source.pitch = 1f; // Reset pitch
            source.Play();
            
            // Start coroutine to return source to pool when finished
            StartCoroutine(ReturnAudioSourceToPool(source, clip.length));
        }
        else
        {
            // Fallback to main effects source
            effectsSource.PlayOneShot(clip, effectsVolume * masterVolume);
        }
    }

    AudioSource GetPooledAudioSource()
    {
        if (audioSourcePool.Count <= 0) return null;
        AudioSource source = audioSourcePool.Dequeue();
        activeAudioSources.Add(source);
        return source;
    }

    IEnumerator ReturnAudioSourceToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!source) yield break;
        
        source.Stop();
        source.clip = null;
            
        activeAudioSources.Remove(source);
        audioSourcePool.Enqueue(source);
    }

    #endregion

    #region Audio Settings

    public void UpdateAudioSettings()
    {
        // Update effects volume for all sources
        effectsSource.volume = effectsVolume * masterVolume;
        
        foreach (AudioSource source in activeAudioSources)
        {
            if (source) source.volume = effectsVolume * masterVolume;
        }

        // Update music
        if (musicSource)
        {
            musicSource.volume = musicVolume * masterVolume;
            musicSource.mute = !musicEnabled;
        }

        Debug.Log($"Audio settings updated - Effects: {effectsEnabled}({effectsVolume}), Music: {musicEnabled}({musicVolume})");
    }

    public void SetEffectsEnabled(bool enabled)
    {
        effectsEnabled = enabled;
        UpdateAudioSettings();
    }

    public void SetEffectsVolume(float volume)
    {
        effectsVolume = Mathf.Clamp01(volume);
        UpdateAudioSettings();
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        UpdateAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAudioSettings();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAudioSettings();
    }

    #endregion

    #region Music System (Optional)

    public void PlayBackgroundMusic(AudioClip musicClip)
    {
        if (musicSource && musicClip)
        {
            musicSource.clip = musicClip;
            if (musicEnabled) musicSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        if (musicSource) musicSource.Stop();
    }

    public void PauseBackgroundMusic()
    {
        if (musicSource) musicSource.Pause();
    }

    public void ResumeBackgroundMusic()
    {
        if (musicSource && musicEnabled) musicSource.UnPause();
    }

    #endregion

    #region Public Getters

    public bool AreEffectsEnabled() => effectsEnabled;
    public float GetEffectsVolume() => effectsVolume;
    public bool IsMusicEnabled() => musicEnabled;
    public float GetMusicVolume() => musicVolume;
    public float GetMasterVolume() => masterVolume;

    #endregion

    #region Validation and Debugging

    void OnValidate()
    {
        // Clamp values in inspector
        masterVolume = Mathf.Clamp01(masterVolume);
        effectsVolume = Mathf.Clamp01(effectsVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        
        if (audioSourcePoolSize < 1)
            audioSourcePoolSize = 1;
    }

    void TestAllSounds()
    {
        Debug.Log("Testing all sound effects...");
        
        StartCoroutine(TestSoundsSequence());
    }

    IEnumerator TestSoundsSequence()
    {
        Debug.Log("Playing card flip sound");
        PlayCardFlip();
        yield return new WaitForSeconds(1f);
        
        Debug.Log("Playing match sound");
        PlayMatch();
        yield return new WaitForSeconds(1f);
        
        Debug.Log("Playing mismatch sound");
        PlayMismatch();
        yield return new WaitForSeconds(1f);
        
        Debug.Log("Playing game over sound");
        PlayGameOver();
        
        Debug.Log("Sound test complete");
    }

    public void LogAudioStatus()
    {
        Debug.Log("=== AUDIO STATUS ===");
        Debug.Log($"Effects Enabled: {effectsEnabled} (Volume: {effectsVolume})");
        Debug.Log($"Music Enabled: {musicEnabled} (Volume: {musicVolume})");
        Debug.Log($"Master Volume: {masterVolume}");
        Debug.Log($"Active Audio Sources: {activeAudioSources.Count}");
        Debug.Log($"Pooled Audio Sources: {audioSourcePool.Count}");
        Debug.Log($"Main Effects Source Volume: {effectsSource?.volume ?? 0f}");
        
        // Check for missing clips
        if (cardFlipSound == null) Debug.LogWarning("Card flip sound not assigned!");
        if (matchSound == null) Debug.LogWarning("Match sound not assigned!");
        if (mismatchSound == null) Debug.LogWarning("Mismatch sound not assigned!");
        if (gameOverSound == null) Debug.LogWarning("Game over sound not assigned!");
    }
    
    public void TestEffectsVolume()
    {
        Debug.Log($"Testing effects at volume {effectsVolume * masterVolume:F2}");
        PlayCardFlip();
    }

    #endregion

    void OnDestroy()
    {
        // Clean up any running coroutines
        StopAllCoroutines();
    }
}