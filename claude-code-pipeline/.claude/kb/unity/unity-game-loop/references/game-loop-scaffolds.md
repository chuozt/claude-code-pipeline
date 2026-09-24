# Game Loop & Progression Scaffolds
Detailed implementations for game loop systems. Supplements the PATTERN blocks in the parent SKILL.md.

---

## Complete GameLoopController

### IGamePhase Interface

```csharp
using System.Threading;
using UnityEngine;

/// <summary>
/// Represents a single phase in the core game loop (e.g., Explore, Collect, Craft, Combat).
/// Each phase has async enter/exit for loading and cleanup, and a per-frame tick.
/// </summary>
public interface IGamePhase
{
    /// <summary>Display name for debug UI and logging.</summary>
    string PhaseName { get; }

    /// <summary>
    /// Called when this phase becomes the active phase.
    /// Use for enabling systems, loading assets, starting music, fading in UI.
    /// </summary>
    /// <param name="ct">Cancellation token tied to the controller's lifetime.</param>
    Awaitable EnterAsync(CancellationToken ct);

    /// <summary>
    /// Called every frame while this phase is active.
    /// Use for phase-specific update logic.
    /// </summary>
    /// <param name="deltaTime">Time.deltaTime passed from the controller.</param>
    void Tick(float deltaTime);

    /// <summary>
    /// Called when transitioning away from this phase.
    /// Use for disabling systems, unloading assets, cleanup.
    /// </summary>
    /// <param name="ct">Cancellation token tied to the controller's lifetime.</param>
    Awaitable ExitAsync(CancellationToken ct);
}
```

### GamePhaseConfig ScriptableObject

```csharp
using UnityEngine;

/// <summary>
/// Configuration asset for a single game phase. Designers create one per phase
/// and arrange them in the GameLoopController's phase list.
/// </summary>
[CreateAssetMenu(menuName = "Game Loop/Phase Config")]
public class GamePhaseConfig : ScriptableObject
{
    [Tooltip("Display name for this phase")]
    [SerializeField] private string phaseName;

    [Tooltip("Assembly-qualified type name of the IGamePhase implementation")]
    [SerializeField] private string phaseTypeName;

    [Tooltip("Optional: systems to enable when this phase starts")]
    [SerializeField] private string[] systemTags;

    [Tooltip("Maximum duration in seconds. 0 = phase decides when to end via AdvancePhaseAsync")]
    [SerializeField] private float maxDuration;

    public string PhaseName => phaseName;
    public string PhaseTypeName => phaseTypeName;
    public string[] SystemTags => systemTags;
    public float MaxDuration => maxDuration;
}
```

### GameLoopController MonoBehaviour

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Drives the core game loop through a designer-configured sequence of phases.
/// Phases are instantiated from <see cref="GamePhaseConfig"/> assets.
/// Supports cycling (loop back to phase 0 after the last phase) and direct jumps.
/// </summary>
public class GameLoopController : MonoBehaviour
{
    [SerializeField] private List<GamePhaseConfig> phaseConfigs;
    [SerializeField] private bool isAutoStart = true;
    [SerializeField] private bool isLooping = true;

    private readonly List<IGamePhase> _phases = new();
    private IGamePhase _currentPhase;
    private int _currentIndex = -1;
    private float _phaseElapsed;
    private bool _isTransitioning;

    /// <summary>Index of the currently active phase, or -1 if none.</summary>
    public int CurrentPhaseIndex => _currentIndex;

    /// <summary>The currently active phase, or null.</summary>
    public IGamePhase CurrentPhase => _currentPhase;

    /// <summary>Seconds elapsed in the current phase.</summary>
    public float PhaseElapsedTime => _phaseElapsed;

    /// <summary>Fires when a new phase begins. Arg: phase index.</summary>
    public event Action<int> OnPhaseStarted;

    /// <summary>Fires when a phase ends. Arg: phase index.</summary>
    public event Action<int> OnPhaseEnded;

