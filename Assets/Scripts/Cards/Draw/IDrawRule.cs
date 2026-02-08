using System.Collections.Generic;

/// <summary>
/// Interface para regras de validação/modificação de draw.
/// 
/// SOLID: Open/Closed Principle
/// - Adicione novas regras sem modificar código existente
/// - Cada regra tem uma única responsabilidade
/// 
/// REGRAS IMPLEMENTADAS:
/// - MaxDuplicatesRule: Máximo N cartas iguais por draw
/// - GuaranteedCardsRule: Garante certas cartas após X dias
/// 
/// FUTURAS:
/// - NoConsecutiveRule: Evita 3+ draws consecutivos da mesma carta
/// - RarityBalanceRule: Balanceia raridades no draw
/// </summary>
public interface IDrawRule
{
    /// <summary>
    /// Prioridade de execução (menor = executa primeiro).
    /// Regras de garantia devem ter prioridade maior (executar depois).
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Nome da regra para debug/logs.
    /// </summary>
    string RuleName { get; }
    
    /// <summary>
    /// Aplica a regra ao draw atual.
    /// Pode modificar, adicionar ou remover cartas da lista.
    /// </summary>
    /// <param name="drawnCards">Cartas sacadas originalmente</param>
    /// <param name="context">Contexto com deck, runData, etc.</param>
    /// <returns>Lista modificada de cartas</returns>
    List<CardID> Apply(List<CardID> drawnCards, DrawRuleContext context);
}

/// <summary>
/// Contexto compartilhado entre regras de draw.
/// Contém todas as dependências necessárias.
/// </summary>
public class DrawRuleContext
{
    public IRunDeckSource Deck { get; }
    public RunData RunData { get; }
    public int TargetDrawCount { get; }
    
    public DrawRuleContext(IRunDeckSource deck, RunData runData, int targetDrawCount)
    {
        Deck = deck;
        RunData = runData;
        TargetDrawCount = targetDrawCount;
    }
}
