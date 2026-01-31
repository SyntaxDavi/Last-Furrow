# Analysis Phase Orchestration - Architecture Plan

## Current Problems

### 1. Music Bug (Pause/Resume during Analysis)
When pressing ESC during analysis, the game pauses. On resume, the analysis continues but music stops.

**Root Cause**: The music system's state machine doesn't properly handle the `Paused → Playing` transition when the game was already in `Analyzing` state.

### 2. Race Conditions in Analysis Pipeline
Multiple systems react to state changes independently with no coordination:

```
[Current Chaos - No Orchestration]
┌─────────────────────────────────────────────────────────────────┐
│  User clicks "Sleep"                                             │
│                                                                  │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐     │
│  │ Cards Lock     │  │ Camera Focus   │  │ Reorganize OFF │     │
│  │ (immediate)    │  │ (tween)        │  │ (immediate)    │     │
│  └────────────────┘  └────────────────┘  └────────────────┘     │
│          ↓                   ↓                   ↓               │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐     │
│  │ Hand Fan-Out   │  │ Grid Animation │  │ Music Change   │     │
│  │ (NEW - tween)  │  │ (sequence)     │  │ (crossfade)    │     │
│  └────────────────┘  └────────────────┘  └────────────────┘     │
│                                                                  │
│  🔥 All start at same time, no order, no sync, no completion!  │
└─────────────────────────────────────────────────────────────────┘
```

### 3. Missing Feature: Hand Fan-Out during Analysis
Cards should exit screen when analysis starts and return when it ends.

---

## Proposed Solution: AnalysisPhaseCoordinator

A **centralized orchestrator** that manages the lifecycle of the analyzing phase with explicit phases and completion callbacks.

### Design Principles
1. **Single Responsibility**: Each system only handles its own animation/logic
2. **Dependency Injection**: Coordinator receives all dependencies
3. **Async/Await Pattern**: Use UniTask for clean sequencing
4. **Event-Driven with Ordering**: Systems subscribe with priority
5. **State Machine Integrity**: Music/Game state transitions are atomic

### Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     AnalysisPhaseCoordinator                             │
│                    (The Single Source of Truth)                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Phase 1: PREPARE_ANALYSIS                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ 1.1 GameStateManager.SetState(Analyzing)  ← FIRST (blocks input)   ││
│  │ 1.2 HandManager.ForceReleaseAllDrags()    ← Safety                 ││
│  │ 1.3 await HandManager.FanOut()            ← Cards exit screen      ││
│  │ 1.4 DisableInteractiveUI()                ← Reorganize, etc        ││
│  │ 1.5 await CameraController.FocusOnGrid() ← Camera moves            ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                    ↓                                     │
│  Phase 2: EXECUTE_ANALYSIS                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ 2.1 await DetectPatternsStep.Execute()   ← Core logic + visuals    ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                    ↓                                     │
│  Phase 3: RESTORE_GAMEPLAY                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ 3.1 await DrawCardsStep.Execute()        ← New cards added         ││
│  │ 3.2 await CameraController.ReturnToDefault()                       ││
│  │ 3.3 await HandManager.FanIn()            ← Cards return to screen  ││
│  │ 3.4 EnableInteractiveUI()                                          ││
│  │ 3.5 GameStateManager.SetState(Playing)   ← LAST (enables input)    ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
│  PAUSE HANDLING:                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │ - Coordinator tracks current phase                                  ││
│  │ - On Pause: Stores _wasAnalyzing = true                            ││
│  │ - On Resume: Restores Analyzing state, NOT Playing                 ││
│  │ - Music system respects this via GameStateManager                  ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### Step 1: Create IAnalysisPhaseParticipant Interface
```csharp
public interface IAnalysisPhaseParticipant
{
    int Priority { get; } // Lower = runs first
    UniTask OnAnalysisEnter();
    UniTask OnAnalysisExit();
}
```

