using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builder Pattern (SOLID): Constrói o pipeline de resolução diária.
/// 
/// RESPONSABILIDADES (SRP):
/// - Definir ordem dos steps
/// - Injetar dependências corretas em cada step
/// - Configurar pipeline baseado em contexto
/// 
/// BENEFÍCIOS:
/// - DailyResolutionSystem não conhece steps específicos
/// - Fácil criar pipelines alternativos (debug, teste, weekend)
/// - Testável (mock do builder retorna steps fake)
/// - Configurável (diferentes pipelines para diferentes modos)
/// </summary>
public class DailyPipelineBuilder : IDailyFlowBuilder
{
    /// <summary>
    /// Constrói pipeline padrão do dia (produção).
    /// </summary>
    public List<IFlowStep> BuildPipeline(
        DailyResolutionContext context,
        DailyVisualContext visualContext,
        RunData runData)
    {
        // Validações de segurança
        if (context == null)
        {
            Debug.LogError("[DailyPipelineBuilder] LogicContext é NULL!");
            return new List<IFlowStep>();
        }
        
        if (visualContext == null || !visualContext.IsValid())
        {
            Debug.LogWarning("[DailyPipelineBuilder] VisualContext inválido! Pipeline headless.");
        }

        // CONSTRUÇÃO DO PIPELINE (Single Source of Truth)
        var pipeline = new List<IFlowStep>();
        
        // STEP 1: Crescimento do Grid + Animação Visual
        pipeline.Add(new GrowGridStep(
            context.GridService,
            context.Events,
            context.InputManager,
            runData,
            visualContext?.Analyzer  // Null-safe: funciona sem visual
        ));
        
        // STEP 2: Detecção de Padrões + Animações
        pipeline.Add(new DetectPatternsStep(
            context.GridService,
            context.PatternDetector,
            context.PatternCalculator,
            context.PatternTracking,
            runData,
            context.Events,
            visualContext?.Scanner  // Null-safe: funciona sem visual
        ));
        
        // STEP 3: Cálculo de Score + Meta Semanal
        pipeline.Add(new CalculateScoreStep(
            context.GoalSystem,
            context.RunManager,
            runData,
            context.Events.Progression
        ));
        
        // STEP 4: Avançar Tempo (Dia -> Próximo Dia) + Reset Draw Flag
        pipeline.Add(new AdvanceTimeStep(
            context.RunManager,
            context.SaveManager,
            runData
        ));
        
        // STEP 5: Draw de Novas Cartas
        pipeline.Add(new DailyDrawStep(
            context.HandSystem,
            context.RunManager,
            runData
        ));
        
        Debug.Log($"[DailyPipelineBuilder] ? Pipeline construído: {pipeline.Count} steps");
        return pipeline;
    }
}
