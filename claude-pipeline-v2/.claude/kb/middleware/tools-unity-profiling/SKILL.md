---
name: tools-unity-profiling
description: Unity profiling patterns including ProfilerMarkers, FrameTimingManager, memory profiling, and performance budgets.
---

# Unity Profiling

## Overview

Unity provides comprehensive profiling tools for CPU, GPU, memory, and frame timing analysis. This skill covers instrumentation patterns and performance optimization workflows.

## When to Use

- Identifying performance bottlenecks
- Tracking memory allocations
- Measuring frame times
- Implementing performance budgets
- Detecting hitches and stutters

## ProfilerMarkers

### Basic ProfilerMarker

```csharp
using Unity.Profiling;

public class CombatSystem
{
    private static readonly ProfilerMarker s_updateMarker = new(
        ProfilerCategory.Scripts,
        "CombatSystem.Update"
    );
    
    public void Update()
    {
        s_updateMarker.Begin();
        try
        {
            ProcessCombat();
        }
        finally
        {
            s_updateMarker.End();
        }
    }
}
```

### Auto-Dispose Pattern

```csharp
public class AbilitySystem
{
    private static readonly ProfilerMarker s_executeMarker = new("AbilitySystem.Execute");
    
    public void ExecuteAbility(Ability ability)
    {
        using (s_executeMarker.Auto())
        {
            ability.Activate();
            ApplyEffects();
            PlayAnimation();
        }
    }
}
```

### Nested Markers

```csharp
public class GameLoop
{
    private static readonly ProfilerMarker s_updateMarker = new("GameLoop.Update");
    private static readonly ProfilerMarker s_physicsMarker = new("GameLoop.Physics");
    private static readonly ProfilerMarker s_aiMarker = new("GameLoop.AI");
    private static readonly ProfilerMarker s_renderMarker = new("GameLoop.Render");
    
    public void Update()
    {
        using (s_updateMarker.Auto())
        {
            using (s_physicsMarker.Auto())
            {
                UpdatePhysics();
            }
            
            using (s_aiMarker.Auto())
            {
                UpdateAI();
            }
            
            using (s_renderMarker.Auto())
            {
                PrepareRendering();
            }
        }
    }
}
```

### ProfilerMarker with Metadata

```csharp
private static readonly ProfilerMarker s_loadAssetMarker = new(
    ProfilerCategory.Loading,
    "LoadAsset",
    new ProfilerMarkerDataUnit[]
    {
        ProfilerMarkerDataUnit.Bytes,
        ProfilerMarkerDataUnit.Count
    }
);

public void LoadAsset(string path)
{
    using (s_loadAssetMarker.Auto())
    {
        // Marker metadata captures additional info
        var asset = Resources.Load(path);
    }
}
```

## FrameTimingManager

### Basic Frame Timing

```csharp
public class FrameTimeTracker
{
    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
    
    public void CaptureFrameTiming()
    {
        if (!FrameTimingManager.IsFeatureEnabled())
        {
            GameDebug.LogWarning("FrameTimingManager not available");
            return;
        }
        
        FrameTimingManager.CaptureFrameTimings();
        var count = FrameTimingManager.GetLatestTimings(1, _frameTimings);
        
        if (count == 0)
            return;

        var timing = _frameTimings[0];
        
        GameDebug.Log($"CPU Frame Time: {timing.cpuFrameTime:F2}ms");
        GameDebug.Log($"CPU Main Thread: {timing.cpuMainThreadFrameTime:F2}ms");
        GameDebug.Log($"CPU Render Thread: {timing.cpuRenderThreadFrameTime:F2}ms");
        GameDebug.Log($"GPU Frame Time: {timing.gpuFrameTime:F2}ms");
        GameDebug.Log($"Present Wait: {timing.cpuMainThreadPresentWaitTime:F2}ms");
    }
}
```

### Continuous Frame Monitoring

