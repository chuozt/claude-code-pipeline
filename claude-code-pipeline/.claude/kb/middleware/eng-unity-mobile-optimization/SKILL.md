---
name: eng-unity-mobile-optimization
description: Mobile-specific Unity optimization patterns for memory, battery, thermal, and performance.
---

# Unity Mobile Optimization

## Overview

Mobile platforms have unique constraints: limited memory, battery life, thermal throttling, and variable hardware. This skill covers mobile-specific optimization techniques.

## When to Use

- iOS and Android development
- Battery-sensitive applications
- Memory-constrained devices
- Thermal management
- Variable quality settings

## Memory Management

### Memory Budget System

```csharp
public class MobileMemoryManager : MonoBehaviour
{
    [SerializeField] private long warningThresholdMB = 800;
    [SerializeField] private long criticalThresholdMB = 1000;
    
    public event Action OnMemoryWarning;
    public event Action OnMemoryCritical;
    
    private void Awake()
    {
        Application.lowMemory += OnLowMemory;
    }
    
    private void OnDestroy()
    {
        Application.lowMemory -= OnLowMemory;
    }
    
    private void OnLowMemory()
    {
        GameDebug.LogWarning("Low memory warning received");
        OnMemoryCritical?.Invoke();
        
        // The OS lowMemory event is the one place a manual GC.Collect is accepted.
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }
    
    public MemoryStatus GetMemoryStatus()
    {
        long usedMB = Profiler.GetTotalAllocatedMemoryLong() / 1_000_000;
        
        if (usedMB >= criticalThresholdMB)
            return MemoryStatus.Critical;

        if (usedMB >= warningThresholdMB)
            return MemoryStatus.Warning;

        return MemoryStatus.Normal;
    }
    
    public void RequestCleanup()
    {
        StartCoroutine(CleanupRoutine());
    }
    
    private IEnumerator CleanupRoutine()
    {
        var operation = Resources.UnloadUnusedAssets();
        yield return operation;
        
        // Generation-by-generation keeps each pause short enough to hide in a loading screen.
        GC.Collect(0, GCCollectionMode.Optimized);
        yield return null;
        
        GC.Collect(1, GCCollectionMode.Optimized);
    }
}

public enum MemoryStatus { Normal, Warning, Critical }
```

### Texture Memory Control

```csharp
public class TextureMemoryController
{
    public static void SetQualityForDevice(DeviceTier tier)
    {
        switch (tier)
        {
            case DeviceTier.Low:
                QualitySettings.globalTextureMipmapLimit = 2; // 1/4 resolution
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                break;
                
            case DeviceTier.Medium:
                QualitySettings.globalTextureMipmapLimit = 1; // 1/2 resolution
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                break;
                
            case DeviceTier.High:
                QualitySettings.globalTextureMipmapLimit = 0; // Full resolution
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                break;
        }
    }
    
    public static void ForceReduceTextureMemory()
    {
        // Raising the mip limit alone does nothing until the old mips are actually unloaded.
        QualitySettings.globalTextureMipmapLimit++;
        Resources.UnloadUnusedAssets();
    }
}
```

## Battery Optimization

### Frame Rate Control

```csharp
public class BatteryOptimizer : MonoBehaviour
{
    [SerializeField] private int highPerformanceFps = 60;
    [SerializeField] private int balancedFps = 30;
    [SerializeField] private int batterySaverFps = 24;
    
    private PerformanceMode _currentMode = PerformanceMode.Balanced;
    
    public void SetPerformanceMode(PerformanceMode mode)
    {
        _currentMode = mode;
        
        switch (mode)
        {
            case PerformanceMode.HighPerformance:
                Application.targetFrameRate = highPerformanceFps;
                QualitySettings.vSyncCount = 0;
                break;
                
            case PerformanceMode.Balanced:
                Application.targetFrameRate = balancedFps;
                QualitySettings.vSyncCount = 1;
                break;
                
            case PerformanceMode.BatterySaver:
                Application.targetFrameRate = batterySaverFps;
                QualitySettings.vSyncCount = 2;
                Screen.brightness = 0.5f;
                break;
        }
    }
    
    public void SetAdaptiveFrameRate(bool isInCombat)
    {
        // Combat is where dropped frames are felt; menus and idle scenes can coast.
        if (isInCombat)
            Application.targetFrameRate = highPerformanceFps;
        else
            Application.targetFrameRate = balancedFps;
    }
}

public enum PerformanceMode { HighPerformance, Balanced, BatterySaver }
```

### Background Behavior

