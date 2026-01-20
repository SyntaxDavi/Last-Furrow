/// <summary>
/// Interface para modificadores de score (Strategy Pattern).
/// 
/// FILOSOFIA:
/// - Cada modificador é uma estratégia independente
/// - Fácil de testar isoladamente
/// - Fácil de adicionar novos modificadores (Open/Closed Principle)
/// - Calculator orquestra, modificadores executam
/// 
/// IMPLEMENTAÇÕES:
/// - CropValueModifier: Valor médio das crops
/// - MaturityModifier: Bonus por maturidade
/// - DecayModifier: Penalty por dias consecutivos
/// - DiversityModifier: Bonus para padrões com diversidade
/// </summary>
public interface IScoreModifier
{
    /// <summary>
    /// Nome do modificador (para logs/UI).
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Calcula o multiplicador para este modificador.
    /// Retorna 1.0f se não aplicável.
    /// </summary>
    /// <param name="match">Padrão sendo calculado</param>
    /// <param name="gridService">Serviço de grid para consultar slots</param>
    /// <returns>Multiplicador a ser aplicado (1.0 = sem mudança)</returns>
    float Calculate(PatternMatch match, IGridService gridService);
    
    /// <summary>
    /// Se este modificador é aplicável ao padrão atual.
    /// </summary>
    bool IsApplicable(PatternMatch match);
}
