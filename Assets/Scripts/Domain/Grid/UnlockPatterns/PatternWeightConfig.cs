using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Unlock Pattern Weights")]
public class PatternWeightConfig : ScriptableObject
{
    public enum PatternType
    {
        Scatter,    // Aleatório espalhado
        Cluster,    // Agrupado no centro ou canto
        Line,       // Linhas retas (H/V)
        Cross,      // Cruz (+)
        DiagonalX,  // X
        LShape,     // L
        TShape,     // T
        Corner      // Cantos
    }

    [System.Serializable]
    public class PatternWeight
    {
        public PatternType Type;
        [Range(0, 100)] public int Weight;
    }

    public List<PatternWeight> Patterns;
    
    [Header("Debug")]
    public bool ForcePattern;
    public PatternType ForcedPatternType;

    public int GetTotalWeight()
    {
        int total = 0;
        foreach (var p in Patterns)
        {
            total += p.Weight;
        }
        return total;
    }

    /// <summary>
    /// Seleciona padrão baseado em pesos probabilisticos usando provider determinístico.
    /// </summary>
    public PatternType SelectPattern(IRandomProvider rng)
    {
        if (ForcePattern)
        {
            Debug.Log($"[PatternWeightConfig] ?? Forçando padrão: {ForcedPatternType}");
            return ForcedPatternType;
        }

        int totalWeight = GetTotalWeight();
        if (totalWeight == 0)
        {
            Debug.LogWarning("[PatternWeightConfig] Peso total = 0! Usando Cluster como fallback.");
            return PatternType.Cluster;
        }

        // Gera número de 0 a totalWeight-1
        int roll = rng.Range(0, totalWeight);
        int accumulated = 0;

        foreach (var pattern in Patterns)
        {
            accumulated += pattern.Weight;
            if (roll < accumulated)
            {
                return pattern.Type;
            }
        }

        return PatternType.Cluster;
    }
}
