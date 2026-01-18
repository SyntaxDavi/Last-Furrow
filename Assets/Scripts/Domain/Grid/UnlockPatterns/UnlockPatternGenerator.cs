using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerador centralizado de padrões de desbloqueamento.
/// 
/// RESPONSABILIDADE:
/// - Selecionar padrão baseado em pesos (via PatternWeightConfig)
/// - Instanciar padrão correto
/// - Gerar coordenadas finais
/// 
/// SOLID - Factory Pattern:
/// - Encapsula lógica de criação de padrões
/// - Desacopla consumidores (GridService) dos padrões concretos
/// </summary>
public static class UnlockPatternGenerator
{
    /// <summary>
    /// Gera padrão de desbloqueamento usando seed determinístico.
    /// </summary>
    /// <param name="seed">Seed para reprodutibilidade (ex: timestamp, RunID)</param>
    /// <param name="slotCount">Quantidade de slots a desbloquear</param>
    /// <param name="gridWidth">Largura do grid</param>
    /// <param name="gridHeight">Altura do grid</param>
    /// <param name="config">Configuração de pesos (opcional, usa padrão se null)</param>
    /// <returns>Lista de coordenadas (x,y) a serem desbloqueadas</returns>
    public static List<Vector2Int> Generate(
        int seed, 
        int slotCount, 
        int gridWidth, 
        int gridHeight,
        PatternWeightConfig config = null)
    {
        // 1. Cria RNG com seed
        var rng = new System.Random(seed);

        // 2. Seleciona padrão (usa config padrão se null)
        PatternWeightConfig.PatternType patternType;
        
        if (config != null)
        {
            patternType = config.SelectPattern(rng);
        }
        else
        {
            // Fallback: pesos hardcoded
            patternType = SelectPatternFallback(rng);
        }

        // 3. Instancia padrão correto (Factory)
        IUnlockPattern pattern = CreatePattern(patternType);

        // 4. Gera coordenadas com validação
        var result = pattern.Generate(gridWidth, gridHeight, slotCount, rng);

        if (!result.Success)
        {
            Debug.LogError(
                $"[UnlockPatternGenerator] ? FALHA ao gerar padrão {pattern.PatternName}!\n" +
                $"Seed: {seed} | Erros: {result.ValidationErrors}\n" +
                $"? Usando fallback (Cluster)."
            );

            // Fallback: tenta Cluster
            var fallback = new ClusterPattern().Generate(gridWidth, gridHeight, slotCount, rng);
            if (!fallback.Success)
            {
                // Último recurso: retorna lista vazia
                Debug.LogError("[UnlockPatternGenerator] ? CRÍTICO: Até fallback falhou!");
                return new List<Vector2Int>();
            }
            result = fallback;
        }

        Debug.Log(
            $"[UnlockPatternGenerator] ? Gerado: {pattern.PatternName} | " +
            $"Seed: {seed} | Slots: {result.Pattern.Count}/{slotCount}"
        );

        return result.Pattern;
    }

    /// <summary>
    /// Factory Method: Cria instância do padrão baseado no tipo.
    /// 
    /// ?? PONTO DE RIGIDEZ INTENCIONAL:
    /// Este switch é necessário hoje, mas pode ser substituído no futuro por:
    /// - Dictionary<PatternType, Func<IUnlockPattern>> (registration-based)
    /// - Reflection (auto-discovery de implementações)
    /// - Injeção via container DI
    /// 
    /// ?? PONTO DE FUGA PLANEJADO:
    /// Se precisar adicionar muitos padrões, extrair para:
    /// PatternRegistry.Register(PatternType.NewPattern, () => new NewPattern());
    /// </summary>
    private static IUnlockPattern CreatePattern(PatternWeightConfig.PatternType type)
    {
        // TODO FUTURO: Substituir este switch por registry quando número de padrões > 15
        switch (type)
        {
            case PatternWeightConfig.PatternType.Cross:
                return new CrossPattern();
            case PatternWeightConfig.PatternType.Line:
                return new LinePattern();
            case PatternWeightConfig.PatternType.DiagonalX:
                return new DiagonalXPattern();
            case PatternWeightConfig.PatternType.LShape:
                return new LShapePattern();
            case PatternWeightConfig.PatternType.TShape:
                return new TShapePattern();
            case PatternWeightConfig.PatternType.Cluster:
                return new ClusterPattern();
            case PatternWeightConfig.PatternType.Corner:
                return new CornerPattern();
            case PatternWeightConfig.PatternType.Scatter:
                return new ScatterPattern();
            default:
                Debug.LogWarning($"[UnlockPatternGenerator] ?? Tipo desconhecido: {type}. Usando Cluster como fallback.");
                return new ClusterPattern();
        }
    }

    /// <summary>
    /// Fallback caso PatternWeightConfig não seja fornecido.
    /// </summary>
    private static PatternWeightConfig.PatternType SelectPatternFallback(System.Random rng)
    {
        int roll = rng.Next(100);

        // Pesos hardcoded (total = 100)
        if (roll < 15) return PatternWeightConfig.PatternType.Cross;       // 15%
        if (roll < 30) return PatternWeightConfig.PatternType.Line;        // 15%
        if (roll < 40) return PatternWeightConfig.PatternType.DiagonalX;   // 10%
        if (roll < 50) return PatternWeightConfig.PatternType.LShape;      // 10%
        if (roll < 60) return PatternWeightConfig.PatternType.TShape;      // 10%
        if (roll < 85) return PatternWeightConfig.PatternType.Cluster;     // 25%
        if (roll < 95) return PatternWeightConfig.PatternType.Corner;      // 10%
        return PatternWeightConfig.PatternType.Scatter;                     // 5%
    }
}
