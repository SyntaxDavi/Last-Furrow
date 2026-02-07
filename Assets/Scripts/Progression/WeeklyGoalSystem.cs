using UnityEngine;
using LastFurrow.Traditions;

public class WeeklyGoalSystem
{
    private readonly IGameLibrary _library;
    private readonly ProgressionEvents _events;
    private readonly ProgressionSettingsSO _settings;
    private readonly ITraditionService _traditionService;

    // Configuração de Game Design
    private const float PARTIAL_FAIL_THRESHOLD = 0.6f; // 60%

    public WeeklyGoalSystem(
    IGameLibrary library, 
    ProgressionEvents events, 
    ProgressionSettingsSO settings,
    ITraditionService traditionService = null)
    {
        _library = library;
        _events = events;
        _settings = settings;
        _traditionService = traditionService;
    }

    public void ProcessNightlyScoring(RunData runData)
    {
        if (IsWeekend(runData)) return;

        int dailyPoints = 0;
        foreach (var slot in runData.GridSlots)
        {
            if (!slot.IsEmpty && !slot.IsWithered && slot.CropID.IsValid)
            {
                if (_library.TryGetCrop(slot.CropID, out CropData data))
                {
                    float score = data.BasePassiveScore;
                    if (slot.CurrentGrowth >= data.DaysToMature) score *= data.MatureScoreMultiplier;
                    dailyPoints += Mathf.RoundToInt(score);
                }
            }
        }

        if (dailyPoints > 0)
        {
            runData.CurrentWeeklyScore += dailyPoints;
            _events.TriggerScoreUpdated(runData.CurrentWeeklyScore, runData.WeeklyGoalTarget);
        }
    }

    public WeekEvaluationResult CheckEndOfProduction(RunData runData)
    {
        // Verifica se é sexta-feira
        int currentDayIndex = (runData.CurrentDay - 1) % _settings.DaysPerCycle;
        if (currentDayIndex != _settings.ProductionDays - 1)
            return WeekEvaluationResult.NotEndOfWeek();

        // --- LÓGICA DE AVALIAÇÃO ---

        float ratio = (float)runData.CurrentWeeklyScore / runData.WeeklyGoalTarget;
        WeekResultType type;
        int nextGoal;

        if (ratio >= 1.0f) // SUCESSO TOTAL
        {
            // Verifica se é a última semana (VITÓRIA!)
            if (runData.CurrentWeek >= _settings.TotalWeeksToWin)
            {
                type = WeekResultType.Victory;
                nextGoal = 0; // Não há próxima meta
            }
            else
            {
                type = WeekResultType.Success;
                nextGoal = GetEffectiveGoal(runData.CurrentWeek + 1);
            }
        }
        else // FALHA
        {
            // Se falhou, a semana NÃO avança, então a meta se mantém a mesma
            // (O jogador tem que tentar bater a mesma meta de novo)
            nextGoal = _settings.GetGoalForWeek(runData.CurrentWeek);

            if (ratio >= PARTIAL_FAIL_THRESHOLD)
                type = WeekResultType.PartialFail;
            else
                type = WeekResultType.CriticalFail;
        }

        return WeekEvaluationResult.Finished(type, nextGoal);
    }

    /// <summary>
    /// Retorna a meta efetiva considerando modificadores de Traditions.
    /// </summary>
    public int GetEffectiveGoal(int weekNumber)
    {
        int baseGoal = _settings.GetGoalForWeek(weekNumber);
        
        if (_traditionService != null)
        {
            int modifier = _traditionService.GetPersistentModifier(PersistentModifierType.WeeklyGoalModifier);
            baseGoal = Mathf.Max(1, baseGoal + modifier); // Nunca menor que 1
        }
        
        return baseGoal;
    }

    private bool IsWeekend(RunData runData)
    {
        int currentDayIndex = (runData.CurrentDay - 1) % _settings.DaysPerCycle;
        return currentDayIndex >= _settings.ProductionDays;
    }
}