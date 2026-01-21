using System.Collections.Generic;

/// <summary>
/// Detecta linha vertical completa (5 crops em sequência vertical).
/// Tier 2: equivalente à linha horizontal.
/// Usa PatternDefinitionSO existente (FULL_LINE).
/// </summary>
public class VerticalLineDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int LINE_SIZE = 5;
    private const int GRID_WIDTH = 5;
    
    public VerticalLineDetector(PatternDefinitionSO definition)
    {
        Definition = definition;
    }
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        if (slotData.IsWithered) return false;
        
        // Só detectar se estamos no topo da linha
        return slotIndex < GRID_WIDTH;
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        // Verificar 5 slots consecutivos verticalmente
        for (int i = 0; i < LINE_SIZE; i++)
        {
            int checkIndex = slotIndex + (i * GRID_WIDTH);
            
            if (checkIndex >= allSlotIndices.Length) return null;
            
            if (!gridService.IsSlotUnlocked(checkIndex)) return null;
            
            var slotData = gridService.GetSlotReadOnly(checkIndex);
            if (slotData == null || !slotData.CropID.IsValid) return null;
            if (slotData.IsWithered) return null;
            
            slotIndices.Add(checkIndex);
            cropIDs.Add(slotData.CropID);
        }
        
        return PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName + " (V)",
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Vertical column {slotIndex % GRID_WIDTH}"
        );
    }
}