    /// <summary>Fires when all phases have completed and loop is disabled.</summary>
    public event Action OnLoopComplete;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
    }

    private async void Start()
    {
        BuildPhases();

        if (isAutoStart && _phases.Count > 0)
            await TransitionToPhaseAsync(0);
    }

    private void Update()
    {
        if (_currentPhase == null || _isTransitioning)
            return;

        _phaseElapsed += Time.deltaTime;
        _currentPhase.Tick(Time.deltaTime);

        // Auto-advance if maxDuration is set and exceeded
        if (_currentIndex >= 0 && _currentIndex < phaseConfigs.Count)
        {
            float maxDur = phaseConfigs[_currentIndex].MaxDuration;

            if (maxDur > 0f && _phaseElapsed >= maxDur)
                _ = AdvancePhaseAsync();
        }
    }

    /// <summary>
    /// Advance to the next phase in the sequence.
    /// If loop is true, wraps to phase 0 after the last phase.
    /// If loop is false, fires OnLoopComplete after the last phase.
    /// </summary>
    public async Awaitable AdvancePhaseAsync()
    {
        int next = _currentIndex + 1;

        if (next >= _phases.Count)
        {
            if (isLooping)
                next = 0;
            else
            {
                // End the current phase and signal completion
                if (_currentPhase != null)
                {
                    _isTransitioning = true;
                    await _currentPhase.ExitAsync(destroyCancellationToken);
                    OnPhaseEnded?.Invoke(_currentIndex);
                    _currentPhase = null;
                    _currentIndex = -1;
                    _isTransitioning = false;
                }

                OnLoopComplete?.Invoke();
                return;
            }
        }

        await TransitionToPhaseAsync(next);
    }

    /// <summary>Jump directly to a specific phase by index.</summary>
    public async Awaitable TransitionToPhaseAsync(int index)
    {
        if (_isTransitioning)
            return;

        if (index < 0 || index >= _phases.Count)
        {
            GameDebug.LogError($"Phase index {index} out of range (0-{_phases.Count - 1}).");
            return;
        }

        _isTransitioning = true;
        var ct = destroyCancellationToken;

        if (_currentPhase != null)
        {
            await _currentPhase.ExitAsync(ct);
            OnPhaseEnded?.Invoke(_currentIndex);
        }

        _currentIndex = index;
        _currentPhase = _phases[_currentIndex];
        _phaseElapsed = 0f;

        await _currentPhase.EnterAsync(ct);
        OnPhaseStarted?.Invoke(_currentIndex);

        _isTransitioning = false;
    }

    private void BuildPhases()
    {
        _phases.Clear();

        foreach (var config in phaseConfigs)
        {
            if (string.IsNullOrEmpty(config.PhaseTypeName))
            {
                GameDebug.LogError($"Phase config '{config.PhaseName}' has no type name.");
                continue;
            }

            var type = Type.GetType(config.PhaseTypeName);

            if (type == null)
            {
                GameDebug.LogError($"Phase type not found: {config.PhaseTypeName}");
                continue;
            }

            if (!typeof(IGamePhase).IsAssignableFrom(type))
            {
                GameDebug.LogError($"Type {config.PhaseTypeName} does not implement IGamePhase.");
                continue;
            }

            _phases.Add((IGamePhase)Activator.CreateInstance(type));
        }
    }
}
```

### Example: ExplorePhase

```csharp
using System.Threading;
using UnityEngine;

/// <summary>
/// Exploration phase: enables player movement, disables combat UI,
/// allows resource gathering. Transitions when the player reaches a trigger zone
/// or the phase timer expires.
/// </summary>
public class ExplorePhase : IGamePhase
{
    public string PhaseName => "Explore";

    public async Awaitable EnterAsync(CancellationToken ct)
    {
        GameDebug.Log("[ExplorePhase] Entering -- enabling exploration systems");
        // Enable exploration-related systems
        // e.g., Services.Get<IMapService>().EnableFogOfWar();
        //       Services.Get<IUIService>().ShowExploreHUD();
        await Awaitable.NextFrameAsync(ct);
    }

    public void Tick(float deltaTime)
    {
        // Phase-specific logic: check if player reached an exit zone, etc.
    }

    public async Awaitable ExitAsync(CancellationToken ct)
    {
        GameDebug.Log("[ExplorePhase] Exiting -- disabling exploration systems");
        // Disable exploration-related systems
        await Awaitable.NextFrameAsync(ct);
    }
}
```

### Example: CombatPhase

```csharp
using System.Threading;
using UnityEngine;

/// <summary>
/// Combat phase: spawns enemies, enables combat systems and UI.
/// Ends when all enemies are defeated or the timer expires.
/// </summary>
public class CombatPhase : IGamePhase
{
    public string PhaseName => "Combat";

    public async Awaitable EnterAsync(CancellationToken ct)
    {
        GameDebug.Log("[CombatPhase] Entering -- spawning enemies, enabling combat");
        // e.g., Services.Get<ISpawnService>().SpawnWave();
        //       Services.Get<IUIService>().ShowCombatHUD();
        await Awaitable.NextFrameAsync(ct);
    }

    public void Tick(float deltaTime)
    {
        // Check if all enemies are defeated, etc.
        // When done, call GameLoopController.AdvancePhaseAsync()
    }

    public async Awaitable ExitAsync(CancellationToken ct)
    {
        GameDebug.Log("[CombatPhase] Exiting -- cleaning up combat");
        // Cleanup spawned enemies, disable combat UI
        await Awaitable.NextFrameAsync(ct);
    }
}
```

---

## Complete SessionManager

### SessionConfig ScriptableObject

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the starting conditions for a play session.
/// Designers create one per level/mode/difficulty combination.
/// </summary>
[CreateAssetMenu(menuName = "Game Loop/Session Config")]
public class SessionConfig : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this session type")]
    [SerializeField] private string sessionId;

    [Tooltip("Display name for UI")]
    [SerializeField] private string displayName;

    [Header("Starting Conditions")]
    [Tooltip("Number of lives the player starts with. 0 = unlimited.")]
    [SerializeField] private int startingLives = 3;

    [Tooltip("Starting score")]
    [SerializeField] private int startingScore;

    [Tooltip("Time limit in seconds. 0 = no time limit.")]
    [SerializeField] private float timeLimitSeconds;

    [Header("References")]
    [Tooltip("Scene to load for this session, if any")]
    [SerializeField] private string sceneName;

    [Tooltip("Difficulty config to use")]
    [SerializeField] private DifficultyConfig difficultyConfig;

    [Tooltip("Pacing config to use")]
    [SerializeField] private PacingCurveConfig pacingConfig;

    [Tooltip("Rewards to grant on victory")]
    [SerializeField] private List<MetaRewardBase> victoryRewards;

    [Tooltip("Rewards to grant on defeat (consolation)")]
    [SerializeField] private List<MetaRewardBase> defeatRewards;

    public string SessionId => sessionId;
    public string DisplayName => displayName;
    public int StartingLives => startingLives;
    public int StartingScore => startingScore;
    public float TimeLimitSeconds => timeLimitSeconds;
    public string SceneName => sceneName;
    public DifficultyConfig DifficultyConfig => difficultyConfig;
    public PacingCurveConfig PacingConfig => pacingConfig;
    public List<MetaRewardBase> VictoryRewards => victoryRewards;
    public List<MetaRewardBase> DefeatRewards => defeatRewards;
}
```

