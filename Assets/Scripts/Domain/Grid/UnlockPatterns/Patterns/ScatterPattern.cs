using System.Collections.Generic;
using UnityEngine;

public class ScatterPattern : IUnlockPattern
{
    public string PatternName => "Scatter";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng)
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

        // Embaralha usando Provider
        rng.Shuffle(allSlots);

        // Pega os primeiros slotCount slots
        for (int i = 0; i < slotCount && i < allSlots.Count; i++)
        {
            result.Add(allSlots[i]);
        }

        return PatternResult.Ok(result);
    }
}
