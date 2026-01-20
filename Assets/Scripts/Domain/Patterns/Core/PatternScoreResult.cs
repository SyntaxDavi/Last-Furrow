using System.Collections.Generic;

/// <summary>
/// DTO que representa o resultado completo do cálculo de score de um padrão.
/// 
/// FUNÇÃO:
/// - Separar responsabilidades: Calculator calcula e retorna dados
/// - Quem chama (DetectPatternsStep) decide o que fazer com os dados (disparar eventos, etc)
/// - Segue Single Responsibility Principle
/// 
/// USO:
/// var result = calculator.CalculateSingleWithMetadata(match, gridService);
/// if (result.HasDecay) {
///     events.TriggerDecayApplied(result);
/// }
/// </summary>
public class PatternScoreResult
{
    /// <summary>
    /// Score final calculado (após todos os modificadores).
    /// </summary>
    public int FinalScore { get; set; }
    
    /// <summary>
    /// Score base do padrão (antes de modificadores).
    /// </summary>
    public int BaseScore { get; set; }
    
    /// <summary>
    /// Match original que foi calculado.
    /// </summary>
    public PatternMatch Match { get; set; }
    
    /// <summary>
    /// Lista de modificadores aplicados ao score.
    /// </summary>
    public List<ScoreModifier> Modifiers { get; set; }
    
    /// <summary>
    /// Se o padrão teve decay aplicado (DaysActive > 1).
    /// </summary>
    public bool HasDecay => DaysActive > 1;
    
    /// <summary>
    /// Se o padrão foi recriado e tem bonus.
    /// </summary>
    public bool HasRecreationBonus { get; set; }
    
    /// <summary>
    /// Dias consecutivos ativo (para UI/Analytics).
    /// </summary>
    public int DaysActive { get; set; }
    
    /// <summary>
    /// Multiplicador de decay aplicado (para UI).
    /// </summary>
    public float DecayMultiplier { get; set; }
    
    public PatternScoreResult()
    {
        Modifiers = new List<ScoreModifier>();
    }
}

/// <summary>
/// Representa um modificador individual aplicado ao score.
/// </summary>
public class ScoreModifier
{
    public string Name { get; set; }
    public float Multiplier { get; set; }
    public string Description { get; set; }
    
    public ScoreModifier(string name, float multiplier, string description = "")
    {
        Name = name;
        Multiplier = multiplier;
        Description = description;
    }
    
    public override string ToString()
    {
        return $"{Name}: {Multiplier:F2}x {Description}";
    }
}
