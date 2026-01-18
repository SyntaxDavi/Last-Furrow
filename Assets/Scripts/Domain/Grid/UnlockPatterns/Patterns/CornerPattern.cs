using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Padrão de canto compacto (quadrado 2×2 ou 3×3 em um dos cantos).
/// 
/// Exemplo 5 slots (canto superior esquerdo):
///     X X X . .
///     X X . . .
///     . . . . .
/// 
/// Forma área compacta em um dos 4 cantos.
/// </summary>
public class CornerPattern : IUnlockPattern
{
    public string PatternName => "Corner Block";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng)
    {
        var result = new List<Vector2Int>();
        
        // Escolhe canto aleatório
        int corner = rng.Next(4);

        // Calcula dimensões do bloco (tenta formar quadrado)
        int size = Mathf.CeilToInt(Mathf.Sqrt(slotCount));

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
