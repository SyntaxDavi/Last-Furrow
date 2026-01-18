using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Padrão em forma de T (pode rotacionar para 4 direções).
/// 
/// Exemplo 5 slots (T normal):
///     . . . . .
///     X X X X X  (barra horizontal)
///     . . X . .  (haste vertical)
///     . . X . .
/// 
/// Pode rotacionar: T, ?, ?, ?
/// </summary>
public class TShapePattern : IUnlockPattern
{
    public string PatternName => "T-Shape";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng)
    {
        var result = new List<Vector2Int>();
        
        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;
        
        // Rotação aleatória (0=Normal T, 1=Invertido, 2=Esquerda, 3=Direita)
        int rotation = rng.Next(4);

        int barLength = Mathf.Max(3, slotCount - 2); // Barra tem no mínimo 3
        int stemLength = slotCount - barLength;      // Haste com o resto

        switch (rotation)
        {
            case 0: // T normal (barra em cima, haste para baixo)
                // Barra horizontal
                int startX = centerX - barLength / 2;
                for (int i = 0; i < barLength && result.Count < slotCount; i++)
                    if (startX + i >= 0 && startX + i < gridWidth)
                        result.Add(new Vector2Int(startX + i, centerY));
                // Haste vertical (desce)
                for (int i = 1; i <= stemLength && result.Count < slotCount; i++)
                    if (centerY - i >= 0)
                        result.Add(new Vector2Int(centerX, centerY - i));
                break;

            case 1: // T invertido (?)
                // Barra horizontal
                startX = centerX - barLength / 2;
                for (int i = 0; i < barLength && result.Count < slotCount; i++)
                    if (startX + i >= 0 && startX + i < gridWidth)
                        result.Add(new Vector2Int(startX + i, centerY));
                // Haste vertical (sobe)
                for (int i = 1; i <= stemLength && result.Count < slotCount; i++)
                    if (centerY + i < gridHeight)
                        result.Add(new Vector2Int(centerX, centerY + i));
                break;

            case 2: // T esquerda (?)
                // Barra vertical
                int startY = centerY - barLength / 2;
                for (int i = 0; i < barLength && result.Count < slotCount; i++)
                    if (startY + i >= 0 && startY + i < gridHeight)
                        result.Add(new Vector2Int(centerX, startY + i));
                // Haste horizontal (direita)
                for (int i = 1; i <= stemLength && result.Count < slotCount; i++)
                    if (centerX + i < gridWidth)
                        result.Add(new Vector2Int(centerX + i, centerY));
                break;

            case 3: // T direita (?)
                // Barra vertical
                startY = centerY - barLength / 2;
                for (int i = 0; i < barLength && result.Count < slotCount; i++)
                    if (startY + i >= 0 && startY + i < gridHeight)
                        result.Add(new Vector2Int(centerX, startY + i));
                // Haste horizontal (esquerda)
                for (int i = 1; i <= stemLength && result.Count < slotCount; i++)
                    if (centerX - i >= 0)
                        result.Add(new Vector2Int(centerX - i, centerY));
                break;
        }

        return PatternResult.Ok(result);
    }
}