### SessionData Class

```csharp
using System;
using UnityEngine;

/// <summary>
/// Mutable state for an active session. Created by <see cref="SessionManager.StartSession"/>,
/// destroyed by <see cref="SessionManager.EndSession"/>. Not persisted -- meta state is separate.
/// </summary>
[Serializable]
public class SessionData
{
    /// <summary>Config that started this session.</summary>
    public SessionConfig Config { get; }

    /// <summary>Current score.</summary>
    public int Score { get; set; }

    /// <summary>Remaining lives. -1 = unlimited.</summary>
    public int LivesRemaining { get; set; }

    /// <summary>Seconds elapsed since session start.</summary>
    public float ElapsedTime { get; set; }

    /// <summary>When the session started (UTC).</summary>
    public DateTime StartTime { get; }

    /// <summary>True if the session has a time limit and it has been exceeded.</summary>
    public bool IsTimeExpired => Config.TimeLimitSeconds > 0 && ElapsedTime >= Config.TimeLimitSeconds;

    /// <summary>True if lives are limited and have reached zero.</summary>
    public bool IsOutOfLives => LivesRemaining == 0 && Config.StartingLives > 0;

    public SessionData(SessionConfig config)
    {
        Config = config;
        Score = config.StartingScore;
        LivesRemaining = config.StartingLives > 0 ? config.StartingLives : -1;
        ElapsedTime = 0f;
        StartTime = DateTime.UtcNow;
    }
}
```

### SessionResult Struct

```csharp
using System;

/// <summary>
/// Immutable snapshot of how a session ended. Consumed by meta progression,
/// analytics, and UI systems.
/// </summary>
[Serializable]
public struct SessionResult
{
    /// <summary>Config that defined this session.</summary>
    public SessionConfig Config;

    /// <summary>True if the player won.</summary>
    public bool Victory;

    /// <summary>Final score.</summary>
    public int Score;

    /// <summary>Total elapsed time in seconds.</summary>
    public float ElapsedTime;

    /// <summary>When the session ended (UTC).</summary>
    public DateTime EndTime;

    /// <summary>Remaining lives at session end.</summary>
    public int LivesRemaining;
}
```

### SessionManager MonoBehaviour

```csharp
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Manages the lifecycle of a single play session (roguelike run, match, level attempt).
/// Persists across scenes via DontDestroyOnLoad. Separates session-scoped state from
/// persistent meta progression.
/// </summary>
public class SessionManager : MonoBehaviour
{
    [SerializeField] private SessionConfig defaultConfig;

    private static SessionManager s_instance;

    private SessionData _currentSession;

    /// <summary>Singleton instance. Reset on domain reload.</summary>
    public static SessionManager Instance => s_instance;

    /// <summary>Current session data. Null when no session is active.</summary>
    public SessionData CurrentSession => _currentSession;

    /// <summary>True while a session is in progress.</summary>
    public bool IsSessionActive => _currentSession != null;

    /// <summary>Fires when a new session begins.</summary>
    public event Action<SessionConfig> OnSessionStarted;

    /// <summary>Fires when a session ends with its result.</summary>
    public event Action<SessionResult> OnSessionEnded;

    /// <summary>Fires when a session is restarted (end + start).</summary>
    public event Action OnSessionRestarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => s_instance = null;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (_currentSession != null)
            _currentSession.ElapsedTime += Time.deltaTime;
    }

    /// <summary>
    /// Begin a new session with the given configuration.
    /// If no config is provided, uses the default.
    /// </summary>
    /// <param name="config">Session configuration. Null = use default.</param>
    public void StartSession(SessionConfig config = null)
    {
        if (_currentSession != null)
        {
            GameDebug.LogWarning("SessionManager: Starting a new session while one is active. Ending current session.");
            EndSession(false);
        }

        config ??= defaultConfig;

        if (config == null)
        {
            GameDebug.LogError("SessionManager: No session config provided and no default set.");
            return;
        }

        _currentSession = new SessionData(config);
        OnSessionStarted?.Invoke(config);
        GameDebug.Log($"[SessionManager] Session started: {config.DisplayName}");
    }

    /// <summary>
    /// End the current session and publish the result.
    /// </summary>
    /// <param name="victory">Whether the player won.</param>
    /// <returns>The session result, or default if no session was active.</returns>
    public SessionResult EndSession(bool victory)
    {
        if (_currentSession == null)
        {
            GameDebug.LogWarning("SessionManager: No active session to end.");
            return default;
        }

        var result = new SessionResult
        {
            Config = _currentSession.Config,
            Victory = victory,
            Score = _currentSession.Score,
            ElapsedTime = _currentSession.ElapsedTime,
            EndTime = DateTime.UtcNow,
            LivesRemaining = _currentSession.LivesRemaining
        };

        _currentSession = null;
        OnSessionEnded?.Invoke(result);
        GameDebug.Log($"[SessionManager] Session ended: victory={victory}, score={result.Score}");

        return result;
    }

    /// <summary>
    /// Restart the current session without reloading the scene.
    /// Ends the current session (as a loss) and starts a new one with the same config.
    /// </summary>
    public void RestartSession()
    {
        var config = _currentSession?.Config ?? defaultConfig;
        EndSession(false);
        StartSession(config);
        OnSessionRestarted?.Invoke();
    }

    /// <summary>
    /// Deduct a life from the current session.
    /// Returns true if lives remain (or unlimited), false if now out of lives.
    /// </summary>
    public bool UseLife()
    {
        if (_currentSession == null)
            return false;

        // Negative lives means unlimited
        if (_currentSession.LivesRemaining < 0)
            return true;

        _currentSession.LivesRemaining--;
        return _currentSession.LivesRemaining > 0;
    }

    /// <summary>Add to the current session's score.</summary>
    public void AddScore(int points)
    {
        if (_currentSession != null)
            _currentSession.Score += points;
    }
}
```

