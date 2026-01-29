using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resultado da geração de padrão (contrato rígido).
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
/// </summary>
public interface IUnlockPattern
{
    string PatternName { get; }

    /// <summary>
    /// Gera lista de coordenadas a serem desbloqueadas com validação.
    /// </summary>
    PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng);
}
