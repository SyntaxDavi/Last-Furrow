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
/// RESPONSABILIDADES (ONDA 5.5 - DUAL-SCAN):
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
    private readonly RunData _runData;
    private readonly GameEvents _events;
    
    // ONDA 5.5: Novo sistema dual-scan
    private GridFullVerification _fullVerification;
    private GridSlotScanner _slotScanner;
    private bool _slotScannerInitialized;
    
    // Tracking separado para UI/Debug
    private int _lastPatternScore;
    private int _lastPatternCount;
    
    public int LastPatternScore => _lastPatternScore;
    public int LastPatternCount => _lastPatternCount;

    public DetectPatternsStep(
        IGridService gridService, 
        PatternDetector detector,
        PatternScoreCalculator calculator,
        RunData runData, 
        GameEvents events)
    {
        _gridService = gridService;
        _detector = detector;
        _calculator = calculator;
        _runData = runData;
        _events = events;
        
        // Criar verificação completa
        var trackingService = AppCore.Instance?.PatternTracking;
        _fullVerification = new GridFullVerification(gridService, detector, trackingService);
    }

    public IEnumerator Execute(FlowControl control)
    {
        Debug.Log("[DetectPatternsStep] ???????????????????????????????????????");
        Debug.Log("[DetectPatternsStep] ===== FASE 1: VERIFICAÇÃO INTEIRA =====");
        
        // FASE 1: Scan completo (lógica pura, sem visual)
        List<PatternMatch> matches = _fullVerification.Scan();
        
        _lastPatternCount = matches.Count;
        
        Debug.Log($"[DetectPatternsStep] {matches.Count} padrões detectados e armazenados no cache");
        
        // Calcular pontos COM METADATA
        int points = 0;
        PatternScoreTotalResult scoreResults = null;
        
        if (matches.Count > 0)
        {
            scoreResults = _calculator.CalculateTotalWithMetadata(matches, _gridService);
            points = scoreResults.TotalScore;
            
            // Disparar eventos para UI baseado na metadata
            foreach (var result in scoreResults.IndividualResults)
            {
                if (result.HasDecay)
                {
                    Debug.Log($"[DetectPatternsStep] Decay: {result.Match.DisplayName} (Dia {result.DaysActive}, {result.DecayMultiplier:F2}x)");
                    _events.Pattern.TriggerPatternDecayApplied(
                        result.Match, 
                        result.DaysActive, 
                        result.DecayMultiplier
                    );
                }
                
                if (result.HasRecreationBonus)
                {
                    Debug.Log($"[DetectPatternsStep] Recreation: {result.Match.DisplayName} (+10%)");
                    _events.Pattern.TriggerPatternRecreated(result.Match);
                }
            }
        }
        
        _lastPatternScore = points;
        
        Debug.Log($"[DetectPatternsStep] Total de pontos: {points}");
        
        // Atualizar HighestDailyPatternScore
        if (points > _runData.HighestDailyPatternScore)
        {
            _runData.HighestDailyPatternScore = points;
            Debug.Log($"[DetectPatternsStep] ? Novo recorde diário: {points}!");
        }
        
        // Adicionar à meta semanal
        if (points > 0)
        {
            int scoreBefore = _runData.CurrentWeeklyScore;
            _runData.CurrentWeeklyScore += points;
            
            Debug.Log($"[DetectPatternsStep] Score semanal: {scoreBefore} + {points} = {_runData.CurrentWeeklyScore}");
        }
        
        // Emitir evento legado (UI antiga pode reagir)
        _events.Pattern.TriggerPatternsDetected(matches, points);
        
        // Log summary
        LogPatternSummary(matches, points, scoreResults);
        
        Debug.Log("[DetectPatternsStep] ===== FASE 2: SCAN INCREMENTAL (VISUAL) =====");
        
        // FASE 2: Scan incremental (slot-por-slot, dispara animações)
        // ONDA 6.0: Agora AGUARDA as animações terminarem (yield return)
        if (matches.Count > 0)
        {
            yield return PlayIncrementalScan();
        }
        else
        {
            Debug.Log("[DetectPatternsStep] Nenhum padrão para animar, pulando scan incremental");
        }
        
        Debug.Log("[DetectPatternsStep] ???????????????????????????????????????");
        
        // Delay final
        yield return new WaitForSeconds(0.2f);
        
        // CLEANUP: Limpar cache ao fim da verificação
        PatternDetectionCache.Instance?.Clear();
        Debug.Log("[DetectPatternsStep] Cache limpo (prevenindo vazamento entre dias)");
    }
    
    /// <summary>
    /// Executa scan incremental (slot-por-slot) para animações.
    /// Cacheia referência ao GridSlotScanner para performance.
    /// </summary>
    private IEnumerator PlayIncrementalScan()
    {
        // Lazy initialization do scanner (primeira chamada)
        if (!_slotScannerInitialized)
        {
            _slotScanner = Object.FindFirstObjectByType<GridSlotScanner>();
            _slotScannerInitialized = true;
            
            if (_slotScanner == null)
            {
                Debug.LogWarning("[DetectPatternsStep] GridSlotScanner não encontrado - animações desabilitadas");
            }
            else
            {
                Debug.Log("[DetectPatternsStep] GridSlotScanner cacheado");
            }
        }
        
        // Executar scan se disponível
        if (_slotScanner != null)
        {
            Debug.Log("[DetectPatternsStep] Iniciando scan incremental...");
            yield return _slotScanner.ScanSequentially();
            Debug.Log("[DetectPatternsStep] Scan incremental concluído");
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
