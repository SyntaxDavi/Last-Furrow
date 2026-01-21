using System.Collections.Generic;

/// <summary>
/// Detecta linha horizontal completa (3 crops em sequência).
/// Tier 2: mais valioso que par adjacente.
/// </summary>
public class HorizontalLineDetector : IPatternDetector
{
    public string PatternID => "HORIZONTAL_LINE";
    public string DisplayName => "Linha Horizontal";
    public int BaseScore => 15;
    
    private const int LINE_SIZE = 3;
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        return slotData != null && slotData.CropID.IsValid;
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        // Verificar se temos 3 slots consecutivos com crops
        for (int i = 0; i < LINE_SIZE; i++)
        {
            int checkIndex = slotIndex + i;
            
            // Validar índice
            if (checkIndex >= allSlotIndices.Length) return null;
            
            // Verificar se slot está desbloqueado e tem crop
            if (!gridService.IsSlotUnlocked(checkIndex)) return null;
            
            var slotData = gridService.GetSlotReadOnly(checkIndex);
            if (slotData == null || !slotData.CropID.IsValid) return null;
            
            slotIndices.Add(checkIndex);
            cropIDs.Add(slotData.CropID);
        }
        
        // Linha completa encontrada!
        var match = PatternMatch.Create(
            patternID: PatternID,
            displayName: DisplayName,
            slotIndices: slotIndices,
            baseScore: BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Slots {slotIndex}-{slotIndex + LINE_SIZE - 1}"
        );
        
        match.SetTrackingData(daysActive: 1, hasRecreationBonus: false);
        return match;
    }
}