```csharp
public class PerformanceMonitor : MonoBehaviour
{
    private readonly FrameTiming[] _frameTimings = new FrameTiming[30];
    
    private float _avgCpuTime;
    private float _avgGpuTime;
    private float _maxFrameTime;
    
    private void Update()
    {
        if (!FrameTimingManager.IsFeatureEnabled())
            return;
        
        FrameTimingManager.CaptureFrameTimings();
        var count = FrameTimingManager.GetLatestTimings(_frameTimings.Length, _frameTimings);
        
        if (count == 0)
            return;

        float totalCpu = 0, totalGpu = 0;
        _maxFrameTime = 0;
        int gpuCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            var t = _frameTimings[i];
            totalCpu += t.cpuFrameTime;
            
            // GPU timing is unavailable on some drivers and reports 0; skip it from the average.
            if (t.gpuFrameTime > 0)
            {
                totalGpu += t.gpuFrameTime;
                gpuCount++;
            }
            
            var frameTime = Mathf.Max(t.cpuFrameTime, t.gpuFrameTime);
            _maxFrameTime = Mathf.Max(_maxFrameTime, frameTime);
        }
        
        _avgCpuTime = totalCpu / count;
        _avgGpuTime = gpuCount > 0 ? totalGpu / gpuCount : 0;
    }
    
    public (float cpu, float gpu, float max) GetFrameTimes()
    {
        return (_avgCpuTime, _avgGpuTime, _maxFrameTime);
    }
}
```

## Memory Profiling

### Memory Snapshot

```csharp
using UnityEngine.Profiling;

public class MemoryTracker
{
    public MemorySnapshot CaptureSnapshot()
    {
        return new MemorySnapshot
        {
            TotalAllocated = Profiler.GetTotalAllocatedMemoryLong(),
            TotalReserved = Profiler.GetTotalReservedMemoryLong(),
            TotalUnused = Profiler.GetTotalUnusedReservedMemoryLong(),
            MonoUsed = Profiler.GetMonoUsedSizeLong(),
            MonoHeap = Profiler.GetMonoHeapSizeLong(),
            GfxDriver = Profiler.GetAllocatedMemoryForGraphicsDriver(),
            Timestamp = Time.realtimeSinceStartup
        };
    }
}

public struct MemorySnapshot
{
    public long TotalAllocated;
    public long TotalReserved;
    public long TotalUnused;
    public long MonoUsed;
    public long MonoHeap;
    public long GfxDriver;
    public float Timestamp;
    
    public long TotalMB => TotalAllocated / 1_000_000;
    public long MonoMB => MonoUsed / 1_000_000;
}
```

### GC Allocation Tracking

```csharp
public class GCAllocationTracker
{
    private long _lastGCMemory;
    private int _lastGCCount;
    
    public void BeginFrame()
    {
        _lastGCMemory = GC.GetTotalMemory(false);
        _lastGCCount = GC.CollectionCount(0);
    }
    
    public (long allocated, bool gcOccurred) EndFrame()
    {
        var currentMemory = GC.GetTotalMemory(false);
        var currentGCCount = GC.CollectionCount(0);
        
        var allocated = currentMemory - _lastGCMemory;
        var gcOccurred = currentGCCount > _lastGCCount;
        
        // Anything under 1 KB is noise from the profiler itself.
        if (allocated > 1024)
            GameDebug.LogWarning($"Frame allocated {allocated} bytes");
        
        return (allocated, gcOccurred);
    }
}
```

### GC Notification

```csharp
public class GCNotificationHandler
{
    private bool _isRegistered;
    
    public void Register()
    {
        if (_isRegistered)
            return;
        
        GC.RegisterForFullGCNotification(10, 10);
        _isRegistered = true;
        
        StartMonitoringThread();
    }
    
    private void StartMonitoringThread()
    {
        // WaitForFullGC* block, so they must live off the main thread.
        // The callbacks therefore run on that thread too: no Unity API calls inside them.
        var thread = new Thread(() =>
        {
            while (_isRegistered)
            {
                var status = GC.WaitForFullGCApproach();
                if (status == GCNotificationStatus.Succeeded)
                    OnGCApproaching();
                
                status = GC.WaitForFullGCComplete();
                if (status == GCNotificationStatus.Succeeded)
                    OnGCComplete();
            }
        });
        thread.IsBackground = true;
        thread.Start();
    }
    
    private void OnGCApproaching()
    {
        GameDebug.Log("Full GC approaching");
    }
    
    private void OnGCComplete()
    {
        GameDebug.Log("Full GC complete");
    }
}
```

