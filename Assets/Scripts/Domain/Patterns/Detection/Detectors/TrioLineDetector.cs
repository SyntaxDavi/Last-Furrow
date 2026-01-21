using System.Collections.Generic;

/// <summary>
/// Detecta trio em linha (3 crops em sequência horizontal ou vertical).
/// Tier 1: padrão simples. Todas as plantas devem estar vivas (NÃO withered).
/// Usa PatternDefinitionSO existente (TRIO_LINE).
/// </summary>
public class TrioLineDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int TRIO_SIZE = 3;
    private const int GRID_WIDTH = 5; // Assumindo grid 5x5
    
    public TrioLineDetector(PatternDefinitionSO definition)
    {
        Definition = definition;
    }
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        
        if (slotData.IsWithered) return false;
        
        return true;
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        // Tentar horizontal primeiro
        var horizontalMatch = TryDetectHorizontal(gridService, slotIndex, allSlotIndices);
        if (horizontalMatch != null) return horizontalMatch;
        
        // Tentar vertical
        var verticalMatch = TryDetectVertical(gridService, slotIndex, allSlotIndices);
        return verticalMatch;
    }
    
    private PatternMatch TryDetectHorizontal(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        int row = slotIndex / GRID_WIDTH;
        
        for (int i = 0; i < TRIO_SIZE; i++)
        {
            int checkIndex = slotIndex + i;
            
            if (checkIndex >= allSlotIndices.Length) return null;
            
            int checkRow = checkIndex / GRID_WIDTH;
            if (checkRow != row) return null;
            
            if (!gridService.IsSlotUnlocked(checkIndex)) return null;
            
            var slotData = gridService.GetSlotReadOnly(checkIndex);
            if (slotData == null || !slotData.CropID.IsValid) return null;
            if (slotData.IsWithered) return null;
            
            slotIndices.Add(checkIndex);
            cropIDs.Add(slotData.CropID);
        }
        
        return PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName + " (H)",
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Horizontal {slotIndex}-{slotIndex + TRIO_SIZE - 1}"
        );
    }
    
    private PatternMatch TryDetectVertical(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        for (int i = 0; i < TRIO_SIZE; i++)
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
            debugDescription: $"Vertical {slotIndex}, {slotIndex + GRID_WIDTH}, {slotIndex + (GRID_WIDTH * 2)}"
        );
    }
}