### Step 2: Create AnalysisPhaseCoordinator
```csharp
public class AnalysisPhaseCoordinator
{
    private readonly List<IAnalysisPhaseParticipant> _participants;
    private readonly GameStateManager _stateManager;
    
    public AnalysisPhase CurrentPhase { get; private set; }
    
    public async UniTask RunAnalysis(/* deps */)
    {
        // Phase 1: Enter
        CurrentPhase = AnalysisPhase.Entering;
        _stateManager.SetState(GameState.Analyzing);
        
        foreach (var p in _participants.OrderBy(x => x.Priority))
            await p.OnAnalysisEnter();
        
        // Phase 2: Execute
        CurrentPhase = AnalysisPhase.Executing;
        await ExecuteAnalysisLogic();
        
        // Phase 3: Exit
        CurrentPhase = AnalysisPhase.Exiting;
        
        foreach (var p in _participants.OrderByDescending(x => x.Priority))
            await p.OnAnalysisExit();
        
        CurrentPhase = AnalysisPhase.Idle;
        _stateManager.SetState(GameState.Playing);
    }
}
```

### Step 3: Implement Participants

| Participant | Priority | OnEnter | OnExit |
|-------------|----------|---------|--------|
| HandController | 10 | FanOut() + Disable | FanIn() + Enable |
| CameraController | 20 | FocusGrid() | ReturnDefault() |
| UIController | 30 | DisableButtons() | EnableButtons() |
| MusicController | 40 | (auto via state) | (auto via state) |

### Step 4: Fix Music Resume Bug
In `GameplayMusicController` or `AudioManager`:
```csharp
private void OnGameStateChanged(GameState oldState, GameState newState)
{
    // If resuming from Pause to Analyzing, music should continue/restart
    if (oldState == GameState.Paused && newState == GameState.Analyzing)
    {
        ResumeOrRestartMusic();
    }
}
```

### Step 5: Add HandManager.FanOut/FanIn
```csharp
public async UniTask FanOut()
{
    // Animate all cards to exit position (SpawnOffset but inverted)
    foreach (var card in _activeCards)
    {
        var target = _handCenter.position + _layoutConfig.FanOutOffset;
        card.UpdateLayoutTarget(/* target with exit position */);
    }
    await UniTask.Delay(_layoutConfig.FanOutDuration);
}

public async UniTask FanIn()
{
    RecalculateLayoutTargets(); // Normal positions
    await UniTask.Delay(_layoutConfig.FanInDuration);
}
```

---

## Files to Modify

1. **NEW**: `Assets/Scripts/Flow/AnalysisPhaseCoordinator.cs`
2. **NEW**: `Assets/Scripts/Flow/IAnalysisPhaseParticipant.cs`
3. **MODIFY**: `Assets/Scripts/Cards/Hand/HandManager.cs` - Add FanOut/FanIn
4. **MODIFY**: `Assets/Scripts/Audio/GameplayMusicController.cs` - Fix pause resume
5. **MODIFY**: `Assets/Scripts/Flow/Steps/DetectPatternsStep.cs` - Use coordinator
6. **MODIFY**: `Assets/Scripts/Core/State/GameStateManager.cs` - Track analysis state

---

## Edge Cases Handled

1. ✅ **Pause during Analysis**: Coordinator tracks phase, resumes correctly
2. ✅ **Multiple Sleep clicks**: State machine prevents re-entry
3. ✅ **Card drag during transition**: ForceReleaseAllDrags called first
4. ✅ **Interrupt mid-animation**: UniTask cancellation tokens
5. ✅ **Error during analysis**: try/finally ensures cleanup

---

## Alternative: Event Bus with Phases

Instead of direct calls, use events with phase tags:

```csharp
GameEvents.Analysis.OnPhaseChanged += (phase) => {
    if (phase == AnalysisPhase.Entering) { /* ... */ }
};
```

**Pros**: More decoupled
**Cons**: Harder to guarantee order, no await

**Recommendation**: Use direct orchestrator for v1, refactor to events later if needed.
