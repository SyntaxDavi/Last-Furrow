using System.Collections.Generic;

/// <summary>
/// Factory Pattern: Define contrato para construção do pipeline diário.
/// SOLID: Open/Closed Principle - Fácil criar builders alternativos (teste, debug, etc).
/// </summary>
public interface IDailyFlowBuilder
{
    /// <summary>
    /// Constrói a sequência de steps do pipeline diário.
    /// </summary>
    /// <param name="context">Contexto com todas as dependências lógicas</param>
    /// <param name="visualContext">Contexto com todas as dependências visuais</param>
    /// <param name="runData">Dados da run atual</param>
    /// <returns>Lista ordenada de steps a executar</returns>
    List<IFlowStep> BuildPipeline(
        DailyResolutionContext context,
        DailyVisualContext visualContext,
        RunData runData
    );
}
