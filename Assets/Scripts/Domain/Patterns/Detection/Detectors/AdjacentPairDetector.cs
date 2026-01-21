using System.Collections.Generic;

/// <summary>
/// Detecta pares de crops adjacentes horizontalmente.
/// Padrão mais simples: 2 crops lado a lado (NÃO withered).
/// Usa PatternDefinitionSO existente (ADJACENT_PAIR).
/// </summary>
public class AdjacentPairDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    public AdjacentPairDetector(PatternDefinitionSO definition)
    {
        Definition = definition;
    }
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        
        // CRÍTICO: Ignorar plantas withered!
        if (slotData.IsWithered) return false;
        
        return true;
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        // Verificar próximo slot horizontal
        int nextIndex = slotIndex + 1;
        
        if (nextIndex >= allSlotIndices.Length) return null;
        
        if (!gridService.IsSlotUnlocked(nextIndex)) return null;
        
        var nextSlotData = gridService.GetSlotReadOnly(nextIndex);
        if (nextSlotData == null || !nextSlotData.CropID.IsValid) return null;
        
        // CRÍTICO: Próximo slot também não pode estar withered!
        if (nextSlotData.IsWithered) return null;
        
        // Padrão encontrado!
        var slotIndices = new List<int> { slotIndex, nextIndex };
        var cropIDs = new List<CropID>
        {
            gridService.GetSlotReadOnly(slotIndex).CropID,
            nextSlotData.CropID
        };
        
        var match = PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName,
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Slots {slotIndex}-{nextIndex}"
        );
        
        match.SetTrackingData(daysActive: 1, hasRecreationBonus: false);
        return match;
    }
}

