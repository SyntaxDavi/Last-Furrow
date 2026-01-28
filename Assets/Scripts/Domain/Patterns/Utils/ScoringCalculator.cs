using UnityEngine;

/// <summary>
/// Utilitário centralizado para cálculo de pontos de grid.
/// Garante que a lógica seja idêntica entre Data Steps e Visual Controllers.
/// </summary>
public static class ScoringCalculator
{
    public static int CalculatePassiveScore(IReadOnlyCropState slot, IGameLibrary library)
    {
        if (slot.IsEmpty || slot.IsWithered || !slot.CropID.IsValid) return 0;

        if (library.TryGetCrop(slot.CropID, out CropData data))
        {
            float score = data.BasePassiveScore;
            if (slot.CurrentGrowth >= data.DaysToMature) score *= data.MatureScoreMultiplier;
            return Mathf.RoundToInt(score);
        }

        return 0;
    }

    public static DayAnalysisResult AnalyzeDay(RunData runData, IGridService gridService, IPatternDetector detector, IGameLibrary library)
    {
        var result = new DayAnalysisResult();

        // 1. Passive Scores
        for (int i = 0; i < runData.GridSlots.Length; i++)
        {
            if (!gridService.IsSlotUnlocked(i)) continue;

            int points = CalculatePassiveScore(runData.GridSlots[i], library);
            if (points > 0)
            {
                result.AddPassiveScore(i, points);
            }
        }

        // 2. Patterns (Nota: A detecção real pode requerer mais dependências no futuro)
        // Por enquanto, os Steps já fazem a detecção, então este utilitário pode ser expandido conforme necessário.
        
        return result;
    }
}
