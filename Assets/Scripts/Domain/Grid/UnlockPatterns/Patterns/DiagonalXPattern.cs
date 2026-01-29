using System.Collections.Generic;
using UnityEngine;

public class DiagonalXPattern : IUnlockPattern
{
    public string PatternName => "Diagonal X";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng)
    {
        var result = new List<Vector2Int>();  

        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        // 1. Centro
        result.Add(new Vector2Int(centerX, centerY));

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

            // Failsafe para evitar loop infinito se slotCount for maior que o possível na diagonal
            if (distance > gridWidth && distance > gridHeight) break;
        }

        return PatternResult.Ok(result);      
    }
}
