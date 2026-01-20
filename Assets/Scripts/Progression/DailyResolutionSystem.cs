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

        // 1. Cria o Token de Controle
        var flowControl = new FlowControl();

        var pipeline = new List<IFlowStep>
        {
            new GrowGridStep(AppCore.Instance.GetGridLogic(), _events, _inputManager, runData),
            
            // ? NOVO: Detectar padrões APÓS crescimento, ANTES de calcular score
            new DetectPatternsStep(
                AppCore.Instance.GetGridLogic(),
                AppCore.Instance.PatternDetector,
                AppCore.Instance.PatternCalculator,
                runData,
                _events
            ),
            
            new CalculateScoreStep(_goalSystem, _runManager, runData, _events.Progression),
            new AdvanceTimeStep(_runManager, _saveManager),
            new DailyDrawStep(_handSystem, _runManager, runData)
        };

        // --- EXECUÇÃO SEGURA ---
        foreach (var step in pipeline)
        {
            // Passa o token para o passo
            yield return step.Execute(flowControl);

            // CHECKPOINT: Se alguém apertou o botão vermelho, PARAR TUDO.
            if (flowControl.ShouldAbort)
            {
                Debug.Log("[DailyResolution] Pipeline abortado por solicitação de um Step.");
                break; // Sai do loop foreach imediatamente
            }
        }

        // Se abortou, talvez não devamos disparar 'ResolutionEnded' se a tela de GameOver já assumiu.
        // Mas geralmente, liberar o estado é seguro.
        if (!flowControl.ShouldAbort)
        {
            _events.Time.TriggerResolutionEnded();
            Debug.Log("=== Resolução do Dia Concluída ===");
        }

        _isProcessing = false;
    }
}