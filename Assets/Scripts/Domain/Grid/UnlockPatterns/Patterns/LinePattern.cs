using System.Collections.Generic;
using UnityEngine;

public class LinePattern : IUnlockPattern
{
    public string PatternName => "Line";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng)
    {
        var result = new List<Vector2Int>();

        bool isHorizontal = rng.NextBool();

        if (isHorizontal)
        {
            // Linha horizontal centralizada
            int row = gridHeight / 2;
            // Adiciona variação aleatória na linha? O original era fixo no centro.
            // Para manter igual:
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
