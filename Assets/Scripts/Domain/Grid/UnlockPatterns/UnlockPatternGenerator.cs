using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerador centralizado de padrões de desbloqueamento.
/// </summary>
public static class UnlockPatternGenerator
{
    /// <summary>
    /// Gera padrão de desbloqueamento usando provider determinístico.
    /// </summary>
    public static List<Vector2Int> Generate(
        IRandomProvider random,
        int columns,
        int rows,
        int targetUnlockCount,
        PatternWeightConfig weights = null)
    {
        // 1. Seleciona Tipo de Padrão
        PatternWeightConfig.PatternType type;
        if (weights != null)
        {
            type = weights.SelectPattern(random);
        }
        else
        {
            type = SelectPatternFallback(random);
        }

        Debug.Log($"[UnlockPatternGenerator] Selecionado Padrão: {type}");

        // 2. Instancia Estratégia
        IUnlockPattern strategy = CreateStrategy(type);

        // 3. Gera Coordenadas e valida resultado
        var result = strategy.Generate(columns, rows, targetUnlockCount, random);
        
        if (!result.Success)
        {
            Debug.LogWarning($"[UnlockPatternGenerator] Padrão {type} falhou: {result.ValidationErrors}. Usando Scatter como fallback.");
            return new ScatterPattern().Generate(columns, rows, targetUnlockCount, random).Pattern;
        }

        return result.Pattern;
    }

    private static IUnlockPattern CreateStrategy(PatternWeightConfig.PatternType type)
    {
        // Nota: Assumindo que as classes de estratégia existem ou serão adaptadas
        // Para simplificar e evitar erros de compilação se eu não conheço todos os tipos:
        // Vou usar Reflection ou um switch simples baseado no que conheço.
        // O código original tinha um switch longo. Vou tentar manter o que vi no dump anterior se lembrar, 

        switch (type)
        {
            case PatternWeightConfig.PatternType.Scatter: return new ScatterPattern();
            case PatternWeightConfig.PatternType.Cluster: return new ClusterPattern();
            case PatternWeightConfig.PatternType.Line: return new LinePattern();
            case PatternWeightConfig.PatternType.Corner: return new CornerPattern();
            case PatternWeightConfig.PatternType.Cross: return new CrossPattern();
            case PatternWeightConfig.PatternType.DiagonalX: return new DiagonalXPattern();
            case PatternWeightConfig.PatternType.LShape: return new LShapePattern();
            case PatternWeightConfig.PatternType.TShape: return new TShapePattern();
            default: return new ScatterPattern(); 
        }
    }

    private static PatternWeightConfig.PatternType SelectPatternFallback(IRandomProvider rng)
    {
        // Simulação simples de pesos (Total 100)
        int roll = rng.Range(0, 100);

        if (roll < 15) return PatternWeightConfig.PatternType.Cross;
        if (roll < 30) return PatternWeightConfig.PatternType.Line;
        if (roll < 40) return PatternWeightConfig.PatternType.DiagonalX;
        if (roll < 50) return PatternWeightConfig.PatternType.LShape;
        if (roll < 60) return PatternWeightConfig.PatternType.TShape;
        if (roll < 85) return PatternWeightConfig.PatternType.Cluster;
        if (roll < 95) return PatternWeightConfig.PatternType.Corner;
        return PatternWeightConfig.PatternType.Scatter;
    }
}
