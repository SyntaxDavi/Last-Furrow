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

    public bool StartEndDaySequence()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[DailyResolution] FATAL: Dependências não injetadas via Construct()!");
            return false;
        }

        if (_isProcessing) 
        {
            Debug.LogWarning("[DailyResolution] Already processing a day sequence. Ignoring request.");
            return false;
        }

        var runData = _logicContext.SaveManager.Data.CurrentRun;
        if (runData == null) 
        {
            Debug.LogError("[DailyResolution] No active run found.");
            return false;
        }

        ExecuteDayRoutine(runData).Forget();
        return true;
    }

    private async UniTaskVoid ExecuteDayRoutine(RunData runData)
    {
        _isProcessing = true;
        _logicContext.Events.Time.TriggerResolutionStarted();

        try
        {
            // 1. Constrói o Pipeline
            var pipeline = _pipelineBuilder.BuildPipeline(_logicContext, _visualContext, runData);
            
            // 2. Executa via Executor Gerenciável
            var executor = new PipelineExecutor("DailyResolution");

            await executor.ExecuteAsync(pipeline, 
                onStepFinished: (step, duration) => {
                    if (duration > 2f) Debug.LogWarning($"[DailyResolution] ⚠ {step.GetStepName()} está lento! ({duration:F3}s)");
                });
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DailyResolution] ERRO CRÍTICO no pipeline: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            // 3. Finaliza (Sinaliza o fim ABSOLUTO do ciclo para o botão de Sleep)
            _logicContext.Events.Time.TriggerResolutionSequenceComplete();
            Debug.Log("[DailyResolution] Sequence finalized.");
            _isProcessing = false;
        }
    }
}
