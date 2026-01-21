using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class DailyResolutionSystem : MonoBehaviour
{
    private AnalyzingPhaseController _analyzingController;
    private GridSlotScanner _gridSlotScanner;

    // --- DEPENDÊNCIAS DE LÓGICA (Injetadas via Code) ---
    private DailyResolutionContext _ctx;
    private bool _isInitialized = false;
    private bool _isProcessing = false;

    // Método de Injeção (Chamado pelo Bootloader/AppCore)
    public void Construct(
       DailyResolutionContext context,
       AnalyzingPhaseController visualController, 
       GridSlotScanner scannerController        
   )
    {
        _ctx = context;

        // Guardamos as referências que vieram da cena
        _analyzingController = visualController;
        _gridSlotScanner = scannerController;

        _isInitialized = true;

        // Logs de verificação (Debug)
        if (_analyzingController == null) Debug.LogWarning("[DailyResolution] Rodando sem AnalyzingController (Modo Headless?)");
        if (_gridSlotScanner == null) Debug.LogWarning("[DailyResolution] Rodando sem GridSlotScanner (Modo Headless?)");
    }

    public void StartEndDaySequence()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[DailyResolution] FATAL: Dependências não injetadas via Construct()!");
            return;
        }

        if (_isProcessing) return;

        var runData = _ctx.SaveManager.Data.CurrentRun;
        if (runData == null) return;

        ExecuteDayRoutine(runData).Forget();
    }

    private async UniTaskVoid ExecuteDayRoutine(RunData runData)
    {
        _isProcessing = true;
        _ctx.Events.Time.TriggerResolutionStarted();

        var flowControl = new FlowControl();

        // --- CONSTRUÇÃO DO PIPELINE COM INJEÇÃO EXPLÍCITA ---
        // Note como passamos exatamente o que cada step precisa, misturando Contexto e Referências de Cena

        var pipeline = new List<IFlowStep>
        {
            // GrowGridStep agora recebe o controller visual explicitamente
            new GrowGridStep(_ctx.GridService, _ctx.Events, _ctx.InputManager, runData, _analyzingController),
            
            // DetectPatternsStep recebe tracking service e scanner visual explicitamente
            new DetectPatternsStep(
                _ctx.GridService,
                _ctx.PatternDetector,
                _ctx.PatternCalculator,
                _ctx.PatternTracking, 
                runData,
                _ctx.Events,
                _gridSlotScanner      
            ),

            new CalculateScoreStep(_ctx.GoalSystem, _ctx.RunManager, runData, _ctx.Events.Progression),
            new AdvanceTimeStep(_ctx.RunManager, _ctx.SaveManager),
            new DailyDrawStep(_ctx.HandSystem, _ctx.RunManager, runData)
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
            _ctx.Events.Time.TriggerResolutionEnded();
            Debug.Log("=== Resolução do Dia Concluída ===");
        }

        _isProcessing = false;
    }
}