## Hitch Detection

### Simple Hitch Detector

```csharp
public class HitchDetector : MonoBehaviour
{
    private const float MinorHitchMs = 33.33f;  // < 30 FPS
    private const float MajorHitchMs = 50f;     // < 20 FPS
    private const float SevereHitchMs = 100f;   // < 10 FPS
    
    private float _lastFrameTime;
    
    public event Action<float, HitchSeverity> OnHitchDetected;
    
    private void Update()
    {
        var frameTimeMs = Time.unscaledDeltaTime * 1000f;
        
        if (frameTimeMs >= SevereHitchMs)
            OnHitchDetected?.Invoke(frameTimeMs, HitchSeverity.Severe);
        else if (frameTimeMs >= MajorHitchMs)
            OnHitchDetected?.Invoke(frameTimeMs, HitchSeverity.Major);
        else if (frameTimeMs >= MinorHitchMs)
            OnHitchDetected?.Invoke(frameTimeMs, HitchSeverity.Minor);
        
        _lastFrameTime = frameTimeMs;
    }
}

public enum HitchSeverity { Minor, Major, Severe }
```

### Advanced Hitch Tracker

```csharp
public class HitchTracker
{
    private readonly Queue<float> _frameTimes = new();
    private readonly int _windowSize;
    
    private int _minorHitchCount;
    private int _majorHitchCount;
    private int _severeHitchCount;
    private int _totalFrames;
    
    public float HitchRate => _totalFrames > 0 
        ? (float)(_minorHitchCount + _majorHitchCount + _severeHitchCount) / _totalFrames 
        : 0f;
    
    public HitchTracker(int windowSize = 300)
    {
        _windowSize = windowSize;
    }
    
    public void RecordFrame(float frameTimeMs)
    {
        _frameTimes.Enqueue(frameTimeMs);
        _totalFrames++;
        
        if (frameTimeMs >= 100f)
            _severeHitchCount++;
        else if (frameTimeMs >= 50f)
            _majorHitchCount++;
        else if (frameTimeMs >= 33.33f)
            _minorHitchCount++;
        
        // A sliding window keeps the rate about recent play, not the whole session.
        while (_frameTimes.Count > _windowSize)
        {
            var removed = _frameTimes.Dequeue();
            _totalFrames--;
            
            if (removed >= 100f)
                _severeHitchCount--;
            else if (removed >= 50f)
                _majorHitchCount--;
            else if (removed >= 33.33f)
                _minorHitchCount--;
        }
    }
    
    public HitchStats GetStats()
    {
        return new HitchStats
        {
            TotalFrames = _totalFrames,
            MinorHitches = _minorHitchCount,
            MajorHitches = _majorHitchCount,
            SevereHitches = _severeHitchCount,
            HitchRate = HitchRate,
            WorstFrameTime = _frameTimes.Count > 0 ? _frameTimes.Max() : 0f
        };
    }
}

public struct HitchStats
{
    public int TotalFrames;
    public int MinorHitches;
    public int MajorHitches;
    public int SevereHitches;
    public float HitchRate;
    public float WorstFrameTime;
}
```

## Performance Budgets

### Frame Budget System

```csharp
public class FrameBudgetManager
{
    public const float TotalBudgetMs = 16.67f; // 60 FPS target

    private readonly Dictionary<string, float> _budgets = new();
    private readonly Dictionary<string, float> _actuals = new();
    
    public void SetBudget(string system, float budgetMs)
    {
        _budgets[system] = budgetMs;
    }
    
    public void RecordActual(string system, float actualMs)
    {
        _actuals[system] = actualMs;
        
        if (_budgets.TryGetValue(system, out var budget)
            && actualMs > budget)
            GameDebug.LogWarning($"{system} over budget: {actualMs:F2}ms / {budget:F2}ms");
    }
    
    public bool IsWithinBudget()
    {
        var totalActual = _actuals.Values.Sum();
        return totalActual <= TotalBudgetMs;
    }
    
    public void LogBudgetReport()
    {
        GameDebug.Log("=== Frame Budget Report ===");
        
        foreach (var (system, budget) in _budgets)
        {
            var actual = _actuals.GetValueOrDefault(system, 0f);
            var status = actual <= budget ? "OK" : "OVER";
            GameDebug.Log($"{system}: {actual:F2}ms / {budget:F2}ms [{status}]");
        }
        
        var totalActual = _actuals.Values.Sum();
        GameDebug.Log($"Total: {totalActual:F2}ms / {TotalBudgetMs:F2}ms");
    }
}

// Usage
var budgetManager = new FrameBudgetManager();
budgetManager.SetBudget("Physics", 2f);
budgetManager.SetBudget("AI", 3f);
budgetManager.SetBudget("Animation", 2f);
budgetManager.SetBudget("Rendering", 8f);
```