```csharp
public class BackgroundHandler : MonoBehaviour
{
    private bool _wasInBackground;
    
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            OnEnterBackground();
        else
            OnExitBackground();
        
        _wasInBackground = isPaused;
    }
    
    private void OnEnterBackground()
    {
        AudioListener.pause = true;
        Time.timeScale = 0;
        
        // The OS is more likely to kill a backgrounded app that holds a large heap.
        Resources.UnloadUnusedAssets();
    }
    
    private void OnExitBackground()
    {
        AudioListener.pause = false;
        Time.timeScale = 1;
        
        // The device may have heated up while another app was in front.
        CheckThermalState();
    }
}
```

## Thermal Management

### Thermal State Monitoring

```csharp
public class ThermalManager : MonoBehaviour
{
    private ThermalState _currentState = ThermalState.Normal;
    private float _checkInterval = 5f;
    
    public event Action<ThermalState> OnThermalStateChanged;
    
    private void Start()
    {
        InvokeRepeating(nameof(CheckThermalState), 0, _checkInterval);
    }
    
    private void CheckThermalState()
    {
#if UNITY_IOS
        var newState = GetIOSThermalState();
#elif UNITY_ANDROID
        var newState = GetAndroidThermalState();
#else
        var newState = ThermalState.Normal;
#endif
        
        if (newState != _currentState)
        {
            _currentState = newState;
            OnThermalStateChanged?.Invoke(newState);
            ApplyThermalMitigation(newState);
        }
    }
    
#if UNITY_IOS
    private ThermalState GetIOSThermalState()
    {
        // iOS provides thermal state via native plugin
        // ProcessInfo.ThermalState
        return ThermalState.Normal; // Implement native plugin
    }
#endif
    
#if UNITY_ANDROID
    private ThermalState GetAndroidThermalState()
    {
        // Android thermal API (API 29+)
        // PowerManager.getCurrentThermalStatus()
        return ThermalState.Normal; // Implement native plugin
    }
#endif
    
    private void ApplyThermalMitigation(ThermalState state)
    {
        switch (state)
        {
            case ThermalState.Normal:
                Application.targetFrameRate = 60;
                QualitySettings.SetQualityLevel(2);
                break;
                
            case ThermalState.Fair:
                Application.targetFrameRate = 45;
                break;
                
            case ThermalState.Serious:
                Application.targetFrameRate = 30;
                QualitySettings.SetQualityLevel(1);
                break;
                
            case ThermalState.Critical:
                Application.targetFrameRate = 24;
                QualitySettings.SetQualityLevel(0);
                ShowThermalWarning();
                break;
        }
    }
    
    private void ShowThermalWarning()
    {
        GameDebug.LogWarning("Device overheating - reducing performance");
        // Show UI warning to user
    }
}

public enum ThermalState { Normal, Fair, Serious, Critical }
```

## Device Tier Detection

### Hardware Classification

```csharp
public class DeviceClassifier
{
    public static DeviceTier ClassifyDevice()
    {
        int systemMemoryMB = SystemInfo.systemMemorySize;
        int processorCount = SystemInfo.processorCount;
        int graphicsMemoryMB = SystemInfo.graphicsMemorySize;
        
        int score = 0;
        
        if (systemMemoryMB >= 6000)
            score += 3;
        else if (systemMemoryMB >= 4000)
            score += 2;
        else if (systemMemoryMB >= 2000)
            score += 1;
        
        if (processorCount >= 8)
            score += 3;
        else if (processorCount >= 6)
            score += 2;
        else if (processorCount >= 4)
            score += 1;
        
        if (graphicsMemoryMB >= 4000)
            score += 3;
        else if (graphicsMemoryMB >= 2000)
            score += 2;
        else if (graphicsMemoryMB >= 1000)
            score += 1;
        
        if (score >= 7)
            return DeviceTier.High;

        if (score >= 4)
            return DeviceTier.Medium;

        return DeviceTier.Low;
    }
    
    public static void LogDeviceInfo()
    {
        GameDebug.Log($"Device: {SystemInfo.deviceModel}");
        GameDebug.Log($"OS: {SystemInfo.operatingSystem}");
        GameDebug.Log($"RAM: {SystemInfo.systemMemorySize}MB");
        GameDebug.Log($"CPU: {SystemInfo.processorType} x{SystemInfo.processorCount}");
        GameDebug.Log($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize}MB)");
        GameDebug.Log($"Tier: {ClassifyDevice()}");
    }
}

public enum DeviceTier { Low, Medium, High }
```

### Quality Presets

