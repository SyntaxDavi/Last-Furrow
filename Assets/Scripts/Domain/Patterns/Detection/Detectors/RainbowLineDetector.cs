using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Detecta linha arco-íris: linha com 3-5 tipos DIFERENTES de crops.
/// Tier 3: padrão avançado. Requer diversidade de crops.
/// Usa PatternDefinitionSO existente (RAINBOW_LINE).
/// </summary>
public class RainbowLineDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int LINE_SIZE = 5;
    private const int GRID_WIDTH = 5;
    private const int MIN_DIFFERENT_CROPS = 3;
    
    public RainbowLineDetector(PatternDefinitionSO definition)
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
        var horizontalMatch = TryDetectRainbowHorizontal(gridService, slotIndex, allSlotIndices);
        if (horizontalMatch != null) return horizontalMatch;
        
        // Tentar vertical se estamos na primeira linha
        if (slotIndex < GRID_WIDTH)
        {
            var verticalMatch = TryDetectRainbowVertical(gridService, slotIndex, allSlotIndices);
            if (verticalMatch != null) return verticalMatch;
        }
        
        return null;
    }
    
    private PatternMatch TryDetectRainbowHorizontal(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        int row = slotIndex / GRID_WIDTH;
        
        for (int i = 0; i < LINE_SIZE; i++)
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
        
        // Verificar se temos pelo menos MIN_DIFFERENT_CROPS tipos diferentes
        var uniqueCrops = cropIDs.Distinct().Count();
        if (uniqueCrops < MIN_DIFFERENT_CROPS) return null;
        
        return PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName + " (H)",
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Rainbow Horizontal {uniqueCrops} types"
        );
    }
    
    private PatternMatch TryDetectRainbowVertical(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
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
        
        var uniqueCrops = cropIDs.Distinct().Count();
        if (uniqueCrops < MIN_DIFFERENT_CROPS) return null;
        
        return PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName + " (V)",
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Rainbow Vertical {uniqueCrops} types"
        );
    }
}