---

## Complete ConditionEvaluator

### WinConditionBase / LoseConditionBase

```csharp
using UnityEngine;

/// <summary>
/// Base class for win conditions. Implement as ScriptableObject for Inspector assignment.
/// The ConditionEvaluator holds a list of these and checks them on gameplay events.
/// </summary>
public abstract class WinConditionBase : ScriptableObject
{
    /// <summary>Human-readable description for Inspector tooltips and debug UI.</summary>
    public abstract string Description { get; }

    /// <summary>Initialize this condition for a new session or level.</summary>
    public abstract void Initialize();

    /// <summary>Returns true when the win condition is satisfied.</summary>
    public abstract bool IsMet();

    /// <summary>Cleanup when the session or level ends.</summary>
    public abstract void Teardown();
}

/// <summary>
/// Base class for lose conditions. Same contract as WinConditionBase.
/// </summary>
public abstract class LoseConditionBase : ScriptableObject
{
    /// <summary>Human-readable description for Inspector tooltips and debug UI.</summary>
    public abstract string Description { get; }

    /// <summary>Initialize this condition for a new session or level.</summary>
    public abstract void Initialize();

    /// <summary>Returns true when the lose condition is satisfied.</summary>
    public abstract bool IsMet();

    /// <summary>Cleanup when the session or level ends.</summary>
    public abstract void Teardown();
}
```

### ConditionEvaluator MonoBehaviour

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Evaluates composable win/lose conditions for the current level.
/// Conditions are ScriptableObject assets assigned in the Inspector.
/// Supports AND (all required) and OR (any required) composition modes.
/// Call <see cref="Evaluate"/> from gameplay events, not every frame.
/// </summary>
public class ConditionEvaluator : MonoBehaviour
{
    /// <summary>How to compose multiple conditions.</summary>
    public enum CompositionMode
    {
        /// <summary>All conditions must be true.</summary>
        AllRequired,
        /// <summary>Any single condition being true is sufficient.</summary>
        AnyRequired
    }

    [Header("Win Conditions")]
    [SerializeField] private List<WinConditionBase> winConditions = new();
    [SerializeField] private CompositionMode winMode = CompositionMode.AllRequired;

    [Header("Lose Conditions")]
    [SerializeField] private List<LoseConditionBase> loseConditions = new();
    [SerializeField] private CompositionMode loseMode = CompositionMode.AnyRequired;

    private bool _isResolved;

    /// <summary>True if the evaluator has already fired a win or lose event.</summary>
    public bool IsResolved => _isResolved;

    /// <summary>Fires when win conditions are satisfied.</summary>
    public event Action OnWin;

    /// <summary>Fires when lose conditions are satisfied.</summary>
    public event Action OnLose;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
    }

    private void OnEnable()
    {
        _isResolved = false;

        foreach (var c in winConditions)
            c.Initialize();

        foreach (var c in loseConditions)
            c.Initialize();
    }

    private void OnDisable()
    {
        foreach (var c in winConditions)
            c.Teardown();

        foreach (var c in loseConditions)
            c.Teardown();
    }

    /// <summary>
    /// Evaluate all conditions. Call this from gameplay events
    /// (enemy killed, timer tick, objective captured) rather than polling.
    /// Lose conditions are checked first -- if the player has lost, win is not evaluated.
    /// </summary>
    public void Evaluate()
    {
        if (_isResolved)
            return;

        if (CheckLose())
        {
            _isResolved = true;
            GameDebug.Log("[ConditionEvaluator] Lose conditions met.");
            OnLose?.Invoke();
            return;
        }

        if (CheckWin())
        {
            _isResolved = true;
            GameDebug.Log("[ConditionEvaluator] Win conditions met.");
            OnWin?.Invoke();
        }
    }

    /// <summary>Reset the evaluator so it can fire again (e.g., after a checkpoint).</summary>
    public void ResetEvaluation()
    {
        _isResolved = false;
    }

    private bool CheckWin()
    {
        if (winConditions.Count == 0)
            return false;

        return winMode == CompositionMode.AllRequired
            ? winConditions.TrueForAll(c => c.IsMet())
            : winConditions.Exists(c => c.IsMet());
    }

    private bool CheckLose()
    {
        if (loseConditions.Count == 0)
            return false;

        return loseMode == CompositionMode.AnyRequired
            ? loseConditions.Exists(c => c.IsMet())
            : loseConditions.TrueForAll(c => c.IsMet());
    }
}
```

### Example: KillAllCondition

```csharp
using UnityEngine;

/// <summary>
/// Win condition: all tracked enemies are dead.
/// Listens to a static enemy count. Call <see cref="NotifyEnemyKilled"/>
/// from enemy death logic, then trigger ConditionEvaluator.Evaluate().
/// </summary>
[CreateAssetMenu(menuName = "Game Loop/Conditions/Kill All Enemies")]
public class KillAllCondition : WinConditionBase
{
    private int _totalEnemies;
    private int _killedEnemies;

