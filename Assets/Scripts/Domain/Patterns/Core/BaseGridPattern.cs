using System.Collections.Generic;

/// <summary>
/// Classe base abstrata para implementações de padrões.
/// 
/// BENEFÍCIOS:
/// - Elimina código duplicado (propriedades PatternID, DisplayName, BaseScore)
/// - Garante consistência (todos usam dados do SO)
/// - Simplifica implementações (foco apenas na geometria)
/// 
/// USO:
/// public class MeuPattern : BaseGridPattern
/// {
///     public MeuPattern(PatternDefinitionSO definition) : base(definition) { }
///     
///     public override List<PatternMatch> DetectAll(IGridService gridService)
///     {
///         // Lógica de detecção aqui
///     }
/// }
/// </summary>
public abstract class BaseGridPattern : IGridPattern
{
    protected readonly PatternDefinitionSO _definition;
    
    /// <summary>
    /// ID estável do padrão. Vem do ScriptableObject.
    /// </summary>
    public string PatternID => _definition?.PatternID ?? "UNKNOWN";
    
    /// <summary>
    /// Nome exibido. Vem do ScriptableObject.
    /// </summary>
    public string DisplayName => _definition?.DisplayName ?? "Unknown Pattern";
    
    /// <summary>
    /// Pontuação base. Vem do ScriptableObject.
    /// </summary>
    public int BaseScore => _definition?.BaseScore ?? 0;
    
    /// <summary>
    /// Construtor base. Todas as subclasses devem receber PatternDefinitionSO.
    /// </summary>
    /// <param name="definition">Configuração do padrão</param>
    protected BaseGridPattern(PatternDefinitionSO definition)
    {
        _definition = definition;
    }
    
    /// <summary>
    /// Método abstrato que implementações devem sobrescrever.
    /// </summary>
    public abstract List<PatternMatch> DetectAll(IGridService gridService);
    
    /// <summary>
    /// Helper para criar PatternMatch com dados do SO.
    /// </summary>
    protected PatternMatch CreateMatch(List<int> slotIndices, List<CropID> cropIDs = null, string debugDescription = "")
    {
        return PatternMatch.Create(
            patternID: this.PatternID,
            displayName: this.DisplayName,
            slotIndices: slotIndices,
            baseScore: this.BaseScore,
            cropIDs: cropIDs,
            debugDescription: debugDescription
        );
    }
}
