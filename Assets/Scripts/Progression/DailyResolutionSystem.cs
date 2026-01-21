using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class DailyResolutionSystem : MonoBehaviour
{
    // ONDA 6.2: Builder Pattern + Context Pattern
    private DailyResolutionContext _logicContext;
    private DailyVisualContext _visualContext;
    private IDailyFlowBuilder _pipelineBuilder;  // ? Builder injetado
    
    private bool _isInitialized = false;
    private bool _isProcessing = false;

    /// <summary>
    /// Injeção de Dependências Explícita (chamado pelo AppCore após scene load).
    /// ONDA 6.2: Agora recebe Builder Pattern para construção do pipeline.
    /// </summary>
    public void Construct(
        DailyResolutionContext logicContext, 
        DailyVisualContext visualContext,
        IDailyFlowBuilder pipelineBuilder)  // ? Builder injetado
    {
        _logicContext = logicContext;
        _visualContext = visualContext;
        _pipelineBuilder = pipelineBuilder;

        _isInitialized = true;

        // Validações
        if (_logicContext == null)
        {
            Debug.LogError("[DailyResolution] FATAL: LogicContext é NULL!");
            _isInitialized = false;
        }
        
        if (_pipelineBuilder == null)
        {
            Debug.LogError("[DailyResolution] FATAL: PipelineBuilder é NULL!");
            _isInitialized = false;
        }
        
        if (_visualContext == null || !_visualContext.IsValid())
        {
            Debug.LogWarning("[DailyResolution] VisualContext inválido (Modo Headless ou faltam referências)");
        }
        
        Debug.Log($"[DailyResolution] ? Construct OK - Builder: {_pipelineBuilder?.GetType().Name}");
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
        
        // ONDA 6.2: Builder Pattern - Sistema NÃO conhece steps específicos
        var pipeline = _pipelineBuilder.BuildPipeline(_logicContext, _visualContext, runData);
        
        Debug.Log($"[DailyResolution] ========== PIPELINE START ==========");
        Debug.Log($"[DailyResolution] Executando {pipeline.Count} steps");
        
        // ONDA 6.3: Error Handling + Telemetry
        float totalDuration = 0f;
        int stepIndex = 0;

        foreach (var step in pipeline)
        {
            stepIndex++;
            string stepName = step.GetType().Name;
            
            // Telemetry: Medir tempo de execução
            float startTime = Time.realtimeSinceStartup;
            
            try
            {
                Debug.Log($"[DailyResolution] [{stepIndex}/{pipeline.Count}] Executando: {stepName}");
                
                await step.Execute(flowControl);
                
                // Telemetry: Log de duração
                float duration = Time.realtimeSinceStartup - startTime;
                totalDuration += duration;
                
                Debug.Log($"[DailyResolution] ? {stepName} concluído em {duration:F3}s");
                
                // Performance Warning
                if (duration > 2f)
                {
                    Debug.LogWarning($"[DailyResolution] ?? {stepName} está lento! ({duration:F3}s)");
                }
            }
            catch (System.Exception ex)
            {
                // Error Handling: Log completo do erro
                Debug.LogError($"[DailyResolution] ? ERRO no step {stepName}:");
                Debug.LogError($"Message: {ex.Message}");
                Debug.LogError($"StackTrace: {ex.StackTrace}");
                
                // Decidir: Abortar pipeline ou continuar?
                if (IsCriticalStep(step))
                {
                    Debug.LogError($"[DailyResolution] Step crítico falhou! Abortando pipeline.");
                    flowControl.AbortPipeline();
                    break;
                }
                else
                {
                    Debug.LogWarning($"[DailyResolution] Step não-crítico falhou. Continuando...");
                }
            }

            if (flowControl.ShouldAbort)
            {
                Debug.Log("[DailyResolution] Pipeline abortado por step.");
                break;
            }

            if (this == null || this.GetCancellationTokenOnDestroy().IsCancellationRequested)
            {
                Debug.Log("[DailyResolution] Pipeline cancelado (objeto destruído).");
                return;
            }
        }

        // Telemetry: Log final
        Debug.Log($"[DailyResolution] ========== PIPELINE END ==========");
        Debug.Log($"[DailyResolution] Tempo total: {totalDuration:F3}s");

        if (!flowControl.ShouldAbort)
        {
            _logicContext.Events.Time.TriggerResolutionEnded();
            Debug.Log("[DailyResolution] ? Resolução do Dia Concluída com Sucesso");
        }
        else
        {
            Debug.LogWarning("[DailyResolution] ?? Pipeline abortado");
        }

        _isProcessing = false;
    }
    
    /// <summary>
    /// Determina se um step é crítico (deve abortar pipeline se falhar).
    /// </summary>
    private bool IsCriticalStep(IFlowStep step)
    {
        // Steps críticos: GrowGridStep, CalculateScoreStep, AdvanceTimeStep
        return step is GrowGridStep || 
               step is CalculateScoreStep || 
               step is AdvanceTimeStep;
    }
}