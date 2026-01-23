using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Executor especializado em rodar sequências de IFlowStep.
/// Encapsula lógica de telemetria, tratamento de erros e rollback.
/// </summary>
public class PipelineExecutor
{
    private readonly string _pipelineName;

    public PipelineExecutor(string pipelineName)
    {
        _pipelineName = pipelineName;
    }

    public async UniTask ExecuteAsync(List<IFlowStep> pipeline, System.Action<IFlowStep> onStepStarted = null, System.Action<IFlowStep, float> onStepFinished = null)
    {
        var flowControl = new FlowControl();
        var executedSteps = new List<IFlowStep>();
        float totalDuration = 0f;

        Debug.Log($"[{_pipelineName}] ========== PIPELINE START ==========");
        Debug.Log($"[{_pipelineName}] Executando {pipeline.Count} steps");

        int stepIndex = 0;
        foreach (var step in pipeline)
        {
            stepIndex++;
            string stepName = step.GetStepName();

            if (!step.CanExecute())
            {
                Debug.Log($"[{_pipelineName}] [{stepIndex}/{pipeline.Count}] ↷ PULADO: {stepName}");
                continue;
            }

            onStepStarted?.Invoke(step);
            float startTime = Time.realtimeSinceStartup;

            try
            {
                Debug.Log($"[{_pipelineName}] [{stepIndex}/{pipeline.Count}] Executando: {stepName}");
                await step.Execute(flowControl);
                executedSteps.Add(step);

                float duration = Time.realtimeSinceStartup - startTime;
                totalDuration += duration;
                onStepFinished?.Invoke(step, duration);

                Debug.Log($"[{_pipelineName}] ✓ {stepName} concluído em {duration:F3}s");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{_pipelineName}] ✗ ERRO no step {stepName}: {ex.Message}\n{ex.StackTrace}");

                if (IsCriticalStep(step))
                {
                    Debug.LogError($"[{_pipelineName}] Step crítico falhou! Abortando e iniciando rollback.");
                    flowControl.AbortPipeline($"Step crítico {stepName} falhou");
                    await RollbackAsync(executedSteps);
                    break;
                }
            }

            if (flowControl.ShouldAbort) break;
        }

        Debug.Log($"[{_pipelineName}] ========== PIPELINE END (Total: {totalDuration:F3}s) ==========");
    }

    private async UniTask RollbackAsync(List<IFlowStep> executedSteps)
    {
        Debug.LogWarning($"[{_pipelineName}] ↺ Iniciando rollback de {executedSteps.Count} steps...");
        for (int i = executedSteps.Count - 1; i >= 0; i--)
        {
            var step = executedSteps[i];
            if (step is IReversibleStep reversibleStep && reversibleStep.WasExecuted)
            {
                try
                {
                    Debug.Log($"[{_pipelineName}] ↺ Rollback: {step.GetStepName()}");
                    await reversibleStep.Rollback();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[{_pipelineName}] ✗ Erro no rollback de {step.GetStepName()}: {ex.Message}");
                }
            }
        }
        Debug.LogWarning($"[{_pipelineName}] ↺ Rollback concluído.");
    }

    private bool IsCriticalStep(IFlowStep step)
    {
        // Regra simples: por padrão steps são críticos a menos que especificado contrário
        // No futuro isso pode ser uma propriedade da interface IFlowStep
        return true; 
    }
}
