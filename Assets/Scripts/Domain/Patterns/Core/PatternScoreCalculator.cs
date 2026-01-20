using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Autoridade ÚNICA de pontuação do Pattern System (REFATORADO - ONDA 5.5).
/// 
/// RESPONSABILIDADE:
/// - Orquestrar modificadores de score (Strategy Pattern)
/// - Calcular sinergia global
/// - Retornar resultados com metadata (DTO)
/// - NÃO disparar eventos (isso é responsabilidade do caller)
/// 
/// MUDANÇAS vs Versão Original:
/// - ? Usa IScoreModifier implementations (extensível)
/// - ? Retorna PatternScoreResult (DTO) ao invés de disparar eventos
/// - ? Remove acoplamento com AppCore.Instance
/// - ? Remove hardcoded PatternIDs (usa PatternIDs constants)
/// - ? Segue Single Responsibility Principle
/// - ? Testável (pode injetar mock modifiers)
/// 
/// FÓRMULA:
/// score = baseScore × modifier1 × modifier2 × ... × modifierN
/// totalScore = sum(scores) × synergyMultiplier
/// </summary>
public class PatternScoreCalculator
{
    private readonly List<IScoreModifier> _modifiers;
    
    /// <summary>
    /// Construtor com Dependency Injection de library.
    /// Inicializa todos os modificadores de score.
    /// </summary>
    public PatternScoreCalculator(IGameLibrary library)
    {
        _modifiers = new List<IScoreModifier>
        {
            new CropValueModifier(library),
            new MaturityModifier(library),
            new DecayModifier(),
            new RecreationBonusModifier(),
            new DiversityModifier()
        };
    }
    
    /// <summary>
    /// Construtor alternativo para testes (inject custom modifiers).
    /// </summary>
    public PatternScoreCalculator(List<IScoreModifier> customModifiers)
    {
        _modifiers = customModifiers ?? new List<IScoreModifier>();
    }
    
    /// <summary>
    /// Calcula score total de todos os padrões (backward compatibility).
    /// </summary>
    public int CalculateTotal(List<PatternMatch> matches, IGridService gridService)
    {
        var results = CalculateTotalWithMetadata(matches, gridService);
        return results.TotalScore;
    }
    
    /// <summary>
    /// Calcula score total de todos os padrões COM METADATA (versão completa).
    /// 
    /// RETORNO:
    /// - TotalScore: Pontuação final com sinergia
    /// - IndividualResults: Lista de PatternScoreResult para cada padrão
    /// - SynergyMultiplier: Multiplicador de sinergia aplicado
    /// 
    /// USO:
    /// var results = calculator.CalculateTotalWithMetadata(matches, gridService);
    /// foreach (var result in results.IndividualResults) {
    ///     if (result.HasDecay) {
    ///         events.TriggerDecayApplied(result);
    ///     }
    /// }
    /// </summary>
    public PatternScoreTotalResult CalculateTotalWithMetadata(List<PatternMatch> matches, IGridService gridService)
    {
        var totalResult = new PatternScoreTotalResult();
        
        if (matches == null || matches.Count == 0)
        {
            return totalResult;
        }
        
        float totalScore = 0f;
        
        foreach (var match in matches)
        {
            var result = CalculateSingleWithMetadata(match, gridService);
            totalResult.IndividualResults.Add(result);
            totalScore += result.FinalScore;
            
            Debug.Log($"[PatternScoreCalculator] {match.DisplayName}: {match.BaseScore} base ? {result.FinalScore} final");
        }
        
        // Aplicar sinergia se houver múltiplos padrões
        float synergyMultiplier = CalculateSynergyMultiplier(matches.Count);
        float finalTotal = totalScore * synergyMultiplier;
        
        totalResult.TotalScore = Mathf.RoundToInt(finalTotal);
        totalResult.ScoreBeforeSynergy = Mathf.RoundToInt(totalScore);
        totalResult.SynergyMultiplier = synergyMultiplier;
        
        if (matches.Count > 1)
        {
            Debug.Log($"[PatternScoreCalculator] Sinergia ({matches.Count} padrões): {synergyMultiplier:F2}x");
        }
        
        Debug.Log($"[PatternScoreCalculator] === TOTAL: {totalResult.TotalScore} pontos de padrões ===");
        
        return totalResult;
    }
    
    /// <summary>
    /// Calcula score de um único padrão COM METADATA.
    /// 
    /// RETORNO:
    /// PatternScoreResult contendo:
    /// - FinalScore
    /// - Lista de Modifiers aplicados
    /// - Flags de decay/recreation
    /// </summary>
    public PatternScoreResult CalculateSingleWithMetadata(PatternMatch match, IGridService gridService)
    {
        var result = new PatternScoreResult
        {
            Match = match,
            BaseScore = match.BaseScore,
            DaysActive = match.DaysActive,
            HasRecreationBonus = match.HasRecreationBonus
        };
        
        float score = match.BaseScore;
        
        // Aplicar todos os modificadores
        foreach (var modifier in _modifiers)
        {
            if (modifier.IsApplicable(match))
            {
                float multiplier = modifier.Calculate(match, gridService);
                score *= multiplier;
                
                result.Modifiers.Add(new ScoreModifier(
                    modifier.Name,
                    multiplier,
                    GetModifierDescription(modifier, match, multiplier)
                ));
                
                // Armazenar decay multiplier específico para eventos
                if (modifier is DecayModifier)
                {
                    result.DecayMultiplier = multiplier;
                }
            }
        }
        
        result.FinalScore = Mathf.RoundToInt(score);
        return result;
    }
    
    /// <summary>
    /// Gera descrição legível do modificador para logs/UI.
    /// </summary>
    private string GetModifierDescription(IScoreModifier modifier, PatternMatch match, float multiplier)
    {
        if (modifier is DecayModifier && match.DaysActive > 1)
        {
            return $"Dia {match.DaysActive}";
        }
        else if (modifier is DiversityModifier && match.CropIDs != null)
        {
            var uniqueCrops = new HashSet<CropID>(match.CropIDs);
            return $"{uniqueCrops.Count} tipos";
        }
        else if (modifier is RecreationBonusModifier && match.HasRecreationBonus)
        {
            return "+10%";
        }
        
        return "";
    }
    
    /// <summary>
    /// Calcula multiplicador de sinergia (soft cap logarítmico).
    /// 
    /// FÓRMULA: 1.0 + 0.2 × log2(patternCount)
    /// - 1 padrão = 1.0x (sem bonus)
    /// - 2 padrões = 1.2x (+20%)
    /// - 4 padrões = 1.4x (+40%)
    /// - 8 padrões = 1.6x (+60%)
    /// </summary>
    private float CalculateSynergyMultiplier(int patternCount)
    {
        if (patternCount <= 1) return 1f;
        
        float logValue = Mathf.Log(patternCount) / Mathf.Log(2);
        return 1f + (0.2f * logValue);
    }
}

/// <summary>
/// DTO que representa o resultado completo de todos os padrões.
/// </summary>
public class PatternScoreTotalResult
{
    public int TotalScore { get; set; }
    public int ScoreBeforeSynergy { get; set; }
    public float SynergyMultiplier { get; set; }
    public List<PatternScoreResult> IndividualResults { get; set; }
    
    public PatternScoreTotalResult()
    {
        IndividualResults = new List<PatternScoreResult>();
    }
}
