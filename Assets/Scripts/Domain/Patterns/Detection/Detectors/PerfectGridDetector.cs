using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Detecta grid perfeito: todos os 25 slots plantados com mínimo 4 tipos diferentes.
/// Tier 4: padrão master. Requer grid completo e diversidade máxima.
/// Usa PatternDefinitionSO existente (PERFECT_GRID).
/// </summary>
public class PerfectGridDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int GRID_SIZE = 25;
    private const int MIN_DIFFERENT_CROPS = 4;
    
    public PerfectGridDetector(PatternDefinitionSO definition)
    {
        Definition = definition;
    }
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        // Só detectar no slot 0 para evitar duplicatas
        if (slotIndex != 0) return false;
        
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        if (slotData.IsWithered) return false;
        
        return true;
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        // Verificar todos os 25 slots
        for (int i = 0; i < GRID_SIZE; i++)
        {
            if (!gridService.IsSlotUnlocked(i)) return null;
            
            var slotData = gridService.GetSlotReadOnly(i);
            if (slotData == null || !slotData.CropID.IsValid) return null;
            if (slotData.IsWithered) return null;
            
            slotIndices.Add(i);
            cropIDs.Add(slotData.CropID);
        }
        
        // Verificar diversidade mínima
        var uniqueCrops = cropIDs.Distinct().Count();
        if (uniqueCrops < MIN_DIFFERENT_CROPS) return null;
        
        return PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName,
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Perfect Grid ({uniqueCrops} different crops)"
        );
    }
}
