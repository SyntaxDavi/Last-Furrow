using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builder Pattern (SOLID): Constr�i o pipeline de resolu��o di�ria.
/// 
/// RESPONSABILIDADES (SRP):
/// - Definir ordem dos steps
/// - Injetar depend�ncias corretas em cada step
/// - Configurar pipeline baseado em contexto
/// 
/// BENEF�CIOS:
/// - DailyResolutionSystem n�o conhece steps espec�ficos
/// - F�cil criar pipelines alternativos (debug, teste, weekend)
/// - Test�vel (mock do builder retorna steps fake)
/// - Configur�vel (diferentes pipelines para diferentes modos)
/// </summary>
public class DailyPipelineBuilder : IDailyFlowBuilder
{
    public List<IFlowStep> BuildPipeline(
        DailyResolutionContext context,
        DailyVisualContext visualContext,
        RunData runData)
    {
        if (context == null)
        {
            Debug.LogError("[DailyPipelineBuilder] LogicContext é NULL!");
            return new List<IFlowStep>();
        }

        if (visualContext == null || !visualContext.IsValid())
        {
            Debug.LogWarning("[DailyPipelineBuilder] VisualContext inválido! Pipeline headless.");
        }

        context.AnalysisResult?.Clear();

        var pipeline = new List<IFlowStep>();

        // STEP 1: Crescimento do Grid + Animação Visual
        pipeline.Add(new GrowGridStep(
            context.GridService,
            context.Events,
            context.InputManager,
            runData,
            visualContext?.Analyzer,
            context.AnalysisResult
        ));

        // STEP 2: Detecção de Padrões + Animações
        pipeline.Add(new DetectPatternsStep(
            context.GridService,
            context.PatternDetector,
            context.PatternCalculator,
            context.PatternTracking,
            runData,
            context.Events,
            visualContext?.Scanner,
            context.AnalysisResult,
            visualContext?.Analyzer,
            AppCore.Instance?.GameStateManager,
            context.SaveManager,
            visualContext?.HandManager
        ));

        // STEP 3: Cálculo de Score + Meta Semanal
        // FIX: Passa GameStateProvider para bloquear input durante resultado
        pipeline.Add(new CalculateScoreStep(
            context.GoalSystem,
            context.RunManager,
            runData,
            context.Events.Progression,
            AppCore.Instance?.GameStateManager
        ));

        // STEP 4: Avançar Tempo
        pipeline.Add(new AdvanceTimeStep(
            context.RunManager,
            context.SaveManager,
            runData
        ));

        // STEP 5: Draw de Novas Cartas
        var drawPolicy = new CardDrawPolicy();
        pipeline.Add(new DailyDrawStep(
            context.HandSystem,
            context.RunManager,
            runData,
            drawPolicy
        ));

        Debug.Log($"[DailyPipelineBuilder] Pipeline construído: {pipeline.Count} steps");
        return pipeline;
    }
}
