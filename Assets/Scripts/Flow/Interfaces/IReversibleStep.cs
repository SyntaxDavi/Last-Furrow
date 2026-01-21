using Cysharp.Threading.Tasks;

/// <summary>
/// Interface: Step com capacidade de rollback (desfazer mudanças).
/// 
/// EXEMPLO DE USO:
/// - AdvanceTimeStep implementa IReversibleStep
/// - Se pipeline abortar, volta o dia anterior
/// - Garante consistência do estado do jogo
/// 
/// SOLID: Interface Segregation Principle
/// - Apenas steps que precisam de rollback implementam esta interface
/// - Steps simples (ex: logs) não precisam implementar
/// </summary>
public interface IReversibleStep : IFlowStep
{
    /// <summary>
    /// Desfaz mudanças feitas por Execute().
    /// Chamado em ordem reversa se pipeline abortar.
    /// </summary>
    UniTask Rollback();
    
    /// <summary>
    /// Indica se step foi executado com sucesso (precisa rollback).
    /// </summary>
    bool WasExecuted { get; }
}
