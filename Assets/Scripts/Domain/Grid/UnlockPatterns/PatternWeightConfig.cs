using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject para configurar pesos de probabilidade dos padrões de desbloqueamento.
/// 
/// EXTENSIBILIDADE:
/// - Adicione novos padrões aqui sem modificar código
/// - Ajuste pesos no Inspector sem recompilar
/// - Crie múltiplas configs para diferentes níveis/dificuldades
/// </summary>
[CreateAssetMenu(fileName = "PatternWeightConfig", menuName = "Last Furrow/Grid/Pattern Weight Config")]
public class PatternWeightConfig : ScriptableObject
{
    [System.Serializable]
    public class PatternWeight
    {
        [Tooltip("Tipo do padrão (identidade estável - enum)")]
        public PatternType Type;

        [Tooltip("Peso relativo (ex: 20 = 20% se total = 100)")]
        [Range(0, 100)]
        public int Weight = 10;

        [Header("Metadata (Opcional - Não Afeta Lógica)")]
        [Tooltip("Nome exibido na UI (opcional - fallback: Type.ToString())")]
        public string DisplayName = "";

        [Tooltip("Descrição para designers (opcional)")]
        [TextArea(2, 4)]
        public string Description = "";

        /// <summary>
        /// Retorna nome de exibição (usa DisplayName ou fallback para enum).
        /// </summary>
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(DisplayName) ? Type.ToString() : DisplayName;
        }
    }

    public enum PatternType
    {
        Cross,
        Line,
        DiagonalX,
        LShape,
        TShape,
        Cluster,
        Corner,
        Scatter
    }

    [Header("Configuração de Probabilidades")]
    [Tooltip("Lista de padrões com seus pesos. Total não precisa ser 100.")]
    public List<PatternWeight> Patterns = new List<PatternWeight>
    {
        new PatternWeight { Type = PatternType.Cross, Weight = 14, DisplayName = "Cruz (+)" },
        new PatternWeight { Type = PatternType.Line, Weight = 13, DisplayName = "Linha" },
        new PatternWeight { Type = PatternType.DiagonalX, Weight = 12, DisplayName = "Diagonal X" },
        new PatternWeight { Type = PatternType.LShape, Weight = 10, DisplayName = "L-Shape" },
        new PatternWeight { Type = PatternType.TShape, Weight = 5, DisplayName = "T-Shape" },
        new PatternWeight { Type = PatternType.Cluster, Weight = 22, DisplayName = "Cluster" },
        new PatternWeight { Type = PatternType.Corner, Weight = 8, DisplayName = "Canto" },
        new PatternWeight { Type = PatternType.Scatter, Weight = 25, DisplayName = "Disperso" }
    };

    [Header("Debug")]
    [Tooltip("Força um padrão específico (desabilita aleatoriedade)")]
    public bool ForcePattern = false;
    public PatternType ForcedPatternType = PatternType.Cross;

    /// <summary>
    /// Calcula peso total para normalização.
    /// </summary>
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
    /// Seleciona padrão baseado em pesos probabilísticos.
    /// </summary>
    public PatternType SelectPattern(System.Random rng)
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

        int roll = rng.Next(totalWeight);
        int accumulated = 0;

        foreach (var pattern in Patterns)
        {
            accumulated += pattern.Weight;
            if (roll < accumulated)
            {
                return pattern.Type;
            }
        }

        // Fallback (não deveria chegar aqui)
        return PatternType.Cluster;
    }
}