### Memory Budget System

```csharp
public class MemoryBudgetManager
{
    private long _totalBudgetMB;
    private long _textureBudgetMB;
    private long _audioBudgetMB;
    private long _meshBudgetMB;

    public long TotalBudgetMB => _totalBudgetMB;
    public long TextureBudgetMB => _textureBudgetMB;
    public long AudioBudgetMB => _audioBudgetMB;
    public long MeshBudgetMB => _meshBudgetMB;
    
    public void SetBudgetsForDevice(DeviceTier tier)
    {
        switch (tier)
        {
            case DeviceTier.Low:
                _totalBudgetMB = 512;
                _textureBudgetMB = 256;
                _audioBudgetMB = 64;
                _meshBudgetMB = 128;
                break;
                
            case DeviceTier.Medium:
                _totalBudgetMB = 1024;
                _textureBudgetMB = 512;
                _audioBudgetMB = 128;
                _meshBudgetMB = 256;
                break;
                
            case DeviceTier.High:
                _totalBudgetMB = 2048;
                _textureBudgetMB = 1024;
                _audioBudgetMB = 256;
                _meshBudgetMB = 512;
                break;
        }
    }
    
    public bool IsOverBudget()
    {
        var currentMB = Profiler.GetTotalAllocatedMemoryLong() / 1_000_000;
        return currentMB > _totalBudgetMB;
    }
    
    public float GetBudgetUsagePercent()
    {
        var currentMB = Profiler.GetTotalAllocatedMemoryLong() / 1_000_000;
        return (float)currentMB / _totalBudgetMB * 100f;
    }
}

public enum DeviceTier { Low, Medium, High }
```

## Profiler Counters (Custom)

### ProfilerCounter

```csharp
using Unity.Profiling;

public class CustomProfilerCounters
{
    // Readonly statics exposed on purpose: Sample() is the only operation and the
    // counters must be reachable from every system without a wrapper call.
    public static readonly ProfilerCounter<int> EnemyCount = new(
        ProfilerCategory.Scripts,
        "Enemy Count",
        ProfilerMarkerDataUnit.Count
    );
    
    public static readonly ProfilerCounter<int> ActiveEffects = new(
        ProfilerCategory.Scripts,
        "Active Effects",
        ProfilerMarkerDataUnit.Count
    );
    
    public static readonly ProfilerCounter<float> MemoryUsageMB = new(
        ProfilerCategory.Memory,
        "Custom Memory MB",
        ProfilerMarkerDataUnit.Bytes
    );
}

// Usage in Update
CustomProfilerCounters.EnemyCount.Sample(_enemies.Count);
CustomProfilerCounters.ActiveEffects.Sample(_activeEffects.Count);
```

## Best Practices

1. **Use ProfilerMarkers** for custom code sections
2. **Sample FrameTimingManager** for accurate timing
3. **Set performance budgets** early in development
4. **Track allocations** to prevent GC spikes
5. **Detect hitches** in production builds
6. **Profile on target devices** - Editor is not representative
7. **Use Deep Profile sparingly** - Significant overhead
8. **Profile specific scenarios** - Combat, loading, menus
9. **Automate profiling** - CI performance tests
10. **Log outliers** - Capture worst-case frames

## Troubleshooting

| Issue | Investigation |
|-------|--------------|
| Low FPS | Check CPU vs GPU bound in Profiler |
| Hitches | Look for GC, loading, or spikes |
| Memory growth | Track allocations over time |
| GC spikes | Find allocation sources |
| GPU bound | Reduce draw calls, shader complexity |
| CPU bound | Optimize scripts, reduce Updates |