    public override string Description => "All enemies must be eliminated.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
    }

    public override void Initialize()
    {
        _totalEnemies = 0;
        _killedEnemies = 0;
    }

    /// <summary>Call when an enemy spawns to register it.</summary>
    public void RegisterEnemy()
    {
        _totalEnemies++;
    }

    /// <summary>Call when an enemy dies.</summary>
    public void NotifyEnemyKilled()
    {
        _killedEnemies++;
    }

    public override bool IsMet()
    {
        return _totalEnemies > 0 && _killedEnemies >= _totalEnemies;
    }

    public override void Teardown()
    {
        _totalEnemies = 0;
        _killedEnemies = 0;
    }
}
```

### Example: TimerCondition

```csharp
using UnityEngine;

/// <summary>
/// Lose condition: the session timer has expired.
/// Reads elapsed time from <see cref="SessionManager"/>.
/// </summary>
[CreateAssetMenu(menuName = "Game Loop/Conditions/Timer Expired")]
public class TimerCondition : LoseConditionBase
{
    [Tooltip("Time limit in seconds. If 0, reads from SessionConfig.")]
    [SerializeField] private float overrideTimeLimit;

    private float _timeLimit;

    public override string Description => "Time limit has expired.";

    public override void Initialize()
    {
        if (overrideTimeLimit > 0)
        {
            _timeLimit = overrideTimeLimit;
            return;
        }

        var session = SessionManager.Instance?.CurrentSession;
        _timeLimit = session?.Config.TimeLimitSeconds ?? 0f;
    }

    public override bool IsMet()
    {
        if (_timeLimit <= 0f)
            return false;

        var session = SessionManager.Instance?.CurrentSession;

        if (session == null)
            return false;

        return session.ElapsedTime >= _timeLimit;
    }

    public override void Teardown()
    {
    }
}
```

### Example: ReachTargetCondition

```csharp
using UnityEngine;

/// <summary>
/// Win condition: the player has reached a designated target zone.
/// Set <see cref="MarkReached"/> from a trigger collider callback.
/// </summary>
[CreateAssetMenu(menuName = "Game Loop/Conditions/Reach Target")]
public class ReachTargetCondition : WinConditionBase
{
    private bool _isReached;

    public override string Description => "Player must reach the target zone.";

    public override void Initialize()
    {
        _isReached = false;
    }

    /// <summary>Call from a trigger zone's OnTriggerEnter when the player enters.</summary>
    public void MarkReached()
    {
        _isReached = true;
    }

    public override bool IsMet() => _isReached;

    public override void Teardown()
    {
        _isReached = false;
    }
}
```

---

## Complete DifficultyProvider

### DifficultyConfig ScriptableObject

```csharp
using UnityEngine;

