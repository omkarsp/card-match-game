using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class PerformanceOptimizer : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] bool enableOptimizations = true;
    [SerializeField] bool enablePerformanceMonitoring = false;
    [SerializeField] float targetFrameRate = 60f;
    [SerializeField] int maxCardPoolSize = 100;

    [Header("Memory Management")]
    [SerializeField] bool enableAutomaticGC = false;
    [SerializeField] float gcInterval = 30f;

    [Header("Visual Optimizations")]
    [SerializeField] bool enableObjectCulling = true;
    [SerializeField] bool enableLODSystem = false;
    [SerializeField] float cullDistance = 50f;

    // Performance monitoring
    float lastFrameTime;
    float averageFrameTime;
    int frameCount;
    float frameTimeAccumulator;

    // Object pooling
    Dictionary<string, Queue<GameObject>> objectPools;
    Dictionary<string, GameObject> poolPrefabs;

    // Memory management
    float lastGCTime;

    // Singleton
    public static PerformanceOptimizer Instance { get; private set; }

    // Performance stats
    public float CurrentFPS { get; private set; }
    public float AverageFrameTime => averageFrameTime;
    public long MemoryUsage { get; private set; }

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeOptimizations();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Set target frame rate
        if (enableOptimizations)
        {
            Application.targetFrameRate = Mathf.RoundToInt(targetFrameRate);
            QualitySettings.vSyncCount = 0; // Disable VSync for consistent frame rate
        }
    }

    void Update()
    {
        if (enablePerformanceMonitoring) UpdatePerformanceMetrics();

        if (enableAutomaticGC) HandleAutomaticGC();
    }

    void InitializeOptimizations()
    {
        // Initialize object pooling
        objectPools = new Dictionary<string, Queue<GameObject>>();
        poolPrefabs = new Dictionary<string, GameObject>();

        // Setup performance optimizations
        if (enableOptimizations)
        {
            // Optimize physics
            Physics.autoSimulation = true;
            Physics2D.autoSimulation = true;

            // Optimize rendering
            if (enableObjectCulling) SetupObjectCulling();

            Debug.Log("Performance optimizations initialized");
        }
    }

    #region Object Pooling

    public void RegisterPoolPrefab(string poolName, GameObject prefab, int initialSize = 10)
    {
        if (poolPrefabs.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool {poolName} already registered");
            return;
        }

        poolPrefabs[poolName] = prefab;
        objectPools[poolName] = new Queue<GameObject>();

        // Pre-populate pool
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            objectPools[poolName].Enqueue(obj);
        }

        Debug.Log($"Registered pool '{poolName}' with {initialSize} objects");
    }

    public GameObject GetPooledObject(string poolName)
    {
        if (!objectPools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' not found");
            return null;
        }

        Queue<GameObject> pool = objectPools[poolName];

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // Pool exhausted, create new object if we have the prefab
        if (poolPrefabs.ContainsKey(poolName))
        {
            GameObject newObj = Instantiate(poolPrefabs[poolName]);
            Debug.LogWarning($"Pool '{poolName}' exhausted, created new object");
            return newObj;
        }

        Debug.LogError($"Pool '{poolName}' exhausted and no prefab available");
        return null;
    }

    public void ReturnToPool(string poolName, GameObject obj)
    {
        if (!objectPools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' not found");
            Destroy(obj);
            return;
        }

        // Reset object state
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        // Check pool size limit
        if (objectPools[poolName].Count < maxCardPoolSize)
        {
            objectPools[poolName].Enqueue(obj);
        }
        else
        {
            // Pool is full, destroy the object
            Destroy(obj);
        }
    }

    public void ClearPool(string poolName)
    {
        if (!objectPools.ContainsKey(poolName))
            return;

        Queue<GameObject> pool = objectPools[poolName];
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        Debug.Log($"Cleared pool '{poolName}'");
    }

    public void ClearAllPools()
    {
        foreach (var poolName in objectPools.Keys)
        {
            ClearPool(poolName);
        }
    }

    #endregion

    #region Performance Monitoring

    void UpdatePerformanceMetrics()
    {
        // Calculate FPS
        lastFrameTime = Time.unscaledDeltaTime;
        CurrentFPS = 1f / lastFrameTime;

        // Calculate average frame time
        frameTimeAccumulator += lastFrameTime;
        frameCount++;

        if (frameCount >= 60) // Update average every 60 frames
        {
            averageFrameTime = frameTimeAccumulator / frameCount;
            frameTimeAccumulator = 0f;
            frameCount = 0;
        }

        // Monitor memory usage
        MemoryUsage = Profiler.GetTotalAllocatedMemory();
    }

    public void LogPerformanceStats()
    {
        Debug.Log("=== PERFORMANCE STATS ===");
        Debug.Log($"Current FPS: {CurrentFPS:F1}");
        Debug.Log($"Average Frame Time: {averageFrameTime * 1000f:F2}ms");
        Debug.Log($"Memory Usage: {MemoryUsage / (1024 * 1024):F2} MB");
        Debug.Log($"Active Pools: {objectPools.Count}");
        
        foreach (var pool in objectPools)
        {
            Debug.Log($"  {pool.Key}: {pool.Value.Count} objects");
        }
    }

    #endregion

    #region Memory Management

    void HandleAutomaticGC()
    {
        if (Time.time - lastGCTime >= gcInterval)
        {
            System.GC.Collect();
            lastGCTime = Time.time;
            Debug.Log("Automatic garbage collection performed");
        }
    }

    public void ForceGarbageCollection()
    {
        System.GC.Collect();
        Debug.Log("Manual garbage collection performed");
    }

    #endregion

    #region Visual Optimizations

    void SetupObjectCulling()
    {
        // Enable occlusion culling if available
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.useOcclusionCulling = true;
        }
    }

    public void SetObjectCullingDistance(float distance)
    {
        cullDistance = distance;
        
        // Apply to all cameras
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.farClipPlane = cullDistance;
        }
    }

    #endregion

    #region Platform Optimizations

    public void OptimizeForMobile()
    {
        // Reduce quality settings for mobile
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.particleRaycastBudget = 16;
        QualitySettings.maximumLODLevel = 1;
        
        // Reduce target frame rate for battery life
        Application.targetFrameRate = 30;
        
        Debug.Log("Mobile optimizations applied");
    }

    public void OptimizeForDesktop()
    {
        // Higher quality settings for desktop
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.particleRaycastBudget = 256;
        QualitySettings.maximumLODLevel = 0;
        
        // Higher frame rate for desktop
        Application.targetFrameRate = 60;
        
        Debug.Log("Desktop optimizations applied");
    }

    #endregion

    #region Public Configuration

    public void SetPerformanceMonitoring(bool enabled)
    {
        enablePerformanceMonitoring = enabled;
    }

    public void SetTargetFrameRate(float fps)
    {
        targetFrameRate = fps;
        Application.targetFrameRate = Mathf.RoundToInt(fps);
    }

    public void SetAutomaticGC(bool enabled, float interval = 30f)
    {
        enableAutomaticGC = enabled;
        gcInterval = interval;
    }

    public bool IsOptimizationEnabled()
    {
        return enableOptimizations;
    }

    public int GetPoolSize(string poolName)
    {
        return objectPools.ContainsKey(poolName) ? objectPools[poolName].Count : 0;
    }

    #endregion

    #region Unity Events

    void OnApplicationPause(bool pauseStatus)
    {
        // Force GC when app is paused
        if (pauseStatus && enableOptimizations) ForceGarbageCollection();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Reduce performance when not focused
        if (!hasFocus && enableOptimizations) Application.targetFrameRate = 15;
        // Restore normal performance
        else if (hasFocus) Application.targetFrameRate = Mathf.RoundToInt(targetFrameRate);
    }

    void OnDestroy() => ClearAllPools();

    void OnValidate()
    {
        // Clamp values in inspector
        if (targetFrameRate < 15f) targetFrameRate = 15f;
        if (maxCardPoolSize < 10) maxCardPoolSize = 10;
        if (gcInterval < 5f) gcInterval = 5f;
        if (cullDistance < 10f) cullDistance = 10f;
    }

    #endregion
}