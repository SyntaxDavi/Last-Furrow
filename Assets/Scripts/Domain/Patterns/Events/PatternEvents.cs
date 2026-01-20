using System;
using System.Collections.Generic;

/// <summary>
/// Event bus para o Pattern System.
/// 
/// REGRA CRÍTICA:
/// - Eventos são apenas para OBSERVAÇÃO (UI, analytics, debug)
/// - Lógica de jogo NUNCA depende de eventos terem sido ouvidos
/// - Nenhuma decisão crítica via eventos
/// 
/// EVENTOS DISPONÍVEIS:
/// - OnPatternsDetected: Disparado após detecção completa (lista de matches + total de pontos)
/// - OnPatternScoreCalculated: Disparado após cálculo de score (para UI mostrar breakdown)
/// </summary>
public class PatternEvents
{
    /// <summary>
    /// Disparado após detecção de padrões no fim do dia.
    /// Parâmetros: (Lista de PatternMatch, Pontos totais de padrões)
    /// </summary>
    public event Action<List<PatternMatch>, int> OnPatternsDetected;
    
    /// <summary>
    /// Disparado quando um padrão individual é calculado (para UI/debug detalhado).
    /// Parâmetros: (PatternMatch, Score final calculado)
    /// </summary>
    public event Action<PatternMatch, int> OnPatternScoreCalculated;
    
    // --- Triggers ---
    
    public void TriggerPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        OnPatternsDetected?.Invoke(matches, totalPoints);
    }
    
    public void TriggerPatternScoreCalculated(PatternMatch match, int finalScore)
    {
        OnPatternScoreCalculated?.Invoke(match, finalScore);
    }
}
