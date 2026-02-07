using Cysharp.Threading.Tasks;
using UnityEngine;

public class CalculateScoreStep : IFlowStep
{
    private readonly WeeklyGoalSystem _goalSystem;
    private readonly RunManager _runManager;
    private readonly RunData _runData;
    private readonly ProgressionEvents _events;
    private readonly IGameStateProvider _gameStateProvider;

    private const float CARRY_OVER_PERCENTAGE = 0.15f;
    private const int RESULT_DISPLAY_DURATION_MS = 3000;

    public CalculateScoreStep(
        WeeklyGoalSystem goalSystem,
        RunManager runManager,
        RunData runData,
        ProgressionEvents events,
        IGameStateProvider gameStateProvider = null)
    {
        _goalSystem = goalSystem;
        _runManager = runManager;
        _runData = runData;
        _events = events;
        _gameStateProvider = gameStateProvider ?? AppCore.Instance?.GameStateManager;
    }

    public async UniTask Execute(FlowControl control)
    {
        // REMOVIDO: ProcessNightlyScoring duplicava a contagem de pontos
        // Score agora é aplicado exclusivamente em DetectPatternsStep
        var result = _goalSystem.CheckEndOfProduction(_runData);

        if (result.IsWeekEnd)
        {
            // BLOQUEIO: Muda para estado ShowingResult antes de mostrar mensagem
            var previousState = _gameStateProvider?.CurrentState ?? GameState.Playing;
            _gameStateProvider?.SetState(GameState.ShowingResult);

            ApplyWeekResult(result, control);

            // Espera para ler o relatório (3 segundos)
            if (!control.ShouldAbort)
            {
                await UniTask.Delay(RESULT_DISPLAY_DURATION_MS);
            }

            // DESBLOQUEIO: Restaura estado (se não for Game Over)
            if (!control.ShouldAbort && _runData.CurrentLives > 0)
            {
                _gameStateProvider?.SetState(previousState);
            }
        }
    }

    private void ApplyWeekResult(WeekEvaluationResult result, FlowControl control)
    {
        switch (result.ResultType)
        {
            case WeekResultType.Success:
                HandleSuccess(result);
                break;

            case WeekResultType.PartialFail:
                HandlePartialFail(result, control);
                break;

            case WeekResultType.CriticalFail:
                HandleCriticalFail(result, control);
                break;

            case WeekResultType.Victory:
                HandleVictory(control);
                break;
        }

        // CRUCIAL: Atualiza a UI imediatamente com a nova meta e o novo score
        _events.TriggerScoreUpdated(_runData.CurrentWeeklyScore, _runData.WeeklyGoalTarget);
    }

    private void HandleSuccess(WeekEvaluationResult result)
    {
        Debug.Log("<color=green>SUCESSO TOTAL! Semana Avançada.</color>");

        _runData.CurrentWeeklyScore = 0;
        _runData.WeeklyGoalTarget = result.NextGoal;
        _events.TriggerWeeklyGoalEvaluated(true, _runData.CurrentLives);
    }

    private void HandlePartialFail(WeekEvaluationResult result, FlowControl control)
    {
        Debug.Log("<color=yellow>FALHA PARCIAL! Mantendo 15% do score.</color>");

        _runData.CurrentLives--;
        _events.TriggerLivesChanged(_runData.CurrentLives);

        if (CheckGameOver(control)) return;

        int carryOver = Mathf.RoundToInt(_runData.CurrentWeeklyScore * CARRY_OVER_PERCENTAGE);
        _runData.CurrentWeeklyScore = carryOver;
        _runData.WeeklyGoalTarget = result.NextGoal;
        _events.TriggerWeeklyGoalEvaluated(false, _runData.CurrentLives);
    }

    private void HandleCriticalFail(WeekEvaluationResult result, FlowControl control)
    {
        Debug.Log("<color=red>FALHA CRÍTICA! Score zerado.</color>");

        _runData.CurrentLives--;
        _events.TriggerLivesChanged(_runData.CurrentLives);

        if (CheckGameOver(control)) return;

        _runData.CurrentWeeklyScore = 0;
        _runData.WeeklyGoalTarget = result.NextGoal;
        _events.TriggerWeeklyGoalEvaluated(false, _runData.CurrentLives);
    }

    private void HandleVictory(FlowControl control)
    {
        Debug.Log("<color=cyan>VITÓRIA! Todas as 7 semanas completadas!</color>");
        _events.TriggerVictory();
        _runManager.EndRun(RunEndReason.Victory);
        control.AbortPipeline("Victory - Run Completed");
    }

    private bool CheckGameOver(FlowControl control)
    {
        if (_runData.CurrentLives <= 0)
        {
            _runManager.EndRun(RunEndReason.HarvestFailed);
            control.AbortPipeline("Game Over - Vidas esgotadas");
            return true;
        }
        return false;
    }

    public string GetStepName() => "CalculateScoreStep";
}
