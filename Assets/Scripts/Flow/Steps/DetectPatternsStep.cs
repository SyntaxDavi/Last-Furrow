using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IFlowStep que detecta padrões no grid e adiciona pontos à meta semanal.
/// 
/// POSIÇÃO NO PIPELINE:
/// 1. GrowGridStep (plantas crescem/murcham)
/// 2. DetectPatternsStep ? AQUI (avalia grid final)
/// 3. CalculateScoreStep (aplica meta + patterns)
/// 4. AdvanceTimeStep
/// 5. DailyDrawStep
/// 
/// RESPONSABILIDADES:
/// - Chamar PatternDetector.DetectAll()
/// - Chamar PatternTrackingService.UpdateActivePatterns() [ONDA 4]
/// - Chamar PatternScoreCalculator.CalculateTotal()
/// - Adicionar pontos ao RunData.CurrentWeeklyScore
/// - Emitir evento OnPatternsDetected
/// - Logs de debug verbosos
/// 
/// NOTA: Pontos de padrões são ADICIONADOS ao CurrentWeeklyScore,
/// junto com os pontos de plantas (processados por WeeklyGoalSystem).
/// O sistema atual já adiciona pontos por plantas vivas, então
/// padrões são um BÔNUS adicional.
/// </summary>
public class DetectPatternsStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly PatternDetector _detector;
    private readonly PatternScoreCalculator _calculator;
    private readonly RunData _runData;
    private readonly GameEvents _events;
    
    // ? Tracking separado para UI/Debug (conforme solicitado)
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
    }

    public IEnumerator Execute(FlowControl control)
    {
        Debug.Log("[DetectPatternsStep] ????????????????????????????????????????");
        Debug.Log("[DetectPatternsStep] Iniciando detecção de padrões...");
        
        // 1. Detectar padrões
        List<PatternMatch> matches = _detector.DetectAll(_gridService);
        Debug.Log($"[DetectPatternsStep] {matches.Count} padrões detectados");
        
        // 2. ONDA 4: Atualizar tracking (decay, identidade, etc)
        var trackingService = AppCore.Instance.PatternTracking;
        if (trackingService != null)
        {
            matches = trackingService.UpdateActivePatterns(matches);
            Debug.Log($"[DetectPatternsStep] Tracking atualizado - padrões com DaysActive preenchido");
        }
        else
        {
            Debug.LogWarning("[DetectPatternsStep] PatternTrackingService não disponível - decay não será aplicado");
        }
        
        _lastPatternCount = matches.Count;
        
        // 3. Calcular pontos (agora com decay aplicado via DaysActive)
        int points = 0;
        if (matches.Count > 0)
        {
            points = _calculator.CalculateTotal(matches, _gridService);
        }
        _lastPatternScore = points;
        
        Debug.Log($"[DetectPatternsStep] Total de pontos de padrões: {points}");
        
        // 4. Atualizar HighestDailyPatternScore
        if (points > _runData.HighestDailyPatternScore)
        {
            _runData.HighestDailyPatternScore = points;
            Debug.Log($"[DetectPatternsStep] ?? Novo recorde diário de padrões: {points}!");
        }
        
        // 5. Adicionar à meta semanal
        if (points > 0)
        {
            int scoreBefore = _runData.CurrentWeeklyScore;
            _runData.CurrentWeeklyScore += points;
            
            Debug.Log($"[DetectPatternsStep] Score semanal: {scoreBefore} + {points} = {_runData.CurrentWeeklyScore}");
        }
        
        // 6. Emitir evento (UI pode reagir)
        _events.Pattern.TriggerPatternsDetected(matches, points);
        
        // 7. Log summary
        LogPatternSummary(matches, points);
        
        Debug.Log("[DetectPatternsStep] ????????????????????????????????????????");
        
        // 8. Delay visual pequeno para feedback
        yield return new WaitForSeconds(0.2f);
    }
    
    private void LogPatternSummary(List<PatternMatch> matches, int totalPoints)
    {
        if (matches.Count == 0)
        {
            Debug.Log("[DetectPatternsStep] Nenhum padrão encontrado neste turno.");
            return;
        }
        
        Debug.Log("[DetectPatternsStep] --- RESUMO DE PADRÕES ---");
        
        // Agrupar por tipo para log mais limpo
        var grouped = new Dictionary<string, int>();
        var decayInfo = new Dictionary<string, int>(); // Maior DaysActive por tipo
        
        foreach (var match in matches)
        {
            if (!grouped.ContainsKey(match.DisplayName))
            {
                grouped[match.DisplayName] = 0;
                decayInfo[match.DisplayName] = 0;
            }
            grouped[match.DisplayName]++;
            
            if (match.DaysActive > decayInfo[match.DisplayName])
                decayInfo[match.DisplayName] = match.DaysActive;
        }
        
        foreach (var kvp in grouped)
        {
            string plural = kvp.Value > 1 ? "s" : "";
            string decay = decayInfo[kvp.Key] > 1 ? $" [Decay: Dia {decayInfo[kvp.Key]}]" : "";
            Debug.Log($"  ? {kvp.Value}x {kvp.Key}{plural}{decay}");
        }
        
        Debug.Log($"  ? TOTAL: {totalPoints} pontos de padrões ?");
    }
}
