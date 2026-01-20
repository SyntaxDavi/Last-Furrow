using System.Collections.Generic;

/// <summary>
/// Padrão #10: Grid Perfeito
/// 
/// DESCRIÇÃO: Todos os 25 slots plantados COM diversidade (mínimo 4 tipos diferentes)
/// PONTOS BASE: 150 pts
/// TIER: 4 (Master)
/// DIFICULDADE: ?????
/// 
/// REGRAS:
/// - TODOS os 25 slots devem ter plantas vivas
/// - Mínimo 4 tipos de crops DIFERENTES
/// - Slots bloqueados INVALIDAM (todos devem estar desbloqueados)
/// - Nenhuma planta pode estar withered
/// 
/// FILOSOFIA:
/// - Mais comum do mid-game em diante
/// - Raro por escolha (diversidade), não por dificuldade técnica
/// - High-investment late-game strategy
/// - Game changer, mas não win condition automática
/// 
/// NOTA: O score é calculado com fórmula especial no PatternScoreCalculator
/// baseado no número de tipos únicos (diversityBonus).
/// 
/// EXEMPLO VÁLIDO:
/// [??][??][??][??][??]
/// [??][??][??][??][??]
/// [??][??][??][??][??]
/// [??][??][??][??][??]
/// [??][??][??][??][??]
/// = 4 tipos diferentes, 25 slots = Grid Perfeito!
/// </summary>
public class PerfectGridPattern : BaseGridPattern
{
    public PerfectGridPattern(PatternDefinitionSO definition) : base(definition) { }
    
    /// <summary>
    /// Número mínimo de tipos de crops diferentes para Grid Perfeito.
    /// </summary>
    private const int MIN_UNIQUE_CROPS = 4;
    
    public override List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int totalSlots = config.Rows * config.Columns;
        
        var allIndices = new List<int>();
        var uniqueCrops = new HashSet<CropID>();
        var cropIDs = new List<CropID>();
        
        // Verificar todos os slots
        for (int i = 0; i < totalSlots; i++)
        {
            // Verificar se slot está desbloqueado
            if (!gridService.IsSlotUnlocked(i))
                return matches; // Grid não está completo (slot bloqueado)
            
            // Verificar se slot é válido para padrão
            if (!PatternHelper.IsSlotValidForPattern(i, gridService))
                return matches; // Slot vazio ou withered
            
            CropID cropID = PatternHelper.GetCropID(i, gridService);
            
            allIndices.Add(i);
            cropIDs.Add(cropID);
            uniqueCrops.Add(cropID);
        }
        
        // Verificar se temos todos os 25 slots
        if (allIndices.Count != totalSlots)
            return matches;
        
        // Verificar diversidade mínima
        if (uniqueCrops.Count < MIN_UNIQUE_CROPS)
            return matches;
        
        // Criar match
        string desc = $"25 slots, {uniqueCrops.Count} tipos";
        matches.Add(CreateMatch(allIndices, cropIDs, desc));
        
        return matches;
    }
}
