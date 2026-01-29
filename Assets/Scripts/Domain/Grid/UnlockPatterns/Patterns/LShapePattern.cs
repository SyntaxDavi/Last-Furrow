using System.Collections.Generic;
using UnityEngine;

public class LShapePattern : IUnlockPattern
{
    public string PatternName => "L-Shape";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng)
    {
        var result = new List<Vector2Int>();  

        // Escolhe um dos 4 cantos para rotacionar o L
        int corner = rng.Range(0, 4);
        int armLength = slotCount / 2 + 1;

        switch (corner)
        {
            case 0: // Top-Left
                for (int x = 0; x < armLength && x < gridWidth && result.Count < slotCount; x++)
                    result.Add(new Vector2Int(x, gridHeight - 1));
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
