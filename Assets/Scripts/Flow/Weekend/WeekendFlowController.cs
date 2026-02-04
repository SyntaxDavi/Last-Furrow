using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// Orquestra os pipelines de entrada e saída do Weekend.
/// 
/// FIX: Agora escuta OnExitWeekendRequested (UI) ao invés de OnProductionStarted.
/// Isso garante que o pipeline de exit seja executado ANTES de StartNextWeek(),
/// resolvendo a race condition do botão Sleep.
/// </summary>
public class WeekendFlowController : MonoBehaviour
{
    private RunManager _runManager;
    private IWeekendFlowBuilder _flowBuilder;
    private UIEvents _uiEvents;

    // Controle de cancelamento para segurança
    private System.Threading.CancellationTokenSource _cts;

    public void Initialize(RunManager runManager, IWeekendFlowBuilder flowBuilder, UIEvents uiEvents)
    {
        _runManager = runManager;
        _flowBuilder = flowBuilder;
        _uiEvents = uiEvents;

        // Entrada no Weekend: escuta o RunManager
        _runManager.OnWeekendStarted += StartWeekendSequence;
        
        // FIX: Saída do Weekend: escuta o evento de UI (não mais OnProductionStarted)
        // O StartNextWeek agora é chamado DENTRO do pipeline via StartNextWeekStep
        _uiEvents.OnExitWeekendRequested += HandleExitWeekendRequested;
    }

    private void OnDestroy()
    {
        if (_runManager != null)
        {
            _runManager.OnWeekendStarted -= StartWeekendSequence;
        }

        if (_uiEvents != null)
        {
            _uiEvents.OnExitWeekendRequested -= HandleExitWeekendRequested;
        }

        _cts?.Cancel();
        _cts?.Dispose();
    }

    // --- REAÇÃO AOS EVENTOS ---

    private void StartWeekendSequence(RunData runData)
    {
        var pipeline = _flowBuilder.BuildEnterPipeline(runData);
        RunPipeline(pipeline).Forget();
    }

    private void HandleExitWeekendRequested()
    {
        // Pega RunData atual do SaveManager via AppCore
        var runData = AppCore.Instance?.SaveManager?.Data?.CurrentRun;
        
        if (runData == null)
        {
            Debug.LogError("[WeekendFlowController] RunData null ao tentar sair do Weekend!");
            return;
        }

        Debug.Log($"[WeekendFlowController] Exit Weekend solicitado (Semana {runData.CurrentWeek})");
        
        var pipeline = _flowBuilder.BuildExitPipeline(runData);
        RunPipeline(pipeline).Forget();
    }

    // --- EXECUTOR ---

    private async UniTaskVoid RunPipeline(List<IFlowStep> steps)
    {
        _cts?.Cancel();
        _cts = new System.Threading.CancellationTokenSource();
        var token = _cts.Token;

        var flowControl = new FlowControl();

        foreach (var step in steps)
        {
            if (this == null || token.IsCancellationRequested) return;

            await step.Execute(flowControl);

            if (flowControl.ShouldAbort)
            {
                Debug.Log("[WeekendFlow] Pipeline abortado por um Step.");
                break;
            }
        }
    }
}