```csharp
public class QualityPresetManager
{
    public static void ApplyPresetForTier(DeviceTier tier)
    {
        var preset = GetPreset(tier);
        
        QualitySettings.SetQualityLevel(preset.QualityLevel);
        QualitySettings.shadows = preset.Shadows;
        QualitySettings.shadowResolution = preset.ShadowResolution;
        QualitySettings.antiAliasing = preset.AntiAliasing;
        QualitySettings.pixelLightCount = preset.PixelLights;
        QualitySettings.globalTextureMipmapLimit = preset.TextureMipLevel;
        
        Application.targetFrameRate = preset.TargetFps;
        Time.fixedDeltaTime = 1f / preset.PhysicsRate;
        
        GameDebug.Log($"Applied quality preset for {tier}: {preset.QualityLevel}");
    }
    
    private static QualityPreset GetPreset(DeviceTier tier)
    {
        return tier switch
        {
            DeviceTier.Low => new QualityPreset
            {
                QualityLevel = 0,
                Shadows = ShadowQuality.Disable,
                ShadowResolution = ShadowResolution.Low,
                AntiAliasing = 0,
                PixelLights = 1,
                TextureMipLevel = 2,
                TargetFps = 30,
                PhysicsRate = 30
            },
            DeviceTier.Medium => new QualityPreset
            {
                QualityLevel = 2,
                Shadows = ShadowQuality.HardOnly,
                ShadowResolution = ShadowResolution.Medium,
                AntiAliasing = 2,
                PixelLights = 2,
                TextureMipLevel = 1,
                TargetFps = 45,
                PhysicsRate = 45
            },
            _ => new QualityPreset
            {
                QualityLevel = 4,
                Shadows = ShadowQuality.All,
                ShadowResolution = ShadowResolution.High,
                AntiAliasing = 4,
                PixelLights = 4,
                TextureMipLevel = 0,
                TargetFps = 60,
                PhysicsRate = 60
            }
        };
    }
    
    private struct QualityPreset
    {
        public int QualityLevel;
        public ShadowQuality Shadows;
        public ShadowResolution ShadowResolution;
        public int AntiAliasing;
        public int PixelLights;
        public int TextureMipLevel;
        public int TargetFps;
        public int PhysicsRate;
    }
}
```

## Render Pipeline Optimization

### Dynamic Resolution

```csharp
public class DynamicResolutionController : MonoBehaviour
{
    [SerializeField] private float targetFrameTime = 16.67f; // 60 FPS
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1f;
    
    private float _currentScale = 1f;
    private float _smoothedFrameTime;
    
    private void Update()
    {
        // Smoothing keeps a single spike from tanking resolution for one frame.
        float frameTime = Time.unscaledDeltaTime * 1000f;
        _smoothedFrameTime = Mathf.Lerp(_smoothedFrameTime, frameTime, 0.1f);
        
        // Drop fast, recover slowly, so the scale does not oscillate around the target.
        if (_smoothedFrameTime > targetFrameTime * 1.2f)
            _currentScale = Mathf.Max(minScale, _currentScale - 0.05f);
        else if (_smoothedFrameTime < targetFrameTime * 0.8f)
            _currentScale = Mathf.Min(maxScale, _currentScale + 0.02f);
        
        ScalableBufferManager.ResizeBuffers(_currentScale, _currentScale);
    }
}
```

### LOD Bias

```csharp
public class LODController
{
    public static void SetLODForTier(DeviceTier tier)
    {
        switch (tier)
        {
            case DeviceTier.Low:
                QualitySettings.lodBias = 0.5f;
                QualitySettings.maximumLODLevel = 1;
                break;
                
            case DeviceTier.Medium:
                QualitySettings.lodBias = 1f;
                QualitySettings.maximumLODLevel = 0;
                break;
                
            case DeviceTier.High:
                QualitySettings.lodBias = 1.5f;
                QualitySettings.maximumLODLevel = 0;
                break;
        }
    }
}
```

## Best Practices

1. **Profile on actual devices** - Not just Editor
2. **Set memory budgets** per device tier
3. **Monitor thermal state** and adapt
4. **Use dynamic frame rate** based on gameplay
5. **Implement quality tiers** automatically
6. **Handle background/foreground** transitions
7. **Pool and reuse** everything
8. **Compress textures** appropriately
9. **Limit draw calls** to <100 on low-end
10. **Test on minimum spec** devices

## Troubleshooting

| Issue | Solution |
|-------|----------|
| App killed by OS | Reduce memory, handle lowMemory |
| Device overheating | Monitor thermal, reduce quality |
| Battery drain | Lower frame rate, disable features |
| Stuttering | Profile GC, use pools |
| Long load times | Stream assets, show progress |
| Crash on low-end | Test minimum spec, reduce quality |
