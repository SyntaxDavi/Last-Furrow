using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Padrão de linha (horizontal ou vertical) aleatória.
/// 
/// Exemplo 5 slots horizontal:
///     . . . . .
///     X X X X X
///     . . . . .
/// 
/// Exemplo 5 slots vertical:
///     . X . . .
///     . X . . .
///     . X . . .
///     . X . . .
///     . X . . .
/// </summary>
public class LinePattern : IUnlockPattern
{
    public string PatternName => "Line (H/V)";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng)
    {
        var result = new List<Vector2Int>();
        
        bool isHorizontal = rng.Next(2) == 0;

        if (isHorizontal)
        {
            // Linha horizontal centralizada
            int row = gridHeight / 2;
            int startCol = Mathf.Max(0, (gridWidth - slotCount) / 2);

            for (int i = 0; i < slotCount && startCol + i < gridWidth; i++)
            {
                result.Add(new Vector2Int(startCol + i, row));
            }
        }
        else
        {
            // Linha vertical centralizada
            int col = gridWidth / 2;
            int startRow = Mathf.Max(0, (gridHeight - slotCount) / 2);

            for (int i = 0; i < slotCount && startRow + i < gridHeight; i++)
            {
                result.Add(new Vector2Int(col, startRow + i));
            }
        }

        return PatternResult.Ok(result);
    }
}
