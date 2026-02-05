using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// Orquestra os pipelines de entrada e saída do Weekend.
/// 
/// ARQUITETURA: Confia no Two-Phase Initialization do AppCore.
/// Os eventos OnWeekendStarted só são emitidos APÓS este controller estar subscrito.
/// </summary>
public class WeekendFlowController : MonoBehaviour
{
    private RunManager _runManager;
    private IWeekendFlowBuilder _flowBuilder;
    private UIEvents _uiEvents;
    private bool _isInitialized = false;

    private System.Threading.CancellationTokenSource _cts;

    public void Initialize(RunManager runManager, IWeekendFlowBuilder flowBuilder, UIEvents uiEvents)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[WeekendFlowController] Já inicializado!");
            return;
        }

        _runManager = runManager;
        _flowBuilder = flowBuilder;
        _uiEvents = uiEvents;

        // Subscreve aos eventos
        // GARANTIA: O evento OnWeekendStarted só será emitido APÓS esta subscrição
        // graças ao Two-Phase Initialization Pattern do AppCore.
        _runManager.OnWeekendStarted += HandleWeekendStarted;
        _uiEvents.OnExitWeekendRequested += HandleExitWeekendRequested;

        _isInitialized = true;
        Debug.Log("[WeekendFlowController] ✓ Inicializado e subscrito");
    }

    private void OnDestroy()
    {
        if (_runManager != null)
            _runManager.OnWeekendStarted -= HandleWeekendStarted;

        if (_uiEvents != null)
            _uiEvents.OnExitWeekendRequested -= HandleExitWeekendRequested;

        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void HandleWeekendStarted(RunData runData)
    {
        if (runData == null)
        {
            Debug.LogError("[WeekendFlowController] RunData null!");
            return;
        }

        Debug.Log($"[WeekendFlowController] Weekend iniciado (Semana {runData.CurrentWeek})");
        var pipeline = _flowBuilder.BuildEnterPipeline(runData);
        RunPipeline(pipeline).Forget();
    }

    private void HandleExitWeekendRequested()
    {
        var runData = AppCore.Instance?.SaveManager?.Data?.CurrentRun;
        
        if (runData == null)
        {
            Debug.LogError("[WeekendFlowController] RunData null ao sair!");
            return;
        }

        Debug.Log($"[WeekendFlowController] Exit Weekend (Semana {runData.CurrentWeek})");
        var pipeline = _flowBuilder.BuildExitPipeline(runData);
        RunPipeline(pipeline).Forget();
    }

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
                Debug.Log("[WeekendFlow] Pipeline abortado.");
                break;
            }
        }
    }
}