using System; 
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Orquestra a fase de an�lise usando Strategy Pattern para detec��o de padr�es.
/// Dispara eventos que outros controllers escutam (highlight, popup, breathing).
/// SOLID: Single Responsibility - apenas orquestra, detec��o delegada aos detectores.
/// Vers�o: UniTask (Async/Await)
/// </summary>
public class AnalyzingPhaseController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;

    [Header("References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PatternUIManager _uiManager;

    private List<IPatternDetector> _detectors;
    private HashSet<int> _processedSlots;

    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }

        // ONDA 6.1: Remover FindFirstObjectByType - Validar apenas (atribuir no Inspector)
        if (_uiManager == null)
        {
            Debug.LogError("[AnalyzingPhaseController] PatternUIManager n�o atribu�do no Inspector!");
        }

        // Obter todos os detectores da factory
        _detectors = PatternDetectorFactory.GetAllDetectors();
        _processedSlots = new HashSet<int>();
    }

    public async UniTask AnalyzeAndGrowGrid(
        IGridService gridService,
        GameEvents events,
        RunData runData,
        DayAnalysisResult preCalculatedResult = null)
    {
        if (_gridManager == null || _config == null)
        {
            Debug.LogError("[AnalyzingPhase] Missing references!");
            return;
        }

        _config.DebugLog("Starting grid analysis with synchronized results");

        var allSlots = _gridManager.GetComponentsInChildren<GridSlotView>();

        // 1. Process Night Cycle (Garante estado visual correto)
        for (int i = 0; i < allSlots.Length; i++)
        {
            int slotIndex = allSlots[i].SlotIndex;
            if (gridService.IsSlotUnlocked(slotIndex))
            {
                gridService.ProcessNightCycleForSlot(slotIndex);
            }
        }

        _processedSlots.Clear();
        int runningTotalScore = runData.CurrentWeeklyScore;

        // 2. Process Passive Scores (Crops)
        if (preCalculatedResult != null)
        {
            foreach (var passive in preCalculatedResult.PassiveScores)
            {
                var slotView = GetSlotViewByIndex(allSlots, passive.SlotIndex);
                if (slotView == null) continue;

                // Levitação + Popup local
                await LevitateSlot(slotView);
                slotView.ShowPassiveScore(passive.Points);

                // Atualização do HUD (Running Total)
                runningTotalScore += passive.Points;
                events.Grid.TriggerCropPassiveScore(
                    passive.SlotIndex,
                    passive.Points,
                    runningTotalScore,
                    runData.WeeklyGoalTarget
                );

                if (_config.analyzingSlotDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(_config.analyzingSlotDelay));
            }
        }

        // 3. Process Patterns
        if (preCalculatedResult != null)
        {
            foreach (var pattern in preCalculatedResult.PatternMatches)
            {
                // UI atualiza contador ENQUANTO popup aparece
                runningTotalScore += pattern.BaseScore;
                events.Pattern.TriggerScoreIncremented(
                    pattern.BaseScore,
                    runningTotalScore,
                    runData.WeeklyGoalTarget
                );

                // Disparar evento para highlights/popup
                events.Pattern.TriggerPatternSlotCompleted(pattern);

                if (_uiManager != null)
                {
                    await _uiManager.ShowPatternPopupRoutine(pattern);
                }

                // Decay/Recreation (Visual Only)
                HandleVisualEffects(pattern, events);
            }
        }

        _config.DebugLog("Grid analysis complete");
    }

    private GridSlotView GetSlotViewByIndex(GridSlotView[] views, int index)
    {
        for (int i = 0; i < views.Length; i++)
            if (views[i].SlotIndex == index) return views[i];
        return null;
    }

    private void HandleVisualEffects(PatternMatch foundPattern, GameEvents events)
    {
        if (foundPattern.DaysActive > 1)
        {
            float decayMultiplier = Mathf.Pow(0.9f, foundPattern.DaysActive - 1);
            events.Pattern.TriggerPatternDecayApplied(foundPattern, foundPattern.DaysActive, decayMultiplier);
        }

        if (foundPattern.HasRecreationBonus)
        {
            events.Pattern.TriggerPatternRecreated(foundPattern);
        }
    }

    private async UniTask LevitateSlot(GridSlotView slot)
    {
        if (slot == null) return;

        float duration = _config.levitationDuration;

        // Aciona a elevação via processador do Slot (Sistema Unificado)
        slot.SetElevationFactor(1f);
        
        // Espera metade da duração configurada
        await UniTask.Delay(TimeSpan.FromSeconds(duration / 2f));

        // Retorna ao estado original
        slot.SetElevationFactor(0f);
        
        // Espera a outra metade para garantir a descida suave antes do próximo slot
        await UniTask.Delay(TimeSpan.FromSeconds(duration / 2f));
    }
}
    
    /// <summary>
    /// ONDA 6.5: Processa pontos passivos de crop individual.
    /// Mostra popup pequeno "+X" em cima do slot e dispara evento.
    /// </summary>
