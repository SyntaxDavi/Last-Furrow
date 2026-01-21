using Cysharp.Threading.Tasks;
using UnityEngine;

public class CalculateScoreStep : IFlowStep
{
    private readonly WeeklyGoalSystem _goalSystem;
    private readonly RunManager _runManager;
    private readonly RunData _runData;
    private readonly ProgressionEvents _events;

    private const float CARRY_OVER_PERCENTAGE = 0.15f;

    public CalculateScoreStep(WeeklyGoalSystem goalSystem, RunManager runManager, RunData runData, ProgressionEvents events)
    {
        _goalSystem = goalSystem;
        _runManager = runManager;
        _runData = runData;
        _events = events;
    }

    public async UniTask Execute(FlowControl control)
    {
        _goalSystem.ProcessNightlyScoring(_runData);
        var result = _goalSystem.CheckEndOfProduction(_runData);

        if (result.IsWeekEnd)
        {
            ApplyWeekResult(result, control);

            // Espera para ler o relatório (3 segundos = 3000ms)
            if (!control.ShouldAbort)
            {
                await UniTask.Delay(3000);
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
        }

        // CRUCIAL: Atualiza a UI imediatamente com a nova meta e o novo score
        _events.TriggerScoreUpdated(_runData.CurrentWeeklyScore, _runData.WeeklyGoalTarget);
    }

    private void HandleSuccess(WeekEvaluationResult result)
    {
        Debug.Log("<color=green>SUCESSO TOTAL! Semana Avançada.</color>");

        // 1. Zera score
        _runData.CurrentWeeklyScore = 0;

        // 2. Define nova meta (mais difícil)
        _runData.WeeklyGoalTarget = result.NextGoal;

        // 3. Feedback Visual (Vitória)
        _events.TriggerWeeklyGoalEvaluated(true, _runData.CurrentLives);
    }

    private void HandlePartialFail(WeekEvaluationResult result, FlowControl control)
    {
        Debug.Log("<color=yellow>FALHA PARCIAL! Mantendo 15% do score.</color>");

        // 1. Perde vida
        _runData.CurrentLives--;
        _events.TriggerLivesChanged(_runData.CurrentLives);

        if (CheckGameOver(control)) return;

        // 2. Carry Over (Bônus de "Quase lá")
        int carryOver = Mathf.RoundToInt(_runData.CurrentWeeklyScore * CARRY_OVER_PERCENTAGE);
        _runData.CurrentWeeklyScore = carryOver;

        // 3. Mantém a mesma meta (Retry)
        _runData.WeeklyGoalTarget = result.NextGoal;

        // 4. Feedback Visual (Derrota)
        _events.TriggerWeeklyGoalEvaluated(false, _runData.CurrentLives);
    }

    private void HandleCriticalFail(WeekEvaluationResult result, FlowControl control)
    {
        Debug.Log("<color=red>FALHA CRÍTICA! Score zerado.</color>");

        // 1. Perde vida
        _runData.CurrentLives--;
        _events.TriggerLivesChanged(_runData.CurrentLives);

        if (CheckGameOver(control)) return;

        // 2. Zera tudo (Punição)
        _runData.CurrentWeeklyScore = 0;
        _runData.WeeklyGoalTarget = result.NextGoal;

        // 3. Feedback Visual (Derrota)
        _events.TriggerWeeklyGoalEvaluated(false, _runData.CurrentLives);
    }

    private bool CheckGameOver(FlowControl control)
    {
        if (_runData.CurrentLives <= 0)
        {
            _runManager.EndRun(RunEndReason.HarvestFailed);
            control.AbortPipeline();
            return true;
        }
        return false;
    }
}