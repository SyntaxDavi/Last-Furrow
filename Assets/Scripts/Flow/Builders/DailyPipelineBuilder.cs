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
    /// <summary>
    /// Constr�i pipeline padr�o do dia (produ��o).
    /// </summary>
    public List<IFlowStep> BuildPipeline(
        DailyResolutionContext context,
        DailyVisualContext visualContext,
        RunData runData)
    {
        // Valida��es de seguran�a
        if (context == null)
        {
            Debug.LogError("[DailyPipelineBuilder] LogicContext � NULL!");
            return new List<IFlowStep>();
        }
        
        if (visualContext == null || !visualContext.IsValid())
        {
            Debug.LogWarning("[DailyPipelineBuilder] VisualContext invlido! Pipeline headless.");
        }

        // CONSTRUO DO PIPELINE (Single Source of Truth)
        var pipeline = new List<IFlowStep>();
        
        // STEP 1: Crescimento do Grid + Animao Visual
        pipeline.Add(new GrowGridStep(
            context.GridService,
            context.Events,
            context.InputManager,
            runData,
            visualContext?.Analyzer,  // Null-safe: funciona sem visual
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
            visualContext?.Scanner,  // Null-safe: funciona sem visual
            context.AnalysisResult,
            visualContext?.Analyzer, // ⭐ INJETADO
            AppCore.Instance?.GameStateManager,  // Para bloquear input durante análise
            context.SaveManager,                  // Para salvar score preventivamente
            visualContext?.HandManager            // Para liberar cartas em drag
        ));
        
        // STEP 3: Clculo de Score + Meta Semanal
        pipeline.Add(new CalculateScoreStep(
            context.GoalSystem,
            context.RunManager,
            runData,
            context.Events.Progression
        ));
        
        // STEP 4: Avan�ar Tempo (Dia -> Pr�ximo Dia) + Reset Draw Flag
        pipeline.Add(new AdvanceTimeStep(
            context.RunManager,
            context.SaveManager,
            runData
        ));
        
        // STEP 5: Draw de Novas Cartas
        // SOLID: Usa CardDrawPolicy para centralizar regras de neg�cio
        var drawPolicy = new CardDrawPolicy();
        pipeline.Add(new DailyDrawStep(
            context.HandSystem,
            context.RunManager,
            runData,
            drawPolicy
        ));
        
        Debug.Log($"[DailyPipelineBuilder] ? Pipeline constru�do: {pipeline.Count} steps");
        return pipeline;
    }
}
