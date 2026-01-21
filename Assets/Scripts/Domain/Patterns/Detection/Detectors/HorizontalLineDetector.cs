using System.Collections.Generic;

/// <summary>
/// Detecta linha horizontal completa (3 crops em sequência, NÃO withered).
/// Tier 2: mais valioso que par adjacente.
/// Usa PatternDefinitionSO existente (FULL_LINE).
/// </summary>
public class HorizontalLineDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int LINE_SIZE = 3;
    
    public HorizontalLineDetector(PatternDefinitionSO definition)
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
        
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        // Verificar se temos 3 slots consecutivos com crops VIVAS
        for (int i = 0; i < LINE_SIZE; i++)
        {
            int checkIndex = slotIndex + i;
            
            if (checkIndex >= allSlotIndices.Length) return null;
            
            if (!gridService.IsSlotUnlocked(checkIndex)) return null;
            
            var slotData = gridService.GetSlotReadOnly(checkIndex);
            if (slotData == null || !slotData.CropID.IsValid) return null;
            
            // CRÍTICO: Todos os slots devem estar vivos!
            if (slotData.IsWithered) return null;
            
            slotIndices.Add(checkIndex);
            cropIDs.Add(slotData.CropID);
        }
        
        // Linha completa encontrada!
        var match = PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName,
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Slots {slotIndex}-{slotIndex + LINE_SIZE - 1}"
        );
        
        match.SetTrackingData(daysActive: 1, hasRecreationBonus: false);
        return match;
    }
}

