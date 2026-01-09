using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeekendFlowController : MonoBehaviour
{
    private RunManager _runManager;
    private IWeekendFlowBuilder _flowBuilder;

    private Coroutine _activeFlowRoutine;

    // INICIALIZAÇÃO SIMPLIFICADA
    // Ele não recebe mais 10 dependências, recebe apenas quem orquestra (Builder) e quem avisa (RunManager)
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
    }

    // --- REAÇÃO AOS EVENTOS ---

    private void StartWeekendSequence(RunData runData)
    {
        // Pede a receita para o Builder
        var pipeline = _flowBuilder.BuildEnterPipeline(runData);
        RunPipeline(pipeline);
    }

    private void EndWeekendSequence(RunData runData)
    {
        // Pede a receita para o Builder
        var pipeline = _flowBuilder.BuildExitPipeline(runData);
        RunPipeline(pipeline);
    }

    // --- EXECUTOR (Mantido igual) ---

    private void RunPipeline(List<IFlowStep> steps)
    {
        if (_activeFlowRoutine != null) StopCoroutine(_activeFlowRoutine);
        _activeFlowRoutine = StartCoroutine(ExecutePipelineRoutine(steps));
    }

    private IEnumerator ExecutePipelineRoutine(List<IFlowStep> steps)
    {
        foreach (var step in steps)
        {
            yield return step.Execute();
        }
        _activeFlowRoutine = null;
    }
}