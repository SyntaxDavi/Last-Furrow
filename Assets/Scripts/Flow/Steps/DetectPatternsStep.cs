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
    private readonly AnalyzingPhaseOrchestrator _visualController;
    private readonly DayAnalysisResult _analysisResult;
    
    // Novas dependências para segurança e bloqueio de input
    private readonly GameStateManager _gameStateManager;
    private readonly SaveManager _saveManager;
    private readonly HandManager _handManager;

    public DetectPatternsStep(
        IGridService gridService,
        PatternDetector detector,
        PatternScoreCalculator calculator,
        PatternTrackingService trackingService, // Mantido para compatibilidade se necessário
        RunData runData,
        GameEvents events,
        GridSlotScanner slotScanner, // Mantido na assinatura para não quebrar o builder ainda
        DayAnalysisResult analysisResult = null,
        AnalyzingPhaseOrchestrator visualController = null,
        GameStateManager gameStateManager = null,
        SaveManager saveManager = null,
        HandManager handManager = null) 
    {
        _gridService = gridService;
        _detector = detector;
        _calculator = calculator;
        _runData = runData;
        _events = events;
        _analysisResult = analysisResult;
        _visualController = visualController;
        _gameStateManager = gameStateManager;
        _saveManager = saveManager;
        _handManager = handManager;
    }

    public async UniTask Execute(FlowControl control)
    {
        Debug.Log("[DetectPatternsStep] Iniciando fase de análise...");

        // 0. SEGURANÇA: Libera cartas em drag e bloqueia interações
        _handManager?.ForceReleaseAllDrags();
        _gameStateManager?.SetState(GameState.Analyzing);

        try
        {
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

            // 1.5. DATA INTEGRITY: Aplica score ANTES das animações e salva
            if (_analysisResult != null)
            {
                int totalPoints = _analysisResult.TotalDayPoints;
                if (totalPoints > 0)
                {
                    _runData.CurrentWeeklyScore += totalPoints;
                    _saveManager?.SaveGame(); // Persiste imediatamente
                    Debug.Log($"[DetectPatternsStep] Score salvo preventivamente: +{totalPoints} pontos.");
                }
            }

            // 2. Fase Visual (Contagem contínua: Passivos + Padrões)
            // Nota: O score já foi aplicado, então a animação é puramente visual agora
            if (_visualController != null && _analysisResult != null)
            {
                Debug.Log("[DetectPatternsStep] Orquestrando animação contínua...");
                await _visualController.AnalyzeAndGrowGrid(_gridService, _events, _runData, _analysisResult);
            }

            // Limpeza de cache se houver
            PatternDetectionCache.Instance?.Clear();
            await UniTask.Delay(100);
        }
        finally
        {
            // 3. Restaura estado (garantido mesmo se houver exceção)
            _gameStateManager?.SetState(GameState.Playing);
            Debug.Log("[DetectPatternsStep] Estado restaurado para Playing.");
        }
    }
}
