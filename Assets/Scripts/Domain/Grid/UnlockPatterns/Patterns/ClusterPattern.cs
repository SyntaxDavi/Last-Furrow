using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gera slots agrupados (estilo "mancha" ou orgânico).
/// Começa em um ponto aleatório e expande para os vizinhos.
/// </summary>
public class ClusterPattern : IUnlockPattern
{
    public string PatternName => "Cluster (Organic)";

    public PatternResult Generate(int gridWidth, int gridHeight, int slotCount, IRandomProvider rng)
    {
        var result = new List<Vector2Int>();
        var candidates = new List<Vector2Int>();

        // 1. Slot inicial aleatório
        int startX = rng.Range(0, gridWidth);
        int startY = rng.Range(0, gridHeight);
        result.Add(new Vector2Int(startX, startY));

        // 2. Adiciona vizinhos ao pool de candidatos
        AddNeighborsToCandidates(new Vector2Int(startX, startY), gridWidth, gridHeight, result, candidates);

        // 3. Expande até atingir slotCount
        while (result.Count < slotCount && candidates.Count > 0)
        {
            // Escolhe candidato aleatório
            int index = rng.Range(0, candidates.Count);
            var chosen = candidates[index];
            candidates.RemoveAt(index);

            result.Add(chosen);

            // Adiciona novos vizinhos
            AddNeighborsToCandidates(chosen, gridWidth, gridHeight, result, candidates);
        }

        return PatternResult.Ok(result);
    }

    private void AddNeighborsToCandidates(Vector2Int slot, int gridWidth, int gridHeight, 
        List<Vector2Int> existing, List<Vector2Int> candidates)
    {
        // 4 direções cardinais
        Vector2Int[] directions = {
            new Vector2Int(0, 1),  // Cima
            new Vector2Int(0, -1), // Baixo
            new Vector2Int(1, 0),  // Direita
            new Vector2Int(-1, 0)  // Esquerda
        };

        foreach (var dir in directions)
        {
            var neighbor = slot + dir;

            // Valida bounds
            if (neighbor.x < 0 || neighbor.x >= gridWidth || neighbor.y < 0 || neighbor.y >= gridHeight)
                continue;

            // Não adiciona se já existe ou já está nos candidatos
            if (existing.Contains(neighbor) || candidates.Contains(neighbor))
                continue;

            candidates.Add(neighbor);
        }
    }
}