/// <summary>
/// Defines all difficulty parameters as AnimationCurves over normalized progression (0-1).
/// Designers edit curves visually in the Inspector to shape the difficulty ramp.
/// Create one per difficulty preset (Easy, Normal, Hard) or per level.
/// </summary>
[CreateAssetMenu(menuName = "Game Loop/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    [Header("Enemy Scaling")]
    [Tooltip("Max concurrent enemies. X: normalized progression (0=start, 1=end). Y: enemy count.")]
    [SerializeField] private AnimationCurve maxEnemiesCurve = AnimationCurve.Linear(0, 5, 1, 20);

    [Tooltip("Seconds between enemy spawns. X: normalized progression. Y: interval.")]
    [SerializeField] private AnimationCurve spawnIntervalCurve = AnimationCurve.Linear(0, 3f, 1, 0.5f);

    [Tooltip("Enemy health multiplier. X: normalized progression. Y: multiplier.")]
    [SerializeField] private AnimationCurve enemyHealthMultCurve = AnimationCurve.Linear(0, 1f, 1, 2f);

    [Header("Damage Scaling")]
    [Tooltip("Enemy damage multiplier. X: normalized progression. Y: multiplier.")]
    [SerializeField] private AnimationCurve enemyDamageMultCurve = AnimationCurve.Linear(0, 1f, 1, 2.5f);

    [Tooltip("Player damage multiplier. X: normalized progression. Y: multiplier.")]
    [SerializeField] private AnimationCurve playerDamageMultCurve = AnimationCurve.Constant(0, 1, 1f);

    [Header("Resource Scaling")]
    [Tooltip("Resource drop rate multiplier. X: normalized progression. Y: multiplier.")]
    [SerializeField] private AnimationCurve resourceDropRateCurve = AnimationCurve.Linear(0, 1.5f, 1, 0.5f);

    [Tooltip("Pickup value multiplier. X: normalized progression. Y: multiplier.")]
    [SerializeField] private AnimationCurve pickupValueMultCurve = AnimationCurve.Constant(0, 1, 1f);

    public AnimationCurve MaxEnemiesCurve => maxEnemiesCurve;
    public AnimationCurve SpawnIntervalCurve => spawnIntervalCurve;
    public AnimationCurve EnemyHealthMultCurve => enemyHealthMultCurve;
    public AnimationCurve EnemyDamageMultCurve => enemyDamageMultCurve;
    public AnimationCurve PlayerDamageMultCurve => playerDamageMultCurve;
    public AnimationCurve ResourceDropRateCurve => resourceDropRateCurve;
    public AnimationCurve PickupValueMultCurve => pickupValueMultCurve;
}
```

### DifficultyProvider Service

```csharp
using System;
using UnityEngine;

/// <summary>
/// Provides current difficulty values by evaluating <see cref="DifficultyConfig"/> curves.
/// Register via Service Locator. Systems query properties instead of using magic numbers.
/// Supports both time-based progression and manual progression (e.g., player skill rating).
/// </summary>
public class DifficultyProvider
{
    private DifficultyConfig _config;
    private float _normalizedProgress;

    /// <summary>Current normalized progression point (0-1).</summary>
    public float NormalizedProgress => _normalizedProgress;

    /// <summary>Max concurrent enemies at current progression.</summary>
    public int MaxEnemies => Mathf.RoundToInt(_config.MaxEnemiesCurve.Evaluate(_normalizedProgress));

    /// <summary>Seconds between spawns at current progression.</summary>
    public float SpawnInterval => _config.SpawnIntervalCurve.Evaluate(_normalizedProgress);

    /// <summary>Enemy health multiplier at current progression.</summary>
    public float EnemyHealthMult => _config.EnemyHealthMultCurve.Evaluate(_normalizedProgress);

    /// <summary>Enemy damage multiplier at current progression.</summary>
    public float EnemyDamageMult => _config.EnemyDamageMultCurve.Evaluate(_normalizedProgress);

    /// <summary>Player damage multiplier at current progression.</summary>
    public float PlayerDamageMult => _config.PlayerDamageMultCurve.Evaluate(_normalizedProgress);

    /// <summary>Resource drop rate multiplier at current progression.</summary>
    public float ResourceDropRate => _config.ResourceDropRateCurve.Evaluate(_normalizedProgress);

    /// <summary>Pickup value multiplier at current progression.</summary>
    public float PickupValueMult => _config.PickupValueMultCurve.Evaluate(_normalizedProgress);

    /// <summary>Fires when the config or progress changes.</summary>
    public event Action OnDifficultyChanged;

    /// <summary>
    /// Create a new DifficultyProvider with the given config.
    /// </summary>
    /// <param name="config">The difficulty configuration asset.</param>
    public DifficultyProvider(DifficultyConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Set the current progression point.
    /// For time-based: elapsed / totalDuration.
    /// For skill-based: playerSkill / maxSkill.
    /// </summary>
    /// <param name="normalized">Value clamped to 0-1.</param>
    public void SetProgress(float normalized)
    {
        _normalizedProgress = Mathf.Clamp01(normalized);
        OnDifficultyChanged?.Invoke();
    }

    /// <summary>Swap the active config (e.g., when changing levels or difficulty presets).</summary>
    public void SetConfig(DifficultyConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        OnDifficultyChanged?.Invoke();
    }
}
```

### Example: Spawner Using DifficultyProvider

```csharp
using UnityEngine;

/// <summary>
/// Example enemy spawner that reads all tuning values from the DifficultyProvider
/// instead of using hardcoded magic numbers. Designers tune via the DifficultyConfig SO.
/// </summary>
public class DifficultyAwareSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private DifficultyProvider _difficulty;
    private int _activeEnemies;
    private float _spawnTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
    }

    private void Start()
    {
        // Get from Service Locator (registered at bootstrap)
        _difficulty = Services.Get<DifficultyProvider>();
    }

    private void Update()
    {
        if (_difficulty == null)
            return;

        // Update progression based on session time
        var session = SessionManager.Instance?.CurrentSession;

        if (session != null && session.Config.TimeLimitSeconds > 0)
            _difficulty.SetProgress(session.ElapsedTime / session.Config.TimeLimitSeconds);

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _difficulty.SpawnInterval
            && _activeEnemies < _difficulty.MaxEnemies)
        {
            SpawnEnemy();
            _spawnTimer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        var enemy = Instantiate(enemyPrefab, point.position, point.rotation);

        // Apply difficulty scaling to the spawned enemy
        if (enemy.TryGetComponent<EnemyStats>(out var stats))
        {
            stats.HealthMultiplier = _difficulty.EnemyHealthMult;
            stats.DamageMultiplier = _difficulty.EnemyDamageMult;
        }

        _activeEnemies++;
    }

    /// <summary>Call when an enemy is destroyed to update the count.</summary>
    public void NotifyEnemyDestroyed()
    {
        _activeEnemies = Mathf.Max(0, _activeEnemies - 1);
    }
}
```

---

## Complete PacingDirector

### PacingCurveConfig ScriptableObject

```csharp
using UnityEngine;

/// <summary>
/// Defines the target intensity curve for a session's pacing.
/// Designers shape tension/release cycles by editing the curve in the Inspector.
/// X axis: normalized session time (0-1). Y axis: target intensity (0-1).
/// </summary>
[CreateAssetMenu(menuName = "Game Loop/Pacing Curve Config")]
public class PacingCurveConfig : ScriptableObject
{
    [Tooltip("Target intensity over normalized session time. Y: 0=calm, 1=max intensity.")]
    [SerializeField] private AnimationCurve intensityCurve = new(
        new Keyframe(0f, 0.1f),     // gentle start
        new Keyframe(0.15f, 0.4f),  // first ramp
        new Keyframe(0.25f, 0.15f), // first rest
        new Keyframe(0.4f, 0.6f),   // second ramp
        new Keyframe(0.5f, 0.25f),  // midpoint rest
        new Keyframe(0.65f, 0.7f),  // third ramp
        new Keyframe(0.75f, 0.35f), // pre-climax rest
        new Keyframe(0.9f, 0.9f),   // climax ramp
        new Keyframe(1f, 1f)        // peak
    );

