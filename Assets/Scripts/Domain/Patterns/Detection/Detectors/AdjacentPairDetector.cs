using System.Collections.Generic;

/// <summary>
/// Detecta pares de crops adjacentes horizontalmente.
/// Padrão mais simples: 2 crops lado a lado.
/// </summary>
public class AdjacentPairDetector : IPatternDetector
{
    public string PatternID => "ADJACENT_PAIR";
    public string DisplayName => "Par Adjacente";
    public int BaseScore => 5;
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        return slotData != null && slotData.CropID.IsValid;
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        // Verificar próximo slot horizontal
        int nextIndex = slotIndex + 1;
        
        // Validar se próximo existe
        if (nextIndex >= allSlotIndices.Length) return null;
        
        // Verificar se próximo tem crop
        if (!gridService.IsSlotUnlocked(nextIndex)) return null;
        
        var nextSlotData = gridService.GetSlotReadOnly(nextIndex);
        if (nextSlotData == null || !nextSlotData.CropID.IsValid) return null;
        
        // Padrão encontrado!
        var slotIndices = new List<int> { slotIndex, nextIndex };
        var cropIDs = new List<CropID>
        {
            gridService.GetSlotReadOnly(slotIndex).CropID,
            nextSlotData.CropID
        };
        
        var match = PatternMatch.Create(
            patternID: PatternID,
            displayName: DisplayName,
            slotIndices: slotIndices,
            baseScore: BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Slots {slotIndex}-{nextIndex}"
        );
        
        match.SetTrackingData(daysActive: 1, hasRecreationBonus: false);
        return match;
    }
}
