using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Padrão em forma de L em um dos 4 cantos.
/// 
/// Exemplo 5 slots (canto superior esquerdo):
///     X X X . .
///     X . . . .
///     X . . . .
///     . . . . .
/// 
/// Pode rotacionar para qualquer canto.
/// </summary>
public class LShapePattern : IUnlockPattern
{
    public string PatternName => "L-Shape";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng)
    {
        var result = new List<Vector2Int>();
        
        // Escolhe canto aleatório (0=TopLeft, 1=TopRight, 2=BottomLeft, 3=BottomRight)
        int corner = rng.Next(4);

        int armLength = Mathf.CeilToInt(slotCount / 2f);

        switch (corner)
        {
            case 0: // Top-Left
                // Braço horizontal
                for (int x = 0; x < armLength && x < gridWidth && result.Count < slotCount; x++)
                    result.Add(new Vector2Int(x, gridHeight - 1));
                // Braço vertical (pula primeiro pois já foi adicionado)
                for (int y = gridHeight - 2; y >= gridHeight - armLength && y >= 0 && result.Count < slotCount; y--)
                    result.Add(new Vector2Int(0, y));
                break;

            case 1: // Top-Right
                for (int x = gridWidth - 1; x >= gridWidth - armLength && x >= 0 && result.Count < slotCount; x--)
                    result.Add(new Vector2Int(x, gridHeight - 1));
                for (int y = gridHeight - 2; y >= gridHeight - armLength && y >= 0 && result.Count < slotCount; y--)
                    result.Add(new Vector2Int(gridWidth - 1, y));
                break;

            case 2: // Bottom-Left
                for (int x = 0; x < armLength && x < gridWidth && result.Count < slotCount; x++)
                    result.Add(new Vector2Int(x, 0));
                for (int y = 1; y < armLength && y < gridHeight && result.Count < slotCount; y++)
                    result.Add(new Vector2Int(0, y));
                break;

            case 3: // Bottom-Right
                for (int x = gridWidth - 1; x >= gridWidth - armLength && x >= 0 && result.Count < slotCount; x--)
                    result.Add(new Vector2Int(x, 0));
                for (int y = 1; y < armLength && y < gridHeight && result.Count < slotCount; y++)
                    result.Add(new Vector2Int(gridWidth - 1, y));
                break;
        }

        return PatternResult.Ok(result);
    }
}