    [Tooltip("Intensity thresholds that fire events when crossed upward")]
    [SerializeField] private float[] intensityThresholds = { 0.25f, 0.5f, 0.75f, 0.9f };

    [Tooltip("Total expected session duration in seconds, for normalizing elapsed time")]
    [SerializeField] private float sessionDurationSeconds = 600f;

    [Tooltip("Seconds of cooldown before a threshold can fire again after intensity drops below it")]
    [SerializeField] private float thresholdResetCooldown = 10f;

    public AnimationCurve IntensityCurve => intensityCurve;
    public float[] IntensityThresholds => intensityThresholds;
    public float SessionDurationSeconds => sessionDurationSeconds;
    public float ThresholdResetCooldown => thresholdResetCooldown;
}
```

### PacingDirector MonoBehaviour

```csharp
using System;
using UnityEngine;

/// <summary>
/// Reads a designer-authored pacing curve and fires intensity events that
/// encounter spawners, music systems, and VFX systems consume.
/// Uses <see cref="Time.realtimeSinceStartup"/> so pausing does not break the curve.
/// </summary>
public class PacingDirector : MonoBehaviour
{
    [SerializeField] private PacingCurveConfig config;

    // Per-threshold state: fired flag and cooldown timer
    private bool[] _thresholdActive;
    private float[] _thresholdCooldownTimers;

    private float _sessionStartTime;
    private float _currentIntensity;
    private float _previousIntensity;
    private float _normalizedTime;
    private bool _isActive;

    /// <summary>Current intensity value (0-1).</summary>
    public float CurrentIntensity => _currentIntensity;

    /// <summary>Normalized session progress (0-1).</summary>
    public float NormalizedTime => _normalizedTime;

    /// <summary>True if the director is actively tracking pacing.</summary>
    public bool IsActive => _isActive;

    /// <summary>Fires every frame with the current intensity value.</summary>
    public event Action<float> OnIntensityChanged;

    /// <summary>
    /// Fires when an intensity threshold is crossed upward.
    /// Arg: the threshold value that was crossed.
    /// </summary>
    public event Action<float> OnThresholdCrossed;

    /// <summary>
    /// Fires when intensity drops below a threshold after cooldown.
    /// Arg: the threshold value that was reset.
    /// </summary>
    public event Action<float> OnThresholdReset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
    }

    private void Update()
    {
        if (!_isActive || config == null)
            return;

        float elapsed = Time.realtimeSinceStartup - _sessionStartTime;
        _normalizedTime = Mathf.Clamp01(elapsed / config.SessionDurationSeconds);

        _previousIntensity = _currentIntensity;
        _currentIntensity = Mathf.Clamp01(config.IntensityCurve.Evaluate(_normalizedTime));
        OnIntensityChanged?.Invoke(_currentIntensity);

        UpdateThresholds();
    }

    /// <summary>Begin tracking pacing for a new session.</summary>
    public void BeginSession()
    {
        if (config == null)
        {
            GameDebug.LogError("[PacingDirector] No PacingCurveConfig assigned.");
            return;
        }

        _sessionStartTime = Time.realtimeSinceStartup;
        _currentIntensity = 0f;
        _previousIntensity = 0f;
        _normalizedTime = 0f;
        _isActive = true;

        int count = config.IntensityThresholds.Length;
        _thresholdActive = new bool[count];
        _thresholdCooldownTimers = new float[count];
    }

    /// <summary>Stop tracking pacing.</summary>
    public void EndSession()
    {
        _isActive = false;
    }

    /// <summary>
    /// Swap the pacing config at runtime (e.g., entering a boss area
    /// that has its own pacing curve).
    /// </summary>
    /// <param name="newConfig">New pacing config.</param>
    /// <param name="shouldResetTime">If true, resets the session start time.</param>
    public void SetConfig(PacingCurveConfig newConfig, bool shouldResetTime = false)
    {
        config = newConfig;

        if (shouldResetTime && _isActive)
            BeginSession();
    }

    private void UpdateThresholds()
    {
        float[] thresholds = config.IntensityThresholds;

        for (int i = 0; i < thresholds.Length; i++)
        {
            float threshold = thresholds[i];

            if (!_thresholdActive[i])
            {
                if (_currentIntensity >= threshold
                    && _previousIntensity < threshold)
                {
                    _thresholdActive[i] = true;
                    _thresholdCooldownTimers[i] = 0f;
                    OnThresholdCrossed?.Invoke(threshold);
                }

                continue;
            }

            // Still above threshold: the reset cooldown only counts while intensity stays below it
            if (_currentIntensity >= threshold)
            {
                _thresholdCooldownTimers[i] = 0f;
                continue;
            }

            _thresholdCooldownTimers[i] += Time.unscaledDeltaTime;

            if (_thresholdCooldownTimers[i] < config.ThresholdResetCooldown)
                continue;

            _thresholdActive[i] = false;
            _thresholdCooldownTimers[i] = 0f;
            OnThresholdReset?.Invoke(threshold);
        }
    }
}
```

### Example: Encounter Spawner Subscribing to PacingDirector

```csharp
using UnityEngine;

