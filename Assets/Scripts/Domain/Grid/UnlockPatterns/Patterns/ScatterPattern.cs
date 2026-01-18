using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Padrão totalmente disperso (ilhas não conectadas).
/// 
/// Exemplo 5 slots:
///     X . . . X
///     . . X . .
///     . X . . .
///     . . . X .
/// 
/// Slots podem estar em qualquer lugar, sem necessariamente formar área conectada.
/// </summary>
public class ScatterPattern : IUnlockPattern
{
    public string PatternName => "Scatter (Islands)";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, System.Random rng)
    {
        var result = new List<Vector2Int>();
        var allSlots = new List<Vector2Int>();

        // Cria lista de todos os slots possíveis
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                allSlots.Add(new Vector2Int(x, y));
            }
        }

        // Embaralha (Fisher-Yates shuffle)
        for (int i = allSlots.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var temp = allSlots[i];
            allSlots[i] = allSlots[j];
            allSlots[j] = temp;
        }

        // Pega os primeiros slotCount slots
        for (int i = 0; i < slotCount && i < allSlots.Count; i++)
        {
            result.Add(allSlots[i]);
        }

        return PatternResult.Ok(result);
    }
}
