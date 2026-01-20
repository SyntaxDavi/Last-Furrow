using System.Collections.Generic;

/// <summary>
/// Interface base para todos os padrões de grid.
/// 
/// RESPONSABILIDADE ÚNICA:
/// - Definir geometria do padrão
/// - Validar slots (locked, withered, continuidade)
/// - Retornar PatternMatch se válido
/// - Declarar BaseScore (valor fixo)
/// 
/// NÃO DEVE:
/// - Calcular score final (isso é PatternScoreCalculator)
/// - Conhecer outros padrões
/// - Depender de estado global (exceto IGridService)
/// - Conter lógica de negócio além de geometria
/// 
/// PRINCÍPIO:
/// "Padrões dizem 'sou válido?', não 'quanto valho?'"
/// </summary>
public interface IGridPattern
{
    /// <summary>
    /// ID estável do padrão (ex: "ADJACENT_PAIR", "FULL_LINE").
    /// Usado para SaveData e tracking. NUNCA use nomes exibidos ou nomes de classe.
    /// </summary>
    string PatternID { get; }
    
    /// <summary>
    /// Nome legível para UI/Debug (ex: "Par Adjacente", "Linha Completa").
    /// Pode ser localizado no futuro.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Pontuação base fixa do padrão. Sem lógica, apenas valor.
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
