using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LastFurrow.Traditions;

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
            int totalDayPoints = 0;
            
            // --- TRADIÇÕES: Início da Avaliação ---
            var traditions = AppCore.Instance?.Services?.Traditions;
            var evaluator = traditions?.Evaluator;
            evaluator?.StartDayEvaluation(_runData, _gridService, _events);

            // Bônus iniciais (ex: tradições que dão pontos flat no início da fase)
            totalDayPoints += evaluator?.EvaluatePreScoring() ?? 0;

            if (matches.Count > 0)
            {
                foreach (var match in matches)
                {
                    // Pontuação técnica do padrão (calculada via Strategy Pattern)
                    var scoreResult = _calculator.CalculateSingleWithMetadata(match, _gridService);
                    int basePoints = scoreResult.FinalScore;
                    
                    // --- TRADIÇÕES: Bônus por Padrão ---
                    int traditionBonus = evaluator?.EvaluatePatternDetected(match) ?? 0;
                    
                    totalDayPoints += basePoints + traditionBonus;
                }
            }

            // --- TRADIÇÕES: Bônus Pós-padrões (ex: bonus por quantidade total) ---
            totalDayPoints += evaluator?.EvaluatePostPattern() ?? 0;
            
            // --- TRADIÇÕES: Multiplicadores Finais (ex: x1.1 no score total) ---
            // Nota: Se finalMult > 0, tratamos como percentual ou multiplicador dependendo da regra.
            // Aqui vamos assumir multi bônus simples de soma por enquanto ou mult real.
            int multiplierBonus = evaluator?.EvaluateFinalMultiplier() ?? 0;
            totalDayPoints += multiplierBonus;

            if (_analysisResult != null)
            {
                // Patterns são registrados com seus pontos
                _analysisResult.SetPatterns(matches, totalDayPoints);
            }

            // 1.5. DATA INTEGRITY: Aplica score ANTES das animações e salva
            // NOTA: TotalDayPoints já inclui Passive (do GrowGridStep) + Patterns (daqui)
            if (_analysisResult != null)
            {
                int totalPoints = _analysisResult.TotalDayPoints;
                if (totalPoints > 0)
                {
                    Debug.Log($"[DetectPatternsStep] ANTES: CurrentWeeklyScore = {_runData.CurrentWeeklyScore}");
                    _runData.CurrentWeeklyScore += totalPoints;
                    Debug.Log($"[DetectPatternsStep] DEPOIS: CurrentWeeklyScore = {_runData.CurrentWeeklyScore} (+{totalPoints})");
                    _saveManager?.SaveGame();
                    Debug.Log($"[DetectPatternsStep] Score salvo: +{totalPoints} (Passive: {_analysisResult.TotalPassivePoints}, Patterns: {_analysisResult.TotalPatternPoints})");
                }
            }
            
            // --- TRADIÇÕES: Efeitos de Fim do Dia (ex: spawnar crops, ganhar gold extra) ---
            bool metGoal = _runData.CurrentWeeklyScore >= _runData.WeeklyGoalTarget;
            evaluator?.EvaluatePostDay(totalDayPoints, metGoal);
            
            // --- TRADIÇÕES: Limpeza do Contexto ---
            evaluator?.EndDayEvaluation();

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

        // Os pontos serão aplicados durante as animações (PassiveScoresPhase e PatternAnalysisPhase)
        // para manter o display visual sincronizado com os dados reais.
        // Aqui apenas logamos para debug.
        if (_analysisResult != null)
        {
            Debug.Log($"[DetectPatternsStep] Pontos pendentes: Passive={_analysisResult.TotalPassivePoints}, Patterns={_analysisResult.TotalPatternPoints}");
        }
    }
}
