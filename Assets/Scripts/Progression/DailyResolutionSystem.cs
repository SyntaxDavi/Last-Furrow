using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class DailyResolutionSystem : MonoBehaviour
{
    private DailyResolutionContext _logicContext;
    private DailyVisualContext _visualContext;
    private IDailyFlowBuilder _pipelineBuilder;

    private bool _isInitialized = false;      
    private bool _isProcessing = false;       

    /// <summary>
    /// Injeção de Dependências Explícita (chamado pelo Bootstrapper da cena).
    /// </summary>
    public void Construct(
        DailyResolutionContext logicContext,  
        DailyVisualContext visualContext,     
        IDailyFlowBuilder pipelineBuilder)    
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

        Debug.Log($"[DailyResolution] ✓ Construct OK - Builder: {_pipelineBuilder?.GetType().Name}");
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

        // 1. Constrói o Pipeline
        var pipeline = _pipelineBuilder.BuildPipeline(_logicContext, _visualContext, runData);
        
        // 2. Executa via Executor Gerenciável
        var executor = new PipelineExecutor("DailyResolution");

        await executor.ExecuteAsync(pipeline, 
            onStepFinished: (step, duration) => {
                if (duration > 2f) Debug.LogWarning($"[DailyResolution] ⚠ {step.GetStepName()} está lento! ({duration:F3}s)");
            });

        // 3. Finaliza
        // Nota: TriggerResolutionEnded() é chamado pela CameraUnfocusPhase dentro do pipeline
        Debug.Log("[DailyResolution] ✓ Resolução do Dia Concluída");

        _isProcessing = false;
    }
}
