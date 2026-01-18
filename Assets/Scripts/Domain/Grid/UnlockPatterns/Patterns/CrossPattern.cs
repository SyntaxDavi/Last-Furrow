using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Padrão de cruz (+) centralizado.
/// 
/// Exemplo 5 slots em grid 5x5:
///     . . X . .
///     . . X . .
///     X X X X X  (centro + 4 adjacentes cardinais)
///     . . X . .
///     . . X . .
/// 
/// Se slotCount > 5, expande simetricamente.
/// </summary>
public class CrossPattern : IUnlockPattern
{
    public string PatternName => "Cross (+)";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng)
    {
        var result = new List<Vector2Int>();
        
        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        // 1. Centro
        result.Add(new Vector2Int(centerX, centerY));

        // 2. Braços da cruz (expande simetricamente)
        int armsLength = (slotCount - 1) / 4; // Quantos slots por braço
        
        for (int i = 1; i <= armsLength && result.Count < slotCount; i++)
        {
            // Cima
            if (centerY + i < gridHeight) result.Add(new Vector2Int(centerX, centerY + i));
            // Baixo
            if (centerY - i >= 0 && result.Count < slotCount) result.Add(new Vector2Int(centerX, centerY - i));
            // Direita
            if (centerX + i < gridWidth && result.Count < slotCount) result.Add(new Vector2Int(centerX + i, centerY));
            // Esquerda
            if (centerX - i >= 0 && result.Count < slotCount) result.Add(new Vector2Int(centerX - i, centerY));
        }

        return PatternResult.Ok(result);
    }
}
