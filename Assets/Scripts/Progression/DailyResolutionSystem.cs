using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DailyResolutionSystem : MonoBehaviour
{
    // Estado interno
    private bool _isProcessing = false;
    private bool _isInitialized = false;

    // Dependências (Injetadas pelo AppCore ou cacheadas)
    private RunManager _runManager;
    private SaveManager _saveManager;
    private InputManager _inputManager;
    private GameEvents _events;
    private DailyHandSystem _handSystem;
    private WeeklyGoalSystem _goalSystem;

    public void Initialize()
    {
        if (AppCore.Instance != null)
        {
            _runManager = AppCore.Instance.RunManager;
            _saveManager = AppCore.Instance.SaveManager;
            _inputManager = AppCore.Instance.InputManager;
            _events = AppCore.Instance.Events;
            _handSystem = AppCore.Instance.DailyHandSystem;
            _goalSystem = AppCore.Instance.WeeklyGoalSystem;

            _isInitialized = true;
        }
        else
        {
            Debug.LogError("[DailyResolution] FATAL: AppCore não encontrado.");
        }
    }

    public void StartEndDaySequence()
    {
        // 1. Fail Fast (Boot Seguro)
        if (!_isInitialized)
        {
            Debug.LogError("[DailyResolution] Erro: Sistema não inicializado. Verifique a ordem de boot.");
            return;
        }

        if (_isProcessing) return;

        var runData = _saveManager.Data.CurrentRun;
        if (runData == null) return;

        StartCoroutine(ExecuteDayRoutine(runData));
    }

    private IEnumerator ExecuteDayRoutine(RunData runData)
    {
        _isProcessing = true;
        _events.Time.TriggerResolutionStarted();

        // --- CONSTRUÇÃO DO PIPELINE (ON THE FLY) ---
        // Aqui definimos a "Playlist" do fim do dia.
        // No futuro, isso pode ir para um Builder (IDailyFlowBuilder).

        var pipeline = new List<IFlowStep>
        {
            // 1. Crescimento das Plantas
            new GrowGridStep(AppCore.Instance.GetGridLogic(), _events, _inputManager, runData),
            
            // 2. Cálculo de Pontos & Metas (Poderia ser um Step separado também)
            // Por enquanto, mantivemos inline para não criar 10 arquivos hoje, mas idealmente seria CalculateScoreStep
            new LambdaStep(() => ProcessScoring(runData)), 

            // 3. Avanço de Tempo e Save
            new AdvanceTimeStep(_runManager, _saveManager),

            // 4. Cartas (Draw ou Skip)
            new DailyDrawStep(_handSystem, _runManager, runData)
        };

        // --- EXECUÇÃO ---
        foreach (var step in pipeline)
        {
            yield return step.Execute();
        }

        // Finalização
        _events.Time.TriggerResolutionEnded();
        _isProcessing = false;
        Debug.Log("=== Resolução do Dia Concluída ===");
    }

    // Lógica auxiliar encapsulada para o passo "Lambda"
    // No futuro, mova isso para um CalculateScoreStep.cs
    private IEnumerator ProcessScoring(RunData runData)
    {
        _goalSystem.ProcessNightlyScoring(runData);

        var result = _goalSystem.CheckEndOfProduction(runData);
        if (result.IsWeekEnd)
        {
            // Lógica de feedback de meta (Vitória/Derrota)
            runData.WeeklyGoalTarget = result.NextGoal; 
            runData.CurrentWeeklyScore = 0;

            _events.Progression.TriggerWeeklyGoalEvaluated(result.IsSuccess, runData.CurrentLives);
            yield return new WaitForSeconds(2.0f);
        }
        yield return null;
    }
}

// Pequeno Helper para rodar métodos simples como passos
public class LambdaStep : IFlowStep
{
    private readonly System.Func<IEnumerator> _action;
    public LambdaStep(System.Func<IEnumerator> action) => _action = action;
    public IEnumerator Execute() => _action();
}