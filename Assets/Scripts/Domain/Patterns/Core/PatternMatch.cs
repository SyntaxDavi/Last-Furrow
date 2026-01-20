using System.Collections.Generic;

/// <summary>
/// DTO puro que representa um padrão detectado no grid.
/// 
/// FUNÇÃO: Transportar resultado de detecção.
/// NÃO DEVE conter lógica de negócio.
/// 
/// CAMPOS:
/// - PatternID: ID estável (não nome exibido)
/// - DisplayName: Nome legível para UI
/// - SlotIndices: Posições exatas dos slots que formam o padrão
/// - BaseScore: Pontuação base vinda do IGridPattern
/// - CropIDs: IDs das crops nos slots (para tracking de identidade)
/// - DaysActive: Dias consecutivos que este padrão está ativo (para decay)
/// - HasRecreationBonus: Se o padrão foi recriado e tem +10% bonus
/// </summary>
public class PatternMatch
{
    /// <summary>
    /// ID estável do padrão (ex: "FULL_LINE").
    /// Usado para SaveData e analytics.
    /// </summary>
    public string PatternID { get; private set; }
    
    /// <summary>
    /// Nome legível para UI/Debug.
    /// </summary>
    public string DisplayName { get; private set; }
    
    /// <summary>
    /// Índices dos slots que formam este padrão.
    /// Ordem pode importar para alguns padrões (ex: linha tem direção).
    /// </summary>
    public List<int> SlotIndices { get; private set; }
    
    /// <summary>
    /// Pontuação base do padrão (sem multiplicadores).
    /// </summary>
    public int BaseScore { get; private set; }
    
    /// <summary>
    /// IDs das crops nos slots (para identidade de padrão - decay tracking).
    /// </summary>
    public List<CropID> CropIDs { get; private set; }
    
    /// <summary>
    /// Descrição opcional para debug (ex: "Row 0", "Column 2").
    /// </summary>
    public string DebugDescription { get; private set; }
    
    // ===== ONDA 4: Campos de Decay =====
    
    /// <summary>
    /// Número de dias consecutivos que este padrão está ativo.
    /// Usado para calcular decay: -10% por dia após o primeiro.
    /// 
    /// VALORES:
    /// - 0 = padrão novo (ainda não processado pelo tracking)
    /// - 1 = primeiro dia (sem decay)
    /// - 2+ = decay aplicado (0.9^(DaysActive-1))
    /// </summary>
    public int DaysActive { get; private set; }
    
    /// <summary>
    /// Se true, este padrão foi recriado após ser quebrado e tem +10% bonus.
    /// Válido apenas no primeiro dia após recriação.
    /// </summary>
    public bool HasRecreationBonus { get; private set; }
    
    // Factory method para criação consistente
    public static PatternMatch Create(
        string patternID,
        string displayName,
        List<int> slotIndices,
        int baseScore,
        List<CropID> cropIDs = null,
        string debugDescription = "")
    {
        return new PatternMatch
        {
            PatternID = patternID,
            DisplayName = displayName,
            SlotIndices = slotIndices ?? new List<int>(),
            BaseScore = baseScore,
            CropIDs = cropIDs ?? new List<CropID>(),
            DebugDescription = debugDescription,
            DaysActive = 0,  // Será preenchido pelo TrackingService
            HasRecreationBonus = false
        };
    }
    
    /// <summary>
    /// Define os dados de tracking (chamado pelo PatternTrackingService).
    /// </summary>
    public void SetTrackingData(int daysActive, bool hasRecreationBonus)
    {
        DaysActive = daysActive;
        HasRecreationBonus = hasRecreationBonus;
    }
    
    /// <summary>
    /// Retorna string formatada para debug/logs.
    /// </summary>
    public override string ToString()
    {
        string slots = string.Join(",", SlotIndices);
        string desc = string.IsNullOrEmpty(DebugDescription) ? "" : $" ({DebugDescription})";
        string decay = DaysActive > 1 ? $" [Dia {DaysActive}]" : "";
        string bonus = HasRecreationBonus ? " [+10% RECREATED]" : "";
        return $"{DisplayName}{desc} [slots: {slots}] = {BaseScore} pts{decay}{bonus}";
    }
}
