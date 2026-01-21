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
        // Builder constrói pipeline baseado em contextos
        var pipeline = _pipelineBuilder.BuildPipeline(_logicContext, _visualContext, runData);
        
        Debug.Log($"[DailyResolution] Executando pipeline: {pipeline.Count} steps");

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