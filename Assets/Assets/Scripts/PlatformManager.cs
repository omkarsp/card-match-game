using UnityEngine;

public enum Platform
{
    Desktop,
    Mobile,
    WebGL,
    Unknown
}

public class PlatformManager : MonoBehaviour
{
    [Header("Platform Detection")]
    [SerializeField] private bool autoDetectPlatform = true;
    [SerializeField] private Platform forcePlatform = Platform.Unknown;

    [Header("Input Settings")]
    [SerializeField] private bool enableTouchInput = true;
    [SerializeField] private bool enableMouseInput = true;
    [SerializeField] private float touchSensitivity = 1f;

    [Header("Platform Specific Settings")]
    [SerializeField] private bool enableMobileOptimizations = true;
    [SerializeField] private bool enableDesktopFeatures = true;

    // Current platform
    public Platform CurrentPlatform { get; private set; }
    
    // Input handling
    public bool IsTouchSupported { get; private set; }
    public bool IsMouseSupported { get; private set; }

    // Singleton
    public static PlatformManager Instance { get; private set; }

    // Events
    public System.Action<Platform> OnPlatformChanged;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlatform();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ApplyPlatformOptimizations();
    }

    private void InitializePlatform()
    {
        // Detect current platform
        if (autoDetectPlatform && forcePlatform == Platform.Unknown)
        {
            DetectPlatform();
        }
        else if (forcePlatform != Platform.Unknown)
        {
            CurrentPlatform = forcePlatform;
        }
        else
        {
            DetectPlatform();
        }

        // Setup input capabilities
        SetupInputCapabilities();

        Debug.Log($"Platform initialized: {CurrentPlatform}");
        Debug.Log($"Touch supported: {IsTouchSupported}, Mouse supported: {IsMouseSupported}");
    }

    private void DetectPlatform()
    {
        RuntimePlatform runtimePlatform = Application.platform;

        switch (runtimePlatform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
                CurrentPlatform = Platform.Desktop;
                break;

            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                CurrentPlatform = Platform.Mobile;
                break;

            case RuntimePlatform.WebGLPlayer:
                CurrentPlatform = Platform.WebGL;
                break;

            default:
                CurrentPlatform = Platform.Unknown;
                Debug.LogWarning($"Unknown platform: {runtimePlatform}");
                break;
        }
    }

    private void SetupInputCapabilities()
    {
        // Determine input capabilities based on platform
        switch (CurrentPlatform)
        {
            case Platform.Desktop:
                IsTouchSupported = Input.touchSupported && enableTouchInput;
                IsMouseSupported = enableMouseInput;
                break;

            case Platform.Mobile:
                IsTouchSupported = enableTouchInput;
                IsMouseSupported = false; // Mobile doesn't typically have mouse
                break;

            case Platform.WebGL:
                IsTouchSupported = Input.touchSupported && enableTouchInput;
                IsMouseSupported = enableMouseInput;
                break;

            default:
                IsTouchSupported = Input.touchSupported && enableTouchInput;
                IsMouseSupported = enableMouseInput;
                break;
        }
    }

    private void ApplyPlatformOptimizations()
    {
        PerformanceOptimizer optimizer = PerformanceOptimizer.Instance;

        switch (CurrentPlatform)
        {
            case Platform.Mobile:
                if (enableMobileOptimizations)
                {
                    ApplyMobileOptimizations(optimizer);
                }
                break;

            case Platform.Desktop:
                if (enableDesktopFeatures)
                {
                    ApplyDesktopOptimizations(optimizer);
                }
                break;

            case Platform.WebGL:
                ApplyWebGLOptimizations(optimizer);
                break;
        }
    }

    private void ApplyMobileOptimizations(PerformanceOptimizer optimizer)
    {
        // Mobile-specific optimizations
        if (optimizer != null)
        {
            optimizer.OptimizeForMobile();
        }

        // Reduce screen resolution for better performance
        Screen.SetResolution(Screen.width / 2, Screen.height / 2, true);

        // Disable some visual effects
        QualitySettings.particleRaycastBudget = 16;
        QualitySettings.shadowDistance = 20f;

        // Enable battery-saving features
        Screen.sleepTimeout = SleepTimeout.SystemSetting;

        Debug.Log("Mobile optimizations applied");
    }

    private void ApplyDesktopOptimizations(PerformanceOptimizer optimizer)
    {
        // Desktop-specific optimizations
        if (optimizer != null)
        {
            optimizer.OptimizeForDesktop();
        }

        // Higher quality settings for desktop
        QualitySettings.particleRaycastBudget = 256;
        QualitySettings.shadowDistance = 100f;

        // Disable screen timeout
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("Desktop optimizations applied");
    }

    private void ApplyWebGLOptimizations(PerformanceOptimizer optimizer)
    {
        // WebGL-specific optimizations
        if (optimizer != null)
        {
            optimizer.SetTargetFrameRate(30f); // Conservative frame rate for web
        }

        // Reduce quality for web performance
        QualitySettings.particleRaycastBudget = 32;
        QualitySettings.shadowDistance = 30f;

        Debug.Log("WebGL optimizations applied");
    }

    #region Input Handling

    public Vector2 GetInputPosition()
    {
        if (IsTouchSupported && Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        else if (IsMouseSupported)
        {
            return Input.mousePosition;
        }

        return Vector2.zero;
    }

    public bool GetInputDown()
    {
        if (IsTouchSupported && Input.touchCount > 0)
        {
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }
        else if (IsMouseSupported)
        {
            return Input.GetMouseButtonDown(0);
        }

        return false;
    }

    public bool GetInputUp()
    {
        if (IsTouchSupported && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        else if (IsMouseSupported)
        {
            return Input.GetMouseButtonUp(0);
        }

        return false;
    }

    public bool GetInput()
    {
        if (IsTouchSupported && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved;
        }
        else if (IsMouseSupported)
        {
            return Input.GetMouseButton(0);
        }

        return false;
    }

    #endregion

    #region Platform Specific Features

    public void ShowNativeMessage(string title, string message)
    {
        switch (CurrentPlatform)
        {
            case Platform.Mobile:
                // Could integrate with native mobile dialogs
                Debug.Log($"Mobile Message - {title}: {message}");
                break;

            case Platform.Desktop:
                Debug.Log($"Desktop Message - {title}: {message}");
                break;

            default:
                Debug.Log($"Message - {title}: {message}");
                break;
        }
    }

    public void RequestReview()
    {
        switch (CurrentPlatform)
        {
            case Platform.Mobile:
                // Could integrate with App Store review requests
                Debug.Log("Requesting app store review");
                break;

            default:
                Debug.Log("Review request not supported on this platform");
                break;
        }
    }

    public void ShareGame(string text)
    {
        switch (CurrentPlatform)
        {
            case Platform.Mobile:
                // Could integrate with native sharing
                Debug.Log($"Sharing: {text}");
                break;

            default:
                Debug.Log($"Share not supported: {text}");
                break;
        }
    }

    #endregion

    #region Screen and Resolution Management

    public void SetFullscreen(bool fullscreen)
    {
        if (CurrentPlatform == Platform.Desktop)
        {
            Screen.fullScreen = fullscreen;
        }
    }

    public void SetResolution(int width, int height)
    {
        if (CurrentPlatform == Platform.Desktop)
        {
            Screen.SetResolution(width, height, Screen.fullScreen);
        }
    }

    public Vector2 GetScreenSize()
    {
        return new Vector2(Screen.width, Screen.height);
    }

    public float GetScreenAspectRatio()
    {
        return (float)Screen.width / Screen.height;
    }

    public bool IsLandscape()
    {
        return Screen.width > Screen.height;
    }

    public bool IsPortrait()
    {
        return Screen.height > Screen.width;
    }

    #endregion

    #region Performance Monitoring

    public void LogPlatformInfo()
    {
        Debug.Log("=== PLATFORM INFO ===");
        Debug.Log($"Platform: {CurrentPlatform}");
        Debug.Log($"Runtime Platform: {Application.platform}");
        Debug.Log($"Device Model: {SystemInfo.deviceModel}");
        Debug.Log($"Device Type: {SystemInfo.deviceType}");
        Debug.Log($"Operating System: {SystemInfo.operatingSystem}");
        Debug.Log($"Processor: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
        Debug.Log($"Memory: {SystemInfo.systemMemorySize} MB");
        Debug.Log($"Graphics: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
        Debug.Log($"Screen: {Screen.width}x{Screen.height} @ {Screen.currentResolution.refreshRate}Hz");
        Debug.Log($"Touch Support: {IsTouchSupported}");
        Debug.Log($"Mouse Support: {IsMouseSupported}");
    }

    public bool IsLowEndDevice()
    {
        // Simple heuristic to detect low-end devices
        if (CurrentPlatform == Platform.Mobile)
        {
            return SystemInfo.systemMemorySize < 2048 || // Less than 2GB RAM
                   SystemInfo.processorCount < 4; // Less than 4 CPU cores
        }

        return false;
    }

    #endregion

    #region Configuration

    public void SetTouchSensitivity(float sensitivity)
    {
        touchSensitivity = Mathf.Clamp(sensitivity, 0.1f, 3f);
    }

    public void SetInputEnabled(bool touch, bool mouse)
    {
        enableTouchInput = touch;
        enableMouseInput = mouse;
        SetupInputCapabilities();
    }

    public void ForcePlatform(Platform platform)
    {
        Platform oldPlatform = CurrentPlatform;
        CurrentPlatform = platform;
        
        if (oldPlatform != CurrentPlatform)
        {
            SetupInputCapabilities();
            ApplyPlatformOptimizations();
            OnPlatformChanged?.Invoke(CurrentPlatform);
        }
    }

    #endregion

    private void OnValidate()
    {
        // Clamp values in inspector
        touchSensitivity = Mathf.Clamp(touchSensitivity, 0.1f, 3f);
    }

    #region Unity Events

    private void OnApplicationPause(bool pauseStatus)
    {
        if (CurrentPlatform == Platform.Mobile)
        {
            Debug.Log($"Application {(pauseStatus ? "paused" : "resumed")} on mobile");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"Application focus: {hasFocus}");
    }

    #endregion
}