/// <summary>
/// Spawns encounter waves based on the PacingDirector's intensity thresholds.
/// Each threshold crossing triggers a wave of appropriate size.
/// </summary>
public class PacingEncounterSpawner : MonoBehaviour
{
    [SerializeField] private PacingDirector pacingDirector;
    [SerializeField] private GameObject[] smallWavePrefabs;
    [SerializeField] private GameObject[] mediumWavePrefabs;
    [SerializeField] private GameObject[] largeWavePrefabs;
    [SerializeField] private GameObject[] bossWavePrefabs;
    [SerializeField] private Transform spawnRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
    }

    private void OnEnable()
    {
        if (pacingDirector != null)
            pacingDirector.OnThresholdCrossed += HandleThresholdCrossed;
    }

    private void OnDisable()
    {
        if (pacingDirector != null)
            pacingDirector.OnThresholdCrossed -= HandleThresholdCrossed;
    }

    private void HandleThresholdCrossed(float threshold)
    {
        // Map thresholds to wave sizes
        GameObject[] wavePrefabs = threshold switch
        {
            >= 0.9f => bossWavePrefabs,
            >= 0.75f => largeWavePrefabs,
            >= 0.5f => mediumWavePrefabs,
            _ => smallWavePrefabs
        };

        if (wavePrefabs == null || wavePrefabs.Length == 0)
            return;

        var prefab = wavePrefabs[Random.Range(0, wavePrefabs.Length)];
        Instantiate(prefab, spawnRoot.position, Quaternion.identity, spawnRoot);
        GameDebug.Log($"[PacingEncounterSpawner] Spawned wave at threshold {threshold:F2}");
    }
}
```

---

## Memory Lifecycle Diagram

```
SESSION LIFECYCLE & META PROGRESSION FLOW
==========================================

  ┌─────────────────────────────────────────────────────────────────────┐
  │                        APPLICATION LIFETIME                         │
  │                                                                     │
  │  ┌──────────────────┐     Persists across sessions:                │
  │  │ MetaProgression   │     - Currency, XP, Account Level           │
  │  │ Service           │     - Unlocked items                        │
  │  │ (Service Locator) │     - Save/Load via unity-save-system       │
  │  └────────┬─────────┘                                              │
  │           │                                                         │
  │           │  Receives SessionResult                                │
  │           │  Applies MetaReward SOs                                │
  │           ▼                                                         │
  │  ┌─────────────────────────────────────────────────────────┐       │
  │  │                   SESSION N                              │       │
  │  │                                                          │       │
  │  │  SessionManager.StartSession(config)                     │       │
  │  │         │                                                │       │
  │  │         ▼                                                │       │
  │  │  ┌─────────────┐    ┌──────────────┐                    │       │
  │  │  │ SessionData  │    │ Difficulty    │                    │       │
  │  │  │ - Score      │    │ Provider      │                    │       │
  │  │  │ - Lives      │    │ (from config) │                    │       │
  │  │  │ - Timer      │    └──────┬───────┘                    │       │
  │  │  └──────┬──────┘           │                             │       │
  │  │         │                   │                             │       │
  │  │         ▼                   ▼                             │       │
  │  │  ┌─────────────────────────────────────────────┐        │       │
  │  │  │           GAME LOOP PHASES                   │        │       │
  │  │  │                                              │        │       │
  │  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐    │        │       │
  │  │  │  │ Phase 1  │─▶│ Phase 2  │─▶│ Phase 3  │───┐│        │       │
  │  │  │  │ Explore  │  │ Collect  │  │ Combat   │   ││        │       │
  │  │  │  └─────────┘  └─────────┘  └─────────┘   ││        │       │
  │  │  │         ▲                                  │ │        │       │
  │  │  │         └──────── loop ────────────────────┘ │        │       │
  │  │  │                                              │        │       │
  │  │  │  PacingDirector ──▶ intensity events         │        │       │
  │  │  │  ConditionEvaluator ──▶ win/lose events      │        │       │
  │  │  └──────────────────────────────────────────────┘        │       │
  │  │         │                                                │       │
  │  │         │ Win or Lose                                    │       │
  │  │         ▼                                                │       │
  │  │  SessionManager.EndSession(victory)                      │       │
  │  │         │                                                │       │
  │  │         ▼                                                │       │
  │  │  SessionResult { Config, Victory, Score, Time }          │       │
  │  │                                                          │       │
  │  └──────────────────────────┬───────────────────────────────┘       │
  │                             │                                       │
  │                             ▼                                       │
  │                   MetaProgressionService                            │
  │                   .ProcessSessionResult()                           │
  │                             │                                       │
  │                             ▼                                       │
  │                   Apply rewards:                                    │
  │                   - CurrencyReward.Apply()                         │
  │                   - XPReward.Apply()                                │
  │                   - UnlockReward.Apply()                            │
  │                             │                                       │
  │                             ▼                                       │
  │                   Save meta state                                   │
  │                             │                                       │
  │                             ▼                                       │
  │                   ┌──────────────────────┐                         │
  │                   │   BETWEEN SESSIONS    │                         │
  │                   │   - Results screen     │                         │
  │                   │   - Upgrade shop       │                         │
  │                   │   - Loadout selection  │                         │
  │                   │   - Next level select  │                         │
  │                   └──────────┬─────────────┘                         │
  │                              │                                       │
  │                              ▼                                       │
  │                   SESSION N+1 begins...                             │
  │                                                                     │
  └─────────────────────────────────────────────────────────────────────┘

MEMORY OWNERSHIP:
  MetaProgressionService  ── lives in Service Locator, survives scenes
  SessionManager          ── DontDestroyOnLoad MonoBehaviour
  SessionData             ── created/destroyed per session (NOT persisted)
  GameLoopController      ── per-scene MonoBehaviour
  PacingDirector          ── per-scene MonoBehaviour
  ConditionEvaluator      ── per-scene MonoBehaviour
  DifficultyProvider      ── lives in Service Locator, config swapped per level
```
