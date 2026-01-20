using System.Collections.Generic;

/// <summary>
/// Interface base para todos os padrões de grid.
/// 
/// RESPONSABILIDADE ÚNICA:
/// - Definir geometria do padrão
/// - Validar slots (locked, withered, continuidade)
/// - Retornar PatternMatch se válido
/// - Usar PatternDefinitionSO para metadados
/// 
/// NÃO DEVE:
/// - Calcular score final (isso é PatternScoreCalculator)
/// - Conhecer outros padrões
/// - Depender de estado global (exceto IGridService)
/// - Conter lógica de negócio além de geometria
/// 
/// PRINCÍPIO:
/// "Padrões dizem 'sou válido?', não 'quanto valho?'"
/// 
/// ONDA 5: Padrões agora recebem PatternDefinitionSO no construtor.
/// </summary>
public interface IGridPattern
{
    /// <summary>
    /// ID estável do padrão (ex: "ADJACENT_PAIR", "FULL_LINE").
    /// Vem do PatternDefinitionSO configurado no Inspector.
    /// </summary>
    string PatternID { get; }
    
    /// <summary>
    /// Nome legível para UI/Debug (ex: "Par Adjacente", "Linha Completa").
    /// Vem do PatternDefinitionSO configurado no Inspector.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Pontuação base fixa do padrão.
    /// Vem do PatternDefinitionSO configurado no Inspector.
    /// Toda matemática de multiplicadores fica no PatternScoreCalculator.
    /// </summary>
    int BaseScore { get; }
    
    /// <summary>
    /// Tenta detectar este padrão no grid.
    /// Pode retornar MÚLTIPLOS matches (ex: várias linhas completas).
    /// 
    /// REGRAS:
    /// - Slots bloqueados (locked) quebram continuidade geométrica
    /// - Slots com plantas mortas (withered) invalidam o padrão
    /// - Slots vazios não contam para o padrão
    /// </summary>
    /// <param name="gridService">Serviço de grid para consultar slots</param>
    /// <returns>Lista de matches encontrados (pode ser vazia)</returns>
    List<PatternMatch> DetectAll(IGridService gridService);
}
