using UnityEngine;

public class WeeklyGoalSystem
{
    private readonly IGameLibrary _library;
    private readonly ProgressionEvents _events;
    private readonly ProgressionSettingsSO _settings;

    public WeeklyGoalSystem(IGameLibrary library, ProgressionEvents events, ProgressionSettingsSO settings)
    {
        _library = library;
        _events = events;
        _settings = settings;
    }

    // 1. Cálculo de Pontos (Mantém igual, mas é bom separar leitura de escrita no futuro)
    public void ProcessNightlyScoring(RunData runData)
    {
        int dailyPoints = 0;

        foreach (var slot in runData.GridSlots)
        {
            if (!slot.IsEmpty && !slot.IsWithered && slot.CropID.IsValid)
            {
                if (_library.TryGetCrop(slot.CropID, out CropData data))
                {
                    float score = data.BasePassiveScore;
                    if (slot.CurrentGrowth >= data.DaysToMature)
                    {
                        score *= data.MatureScoreMultiplier;
                    }
                    dailyPoints += Mathf.RoundToInt(score);
                }
            }
        }

        if(IsWeekend(runData)) return;

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

    // 2. Avaliação da Semana (Refatorado para não mexer em Vidas)
    public WeekEvaluationResult CheckEndOfProduction(RunData runData)
    {
        // Matemática do Ciclo:
        // Se hoje é dia 5, e o ciclo é 7 dias. (5-1) % 7 = 4. (Produção 5-1) = 4. Bateu!
        // Se hoje é dia 7 (Domingo). (7-1) % 7 = 6. Não bate.

        int currentDayIndex = (runData.CurrentDay - 1) % _settings.DaysPerCycle;
        int lastProductionDayIndex = _settings.ProductionDays - 1;

        bool isEndOfProduction = (currentDayIndex == lastProductionDayIndex);

        if (!isEndOfProduction)
            return WeekEvaluationResult.NotEndOfWeek();

        // Chegou no fim da Sexta-feira (Dia 5): HORA DO JULGAMENTO

        bool success = runData.CurrentWeeklyScore >= runData.WeeklyGoalTarget;
        int currentScore = runData.CurrentWeeklyScore;
        int currentTarget = runData.WeeklyGoalTarget;

        int nextWeek = runData.CurrentWeek + 1;
        int nextGoal = _settings.GetGoalForWeek(nextWeek);

        return WeekEvaluationResult.Finished(success, currentScore, currentTarget, nextGoal);
    }
    private bool IsWeekend(RunData runData)
    {
        int currentDayIndex = (runData.CurrentDay - 1) % _settings.DaysPerCycle;
        return currentDayIndex >= _settings.ProductionDays;
    }
}