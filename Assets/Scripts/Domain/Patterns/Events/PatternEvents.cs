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
/// - OnPatternSlotCompleted: Disparado por slot durante scanner incremental 
/// - OnPatternScoreCalculated: Disparado após cálculo de score (para UI mostrar breakdown)
/// - OnPatternDecayApplied: Disparado quando decay é aplicado a um padrão (ONDA 4)
/// - OnPatternRecreated: Disparado quando padrão quebrado é recriado com bonus (ONDA 4)
/// </summary>
public class PatternEvents
{
    /// <summary>
    /// Disparado após detecção de padrões no fim do dia.
    /// Parâmetros: (Lista de PatternMatch, Pontos totais de padrões)
    /// </summary>
    public event Action<List<PatternMatch>, int> OnPatternsDetected;
    
    /// <summary>
    /// Disparado quando scanner incremental encontra um padrão completo.
    /// Usado para animações sequenciais (highlight + pop-up).
    /// Parâmetros: (PatternMatch)
    /// 
    /// FILOSOFIA:
    /// - Cada padrão dispara 1 evento (mesmo que compartilhe slots)
    /// - UI escuta e aplica visual (cor, pulse, pop-up)
    /// - Não calcula score/tier (recebe dados prontos)
    /// </summary>
    public event Action<PatternMatch> OnPatternSlotCompleted;
    
    /// <summary>
    /// Disparado quando um padrão individual é calculado (para UI/debug detalhado).
    /// Parâmetros: (PatternMatch, Score final calculado)
    /// </summary>
    public event Action<PatternMatch, int> OnPatternScoreCalculated;
    
    /// <summary>
    /// Disparado quando decay é aplicado a um padrão.
    /// Parâmetros: (PatternMatch, DaysActive, DecayMultiplier)
    /// UI pode usar isso para mostrar indicador visual de decay.
    /// </summary>
    public event Action<PatternMatch, int, float> OnPatternDecayApplied;
    
    /// <summary>
    /// Disparado quando um padrão quebrado é recriado com bonus.
    /// Parâmetros: (PatternMatch com HasRecreationBonus = true)
    /// UI pode usar isso para mostrar efeito especial de "+10%".
    /// </summary>
    public event Action<PatternMatch> OnPatternRecreated;
    
    // --- Triggers ---
    
    public void TriggerPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        OnPatternsDetected?.Invoke(matches, totalPoints);
    }
    
    /// <summary>
    /// Dispara evento quando scanner incremental completa um padrão.
    /// Chamado pelo GridSlotScanner ao encontrar padrão completo.
    /// </summary>
    public void TriggerPatternSlotCompleted(PatternMatch match)
    {
        OnPatternSlotCompleted?.Invoke(match);
    }
    
    public void TriggerPatternScoreCalculated(PatternMatch match, int finalScore)
    {
        OnPatternScoreCalculated?.Invoke(match, finalScore);
    }
    
    /// <summary>
    /// Dispara evento de decay aplicado.
    /// </summary>
    public void TriggerPatternDecayApplied(PatternMatch match, int daysActive, float decayMultiplier)
    {
        OnPatternDecayApplied?.Invoke(match, daysActive, decayMultiplier);
    }
    
    /// <summary>
    /// Dispara evento de padrão recriado com bonus.
    /// </summary>
    public void TriggerPatternRecreated(PatternMatch match)
    {
        OnPatternRecreated?.Invoke(match);
    }
}
