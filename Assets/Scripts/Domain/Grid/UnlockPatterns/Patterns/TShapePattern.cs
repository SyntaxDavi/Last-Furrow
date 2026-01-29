using System.Collections.Generic;
using UnityEngine;

public class TShapePattern : IUnlockPattern
{
    public string PatternName => "T-Shape";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng)
    {
        var result = new List<Vector2Int>();  

        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        // Escolhe uma das 4 orientações
        int orientation = rng.Range(0, 4);

        int barLength = (slotCount * 2) / 3;
        int stemLength = slotCount - barLength;

        int startX, startY;

        switch (orientation)
        {
            case 0: // T normal (barra em cima, haste desce)
                // Barra horizontal
                startX = centerX - barLength / 2;
                for (int i = 0; i < barLength && result.Count < slotCount; i++)
                    if (startX + i >= 0 && startX + i < gridWidth)
                        result.Add(new Vector2Int(startX + i, centerY));
                // Haste vertical (baixo)
                for (int i = 1; i <= stemLength && result.Count < slotCount; i++)
                    if (centerY - i >= 0)
                        result.Add(new Vector2Int(centerX, centerY - i));
                break;

            case 1: // T invertido (barra em baixo, haste sobe)
                // Barra horizontal
                startX = centerX - barLength / 2;
                for (int i = 0; i < barLength && result.Count < slotCount; i++)
                    if (startX + i >= 0 && startX + i < gridWidth)
                        result.Add(new Vector2Int(startX + i, centerY));
                // Haste vertical (cima)
                for (int i = 1; i <= stemLength && result.Count < slotCount; i++)
                    if (centerY + i < gridHeight)
                        result.Add(new Vector2Int(centerX, centerY + i));
                break;

            case 2: // T esquerda (barra vertical esquerda, haste direita)
                // Barra vertical
                startY = centerY - barLength / 2;
                for (int i = 0; i < barLength && result.Count < slotCount; i++)
                    if (startY + i >= 0 && startY + i < gridHeight)
                        result.Add(new Vector2Int(centerX, startY + i));
                // Haste horizontal (direita) 
                for (int i = 1; i <= stemLength && result.Count < slotCount; i++)
                    if (centerX + i < gridWidth)
                        result.Add(new Vector2Int(centerX + i, centerY));
                break;

            case 3: // T direita (barra vertical direita, haste esquerda)
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
