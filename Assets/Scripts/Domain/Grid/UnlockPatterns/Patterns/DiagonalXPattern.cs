using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Padrão diagonal em X.
/// 
/// Exemplo 5 slots em grid 5x5:
///     X . . . X
///     . X . X .
///     . . X . .  (centro + diagonais)
///     . X . X .
///     X . . . X
/// 
/// Se slotCount > 5, expande diagonais simetricamente.
/// </summary>
public class DiagonalXPattern : IUnlockPattern
{
    public string PatternName => "Diagonal X";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng)
    {
        var result = new List<Vector2Int>();
        
        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        // 1. Centro
        result.Add(new Vector2Int(centerX, centerY));

        // 2. Diagonais (expande simetricamente)
        int distance = 1;
        
        while (result.Count < slotCount)
        {
            // Diagonal principal (\)
            if (centerX + distance < gridWidth && centerY - distance >= 0)
                result.Add(new Vector2Int(centerX + distance, centerY - distance));
            
            if (result.Count >= slotCount) break;
            
            if (centerX - distance >= 0 && centerY + distance < gridHeight)
                result.Add(new Vector2Int(centerX - distance, centerY + distance));

            if (result.Count >= slotCount) break;

            // Diagonal secundária (/)
            if (centerX + distance < gridWidth && centerY + distance < gridHeight)
                result.Add(new Vector2Int(centerX + distance, centerY + distance));

            if (result.Count >= slotCount) break;

            if (centerX - distance >= 0 && centerY - distance >= 0)
                result.Add(new Vector2Int(centerX - distance, centerY - distance));

            distance++;
        }

        return PatternResult.Ok(result);
    }
}
