using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks; // <--- Importante

public class WeekendFlowController : MonoBehaviour
{
    private RunManager _runManager;
    private IWeekendFlowBuilder _flowBuilder;

    // Controle de cancelamento para segurança
    private System.Threading.CancellationTokenSource _cts;

    public void Initialize(RunManager runManager, IWeekendFlowBuilder flowBuilder)
    {
        _runManager = runManager;
        _flowBuilder = flowBuilder;

        _runManager.OnWeekendStarted += StartWeekendSequence;
        _runManager.OnProductionStarted += EndWeekendSequence;
    }

    private void OnDestroy()
    {
        if (_runManager != null)
        {
            _runManager.OnWeekendStarted -= StartWeekendSequence;
            _runManager.OnProductionStarted -= EndWeekendSequence;
        }

        // Cancela qualquer fluxo pendente se o objeto morrer
        _cts?.Cancel();
        _cts?.Dispose();
    }

    // --- REAÇÃO AOS EVENTOS ---

    private void StartWeekendSequence(RunData runData)
    {
        var pipeline = _flowBuilder.BuildEnterPipeline(runData);
        // Fire-and-forget seguro com UniTask
        RunPipeline(pipeline).Forget();
    }

    private void EndWeekendSequence(RunData runData)
    {
        var pipeline = _flowBuilder.BuildExitPipeline(runData);
        RunPipeline(pipeline).Forget();
    }

    // --- EXECUTOR (Atualizado para UniTask) ---

    private async UniTaskVoid RunPipeline(List<IFlowStep> steps)
    {
        // Reinicia o token de cancelamento
        _cts?.Cancel();
        _cts = new System.Threading.CancellationTokenSource();
        var token = _cts.Token;

        var flowControl = new FlowControl();

        foreach (var step in steps)
        {
            // Verifica se o objeto foi destruído antes de continuar
            if (this == null || token.IsCancellationRequested) return;

            // Aguarda o passo (que agora retorna UniTask)
            await step.Execute(flowControl);

            if (flowControl.ShouldAbort)
            {
                Debug.Log("[WeekendFlow] Pipeline abortado por um Step.");
                break;
            }
        }
    }
}