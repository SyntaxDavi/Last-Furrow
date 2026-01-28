using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Step que detecta padrões no grid e orquestra a contagem de pontos contínua.
/// SRP: Detecta padrões (Lógica) e chama o Controller Visual para animação.
/// </summary>
public class DetectPatternsStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly PatternDetector _detector;
    private readonly PatternScoreCalculator _calculator;
    private readonly RunData _runData;
    private readonly GameEvents _events;
    private readonly AnalyzingPhaseController _visualController;
    private readonly DayAnalysisResult _analysisResult;

    public DetectPatternsStep(
        IGridService gridService,
        PatternDetector detector,
        PatternScoreCalculator calculator,
        PatternTrackingService trackingService, // Mantido para compatibilidade se necessário
        RunData runData,
        GameEvents events,
        GridSlotScanner slotScanner, // Mantido na assinatura para não quebrar o builder ainda
        DayAnalysisResult analysisResult = null,
        AnalyzingPhaseController visualController = null) 
    {
        _gridService = gridService;
        _detector = detector;
        _calculator = calculator;
        _runData = runData;
        _events = events;
        _analysisResult = analysisResult;
        _visualController = visualController;
    }

    public async UniTask Execute(FlowControl control)
    {
        Debug.Log("[DetectPatternsStep] Iniciando fase de análise...");

        // 1. Detecção de Padrões (Lógica)
        var matches = _detector.DetectAll(_gridService);
        int patternPoints = 0;

        if (matches.Count > 0)
        {
            var scoreResults = _calculator.CalculateTotalWithMetadata(matches, _gridService);
            patternPoints = scoreResults.TotalScore;

            if (_analysisResult != null)
            {
                _analysisResult.SetPatterns(matches, patternPoints);
            }
        }

        // 2. Fase Visual (Contagem contínua: Passivos + Padrões)
        if (_visualController != null && _analysisResult != null)
        {
            Debug.Log("[DetectPatternsStep] Orquestrando animação contínua...");
            await _visualController.AnalyzeAndGrowGrid(_gridService, _events, _runData, _analysisResult);
        }

        // 3. Atualização de Dados (Data Integrity)
        // Somamos apenas no final para que o HUD reflita o progresso durante as animações
        if (_analysisResult != null)
        {
            int totalPoints = _analysisResult.TotalDayPoints;
            if (totalPoints > 0)
            {
                _runData.CurrentWeeklyScore += totalPoints;
                Debug.Log($"[DetectPatternsStep] Score final atualizado: +{totalPoints} pontos.");
            }
        }

        // Limpeza de cache se houver
        PatternDetectionCache.Instance?.Clear();
        await UniTask.Delay(100);
    }
}
