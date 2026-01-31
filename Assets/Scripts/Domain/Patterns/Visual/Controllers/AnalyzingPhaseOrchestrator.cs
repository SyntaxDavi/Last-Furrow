using System; 
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using LastFurrow.Domain.Patterns.Visual.Pipeline;
using LastFurrow.Domain.Patterns.Visual.Pipeline.Phases;

/// <summary>
/// Orquestra a fase de análise usando um Pipeline de Phases desacopladas.
/// Segue os princípios SOLID ao separar a lógica de execução da lógica de cada fase.
/// </summary>
public class AnalyzingPhaseOrchestrator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _patternConfig;
    [SerializeField] private GridVisualConfig _gridConfig;

    [Header("References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PatternUIManager _uiManager;
    [SerializeField] private HandManager _handManager;

    private List<IAnalysisPhase> _phases;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (_patternConfig == null)
            _patternConfig = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        
        if (_gridConfig == null)
            _gridConfig = Resources.Load<GridVisualConfig>("Grid/GridVisualConfig");

        if (_uiManager == null)
            Debug.LogError("[AnalyzingPhaseOrchestrator] PatternUIManager não atribuído!");

        // Montagem do Pipeline (Composition Root local)
        _phases = new List<IAnalysisPhase>
        {
            new HandFanOutPhase(_handManager),              // 1. Cards exit screen
            new CameraFocusPhase(400f),                     // 2. Wait for camera to focus on grid
            new NightCyclePhase(),                          // 3. Visual night cycle
            new PassiveScoresPhase(_gridConfig),            // 4. Passive scores animation
            new PatternAnalysisPhase(_uiManager, _patternConfig), // 5. Pattern detection
            new CameraUnfocusPhase(500f),                   // 6. Camera returns to normal + wait
            new HandFanInPhase(_handManager),               // 7. Cards return to screen
            new PostAnalysisDelayPhase(600f),               // 8. Respiro visual antes do draw
        };
    }

    /// <summary>
    /// Executa o pipeline completo de análise.
    /// </summary>
    public async UniTask<AnalysisReport> AnalyzeAndGrowGrid(
        IGridService gridService,
        GameEvents events,
        RunData runData,
        DayAnalysisResult preCalculatedResult = null,
        IProgress<PhaseProgress> progress = null)
    {
        if (_gridManager == null)
        {
            Debug.LogError("[AnalyzingPhaseOrchestrator] Missing GridManager reference!");
            return new AnalysisReport { Success = false, Error = "Missing GridManager" };
        }

        _cts = new CancellationTokenSource();
        var report = new AnalysisReport();
        var token = _cts.Token;

        try
        {
            var context = new AnalysisContext
            {
                GridService = gridService,
                RunData = runData,
                Events = events,
                PreCalculatedResult = preCalculatedResult,
                RunningScore = runData.CurrentWeeklyScore,
                SlotViews = _gridManager.GetComponentsInChildren<GridSlotView>()
            };

            Debug.Log("[AnalyzingPhaseOrchestrator] Starting Analysis Pipeline...");

            foreach (var phase in _phases)
            {
                if (token.IsCancellationRequested) break;

                Debug.Log($"[AnalyzingPhaseOrchestrator] Executing Phase: {phase.Name}");
                
                var result = await phase.ExecuteAsync(context, progress, token);
                report.PhaseResults.Add(result);

                if (!result.Success)
                {
                    Debug.LogError($"[AnalyzingPhaseOrchestrator] Phase {phase.Name} failed: {result.Message}");
                    report.Success = false;
                    report.Error = result.Message;
                    return report;
                }
            }

            report.TotalScoreDelta = context.RunningScore - runData.CurrentWeeklyScore;
            report.Success = true;
            Debug.Log($"[AnalyzingPhaseOrchestrator] Analysis Complete. Total points gained: {report.TotalScoreDelta}");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[AnalyzingPhaseOrchestrator] Analysis cancelled.");
            report.Success = false;
            report.Cancelled = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AnalyzingPhaseOrchestrator] Critical error during analysis: {ex}");
            report.Success = false;
            report.Error = ex.Message;
        }
        finally
        {
            // FALLBACK DE SEGURANÇA: Garante que cartas voltem e câmera se estabilize
            // mesmo em caso de erro ou cancelamento
            if (report.Cancelled || !report.Success)
            {
                // Força câmera a voltar se ainda não voltou
                AppCore.Instance?.Events?.Time.TriggerResolutionEnded();
                
                // Força cartas a voltarem (se ainda não voltaram)
                var fanController = _handManager?.GetFanController();
                if (fanController != null && fanController.IsFannedOut)
                {
                    await fanController.FanIn();
                }
            }
            
            _cts?.Dispose();
            _cts = null;
        }

        return report;
    }

    /// <summary>
    /// Permite interromper a análise (Skip).
    /// </summary>
    public void SkipAnalysis()
    {
        _cts?.Cancel();
    }
}
