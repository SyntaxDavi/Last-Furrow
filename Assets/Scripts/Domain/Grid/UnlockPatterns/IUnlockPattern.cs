using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resultado da geração de padrão (contrato rígido).
/// 
/// GARANTIAS:
/// - Success indica se geração funcionou
/// - Pattern nunca contém duplicatas
/// - Pattern nunca contém coordenadas fora do grid
/// - ValidationErrors explica falhas
/// </summary>
public struct PatternResult
{
    public bool Success;
    public List<Vector2Int> Pattern;
    public string ValidationErrors;

    public static PatternResult Ok(List<Vector2Int> pattern)
    {
        return new PatternResult
        {
            Success = true,
            Pattern = pattern,
            ValidationErrors = null
        };
    }

    public static PatternResult Fail(string errors)
    {
        return new PatternResult
        {
            Success = false,
            Pattern = new List<Vector2Int>(),
            ValidationErrors = errors
        };
    }
}

/// <summary>
/// Interface para padrões de desbloqueamento inicial do grid.
/// 
/// SOLID - Open/Closed Principle:
/// - Aberto para extensão (criar novos padrões)
/// - Fechado para modificação (não precisa mexer no gerador)
/// 
/// CONTRATO RÍGIDO:
/// - Implementações DEVEM respeitar bounds (0..width-1, 0..height-1)
/// - Implementações DEVEM evitar duplicatas
/// - Implementações DEVEM tentar atingir slotCount (ou retornar Fail)
/// - Se impossível, retornar Fail com explicação clara
/// 
/// RESPONSABILIDADE:
/// Gerar lista de coordenadas (Vector2Int) que devem ser desbloqueadas
/// baseado em regras específicas do padrão.
/// </summary>
public interface IUnlockPattern
{
    /// <summary>
    /// Nome do padrão para debug/logs.
    /// </summary>
    string PatternName { get; }

    /// <summary>
    /// Gera lista de coordenadas a serem desbloqueadas com validação.
    /// 
    /// GARANTIAS:
    /// - Retorna Success=true se conseguiu gerar
    /// - Pattern está limpo (sem duplicatas, dentro dos bounds)
    /// - Se falhar, ValidationErrors explica o motivo
    /// </summary>
    /// <param name="gridWidth">Largura do grid (ex: 5)</param>
    /// <param name="gridHeight">Altura do grid (ex: 5)</param>
    /// <param name="slotCount">Quantidade de slots a desbloquear (ex: 5)</param>
    /// <param name="rng">Gerador de números aleatórios (para determinismo via seed)</param>
    /// <returns>Resultado com padrão validado ou erro</returns>
    PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng);
}

