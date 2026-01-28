using System.Collections.Generic;

/// <summary>
/// Estrutura que contém todos os resultados da análise de um dia.
/// Usada para sincronizar a lógica de dados com a fase visual.
/// </summary>
public class DayAnalysisResult
{
    public List<PassiveScoreResult> PassiveScores { get; private set; } = new List<PassiveScoreResult>();
    public List<PatternMatch> PatternMatches { get; private set; } = new List<PatternMatch>();
    
    public int TotalPassivePoints { get; private set; }
    public int TotalPatternPoints { get; private set; }
    public int TotalDayPoints => TotalPassivePoints + TotalPatternPoints;

    public void AddPassiveScore(int slotIndex, int points)
    {
        PassiveScores.Add(new PassiveScoreResult { SlotIndex = slotIndex, Points = points });
        TotalPassivePoints += points;
    }

    public void SetPatterns(List<PatternMatch> matches, int points)
    {
        PatternMatches = matches;
        TotalPatternPoints = points;
    }
}

public struct PassiveScoreResult
{
    public int SlotIndex;
    public int Points;
}
