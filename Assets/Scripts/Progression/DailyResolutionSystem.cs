using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class DailyResolutionSystem : MonoBehaviour
{
    // ONDA 6.1: Dependency Injection via Context Pattern
    private DailyResolutionContext _logicContext;
    private DailyVisualContext _visualContext;
    
    private bool _isInitialized = false;
    private bool _isProcessing = false;

    /// <summary>
    /// Injeção de Dependências Explícita (chamado pelo AppCore após scene load).
    /// </summary>
    public void Construct(DailyResolutionContext logicContext, DailyVisualContext visualContext)
    {
        _logicContext = logicContext;
        _visualContext = visualContext;

        _isInitialized = true;

        // Validações
        if (_logicContext == null)
        {
            Debug.LogError("[DailyResolution] FATAL: LogicContext é NULL!");
            _isInitialized = false;
        }
        
        if (_visualContext == null || !_visualContext.IsValid())
        {
            Debug.LogWarning("[DailyResolution] VisualContext inválido (Modo Headless ou faltam referências)");
        }
        
        Debug.Log($"[DailyResolution] ? Construct OK - Visual Valid: {_visualContext?.IsValid()}");
    }

    public void StartEndDaySequence()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[DailyResolution] FATAL: Dependências não injetadas via Construct()!");
            return;
        }

        if (_isProcessing) return;

        var runData = _logicContext.SaveManager.Data.CurrentRun;
        if (runData == null) return;

        ExecuteDayRoutine(runData).Forget();
    }

    private async UniTaskVoid ExecuteDayRoutine(RunData runData)
    {
        _isProcessing = true;
        _logicContext.Events.Time.TriggerResolutionStarted();

        var flowControl = new FlowControl();
        var pipeline = new List<IFlowStep>
        {
            // GrowGridStep recebe controller visual via construtor
            new GrowGridStep(
                _logicContext.GridService, 
                _logicContext.Events, 
                _logicContext.InputManager, 
                runData, 
                _visualContext.Analyzer  
            ),
            
            // DetectPatternsStep recebe scanner visual via construtor
            new DetectPatternsStep(
                _logicContext.GridService,
                _logicContext.PatternDetector,
                _logicContext.PatternCalculator,
                _logicContext.PatternTracking, 
                runData,
                _logicContext.Events,
                _visualContext.Scanner  
            ),

            new CalculateScoreStep(_logicContext.GoalSystem, _logicContext.RunManager, runData, _logicContext.Events.Progression),
            new AdvanceTimeStep(_logicContext.RunManager, _logicContext.SaveManager),
            new DailyDrawStep(_logicContext.HandSystem, _logicContext.RunManager, runData)
        };

        foreach (var step in pipeline)
        {
            await step.Execute(flowControl);

            if (flowControl.ShouldAbort)
            {
                Debug.Log("[DailyResolution] Pipeline abortado.");
                break;
            }

            if (this == null || this.GetCancellationTokenOnDestroy().IsCancellationRequested)
                return;
        }

        if (!flowControl.ShouldAbort)
        {
            _logicContext.Events.Time.TriggerResolutionEnded();
            Debug.Log("=== Resolução do Dia Concluída ===");
        }

        _isProcessing = false;
    }
}