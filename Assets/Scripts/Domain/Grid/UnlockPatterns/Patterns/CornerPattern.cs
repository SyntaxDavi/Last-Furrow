using System.Collections.Generic;
using UnityEngine;

public class CornerPattern : IUnlockPattern
{
    public string PatternName => "Corner";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng)
    {
        var result = new List<Vector2Int>();
        
        // Determina tamanho do bloco (ex: se pede 4 slots, faz 2x2 se possível)
        int size = Mathf.CeilToInt(Mathf.Sqrt(slotCount));

        // Escolhe um dos 4 cantos
        int corner = rng.Range(0, 4);
        int startX = 0, startY = 0;
        int dirX = 1, dirY = 1;

        switch (corner)
        {
            case 0: // Top-Left
                startX = 0;
                startY = gridHeight - 1;
                dirX = 1;
                dirY = -1;
                break;
            case 1: // Top-Right
                startX = gridWidth - 1;
                startY = gridHeight - 1;
                dirX = -1;
                dirY = -1;
                break;
            case 2: // Bottom-Left
                startX = 0;
                startY = 0;
                dirX = 1;
                dirY = 1;
                break;
            case 3: // Bottom-Right
                startX = gridWidth - 1;
                startY = 0;
                dirX = -1;
                dirY = 1;
                break;
        }

        // Preenche bloco
        for (int y = 0; y < size && result.Count < slotCount; y++)
        {
            for (int x = 0; x < size && result.Count < slotCount; x++)
            {
                int posX = startX + (x * dirX);
                int posY = startY + (y * dirY);

                if (posX >= 0 && posX < gridWidth && posY >= 0 && posY < gridHeight)
                    result.Add(new Vector2Int(posX, posY));
            }
        }

        return PatternResult.Ok(result);
    }
}
