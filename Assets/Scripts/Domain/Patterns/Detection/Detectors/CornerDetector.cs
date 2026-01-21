using System.Collections.Generic;

/// <summary>
/// Detecta padrão de canto (L-shape) nos 4 cantos do grid.
/// Tier 1: padrão simples. 3 crops formando um "L".
/// Usa PatternDefinitionSO existente (CORNER).
/// 
/// Layouts possíveis:
/// Top-Left:     Top-Right:    Bottom-Left:  Bottom-Right:
///   [0][1]        [0][1]        [0]             [0]
///   [2]              [2]        [1][2]        [2][1]
/// </summary>
public class CornerDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int GRID_WIDTH = 5;
    private const int GRID_SIZE = 25;
    
    public CornerDetector(PatternDefinitionSO definition)
    {
        Definition = definition;
    }
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        if (slotData.IsWithered) return false;
        
        // Só pode ser canto se está nas posições corretas
        int topLeft = 0;
        int topRight = GRID_WIDTH - 1;
        int bottomLeft = GRID_SIZE - GRID_WIDTH;
        int bottomRight = GRID_SIZE - 1;
        
        return slotIndex == topLeft || slotIndex == topRight || 
               slotIndex == bottomLeft || slotIndex == bottomRight;
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        int topLeft = 0;
        int topRight = GRID_WIDTH - 1;
        int bottomLeft = GRID_SIZE - GRID_WIDTH;
        int bottomRight = GRID_SIZE - 1;
        
        if (slotIndex == topLeft)
            return TryDetectCorner(gridService, slotIndex, slotIndex + 1, slotIndex + GRID_WIDTH, "Top-Left");
        
        if (slotIndex == topRight)
            return TryDetectCorner(gridService, slotIndex, slotIndex - 1, slotIndex + GRID_WIDTH, "Top-Right");
        
        if (slotIndex == bottomLeft)
            return TryDetectCorner(gridService, slotIndex, slotIndex + 1, slotIndex - GRID_WIDTH, "Bottom-Left");
        
        if (slotIndex == bottomRight)
            return TryDetectCorner(gridService, slotIndex, slotIndex - 1, slotIndex - GRID_WIDTH, "Bottom-Right");
        
        return null;
    }
    
    private PatternMatch TryDetectCorner(IGridService gridService, int corner, int side1, int side2, string cornerName)
    {
        if (!IsValidAndAlive(gridService, corner)) return null;
        if (!IsValidAndAlive(gridService, side1)) return null;
        if (!IsValidAndAlive(gridService, side2)) return null;
        
        var slotIndices = new List<int> { corner, side1, side2 };
        var cropIDs = new List<CropID>
        {
            gridService.GetSlotReadOnly(corner).CropID,
            gridService.GetSlotReadOnly(side1).CropID,
            gridService.GetSlotReadOnly(side2).CropID
        };
        
        return PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName,
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"{cornerName} Corner"
        );
    }
    
    private bool IsValidAndAlive(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        if (slotData.IsWithered) return false;
        
        return true;
    }
}
