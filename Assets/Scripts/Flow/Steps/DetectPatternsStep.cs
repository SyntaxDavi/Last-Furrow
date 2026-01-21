using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IFlowStep que detecta padrões no grid usando DUAL-SCAN architecture.
/// 
/// POSIÇÃO NO PIPELINE:
/// 1. GrowGridStep (plantas crescem/murcham)
/// 2. DetectPatternsStep ? AQUI (avalia grid final)
/// 3. CalculateScoreStep (aplica meta + patterns)
/// 4. AdvanceTimeStep
/// 5. DailyDrawStep
/// 
/// RESPONSABILIDADES (DUAL-SCAN):
/// - [FASE 1] Verificação Inteira (GridFullVerification)
///   - Detecta todos os padrões de uma vez
///   - Calcula pontos e atualiza tracking
///   - Armazena no PatternDetectionCache
/// 
/// - [FASE 2] Verificação Incremental (GridSlotScanner)
///   - Itera slots desbloqueados sequencialmente
///   - Dispara OnPatternSlotCompleted para cada padrão
///   - UI escuta e anima (highlights, pop-ups)
/// 
/// ARQUITETURA (SOLID):
/// - Lógica separada de visual
/// - Cache cleanup para evitar vazamento entre dias
/// - Scanner incrementa é OPCIONAL (pode desabilitar)
/// </summary>
public class DetectPatternsStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly PatternDetector _detector;
    private readonly PatternScoreCalculator _calculator;
    private readonly PatternTrackingService _trackingService; 
    private readonly RunData _runData;
    private readonly GameEvents _events;
    private readonly GridSlotScanner _slotScanner;
    
    // Tracking separado para UI/Debug
    private int _lastPatternScore;
    private int _lastPatternCount;
    
    public int LastPatternScore => _lastPatternScore;
    public int LastPatternCount => _lastPatternCount;

    public DetectPatternsStep(
        IGridService gridService,
        PatternDetector detector,
        PatternScoreCalculator calculator,
        PatternTrackingService trackingService, 
        RunData runData,
        GameEvents events,
        GridSlotScanner slotScanner) 
    {
        _gridService = gridService;
        _detector = detector;
        _calculator = calculator;
        _trackingService = trackingService;
        _runData = runData;
        _events = events;
        _slotScanner = slotScanner;
    }
    /// <summary>
    /// Executa scan incremental (slot-por-slot) para animações.
    /// Cacheia referência ao GridSlotScanner para performance.
    /// </summary>
    public IEnumerator Execute(FlowControl control)
    {
        Debug.Log("[DetectPatternsStep] ===== FASE 1: VERIFICAÇÃO INTEIRA =====");

        // 1. Lógica Pura (Calcula tudo)
        var fullVerification = new GridFullVerification(_gridService, _detector, _trackingService);
        List<PatternMatch> matches = fullVerification.Scan();

        // 2. Pontuação e Eventos de Dados
        int points = 0;
        if (matches.Count > 0)
        {
            var scoreResults = _calculator.CalculateTotalWithMetadata(matches, _gridService);
            points = scoreResults.TotalScore;

            // Disparar eventos de lógica (Decay, etc)
            foreach (var result in scoreResults.IndividualResults)
            {
                if (result.HasDecay) _events.Pattern.TriggerPatternDecayApplied(result.Match, result.DaysActive, result.DecayMultiplier);
                if (result.HasRecreationBonus) _events.Pattern.TriggerPatternRecreated(result.Match);
            }

            LogPatternSummary(matches, points, scoreResults);
        }

        // Atualizar Recordes e Meta
        _lastPatternScore = points;
        if (points > _runData.HighestDailyPatternScore) _runData.HighestDailyPatternScore = points;
        if (points > 0) _runData.CurrentWeeklyScore += points;

        // Dispara evento principal
        _events.Pattern.TriggerPatternsDetected(matches, points);

        Debug.Log("[DetectPatternsStep] ===== FASE 2: SCAN INCREMENTAL (VISUAL) =====");

        // 3. Visual (Chama o método auxiliar AQUI)
        if (matches.Count > 0)
        {
            yield return PlayIncrementalScan();
        }

        // Delay final e Limpeza
        yield return new WaitForSeconds(0.2f);
        PatternDetectionCache.Instance?.Clear();
    }

    // Método auxiliar limpo: só executa se tiver scanner
    private IEnumerator PlayIncrementalScan()
    {
        if (_slotScanner != null)
        {
            yield return _slotScanner.ScanSequentially();
        }
        else
        {
            Debug.LogWarning("[DetectPatternsStep] GridSlotScanner não injetado. Animações puladas.");
        }
    }


    private void LogPatternSummary(List<PatternMatch> matches, int totalPoints, PatternScoreTotalResult scoreResults)
    {
        if (matches.Count == 0)
        {
            Debug.Log("[DetectPatternsStep] Nenhum padrão encontrado neste turno");
            return;
        }
        
        Debug.Log("[DetectPatternsStep] --- RESUMO DE PADRÕES ---");
        
        var grouped = new Dictionary<string, int>();
        var decayInfo = new Dictionary<string, int>();
        
        foreach (var match in matches)
        {
            if (!grouped.ContainsKey(match.DisplayName))
            {
                grouped[match.DisplayName] = 0;
                decayInfo[match.DisplayName] = 0;
            }
            
            grouped[match.DisplayName]++;
            
            if (match.DaysActive > decayInfo[match.DisplayName])
            {
                decayInfo[match.DisplayName] = match.DaysActive;
            }
        }
        
        foreach (var kvp in grouped)
        {
            string decayText = decayInfo[kvp.Key] > 1 ? $" (Decay: Dia {decayInfo[kvp.Key]})" : "";
            Debug.Log($"[DetectPatternsStep]   • {kvp.Value}x {kvp.Key}{decayText}");
        }
        
        if (scoreResults != null && matches.Count > 1)
        {
            Debug.Log($"[DetectPatternsStep]   ? Sinergia: {scoreResults.ScoreBeforeSynergy} ? {totalPoints} ({scoreResults.SynergyMultiplier:F2}x)");
        }
        
        Debug.Log($"[DetectPatternsStep] TOTAL: {totalPoints} pontos");
    }
}
