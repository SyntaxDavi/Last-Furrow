using System.Collections.Generic;

/// <summary>
/// Detecta moldura (frame): todas as 16 bordas do grid plantadas.
/// Tier 3: padrão avançado. Requer todas as bordas vivas.
/// Usa PatternDefinitionSO existente (FRAME).
/// 
/// Layout (grid 5x5):
///   [X][X][X][X][X]
///   [X]...........[ X]
///   [X]...........[ X]
///   [X]...........[ X]
///   [X][X][X][X][X]
/// </summary>
public class FrameDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int GRID_WIDTH = 5;
    private const int GRID_SIZE = 25;
    
    public FrameDetector(PatternDefinitionSO definition)
    {
        Definition = definition;
    }
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        // Só detectar no slot 0 (top-left) para evitar duplicatas
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
        
        // Top row (0-4)
        for (int i = 0; i < GRID_WIDTH; i++)
        {
            if (!AddSlotIfValid(gridService, i, slotIndices, cropIDs))
                return null;
        }
        
        // Bottom row (20-24)
        for (int i = 0; i < GRID_WIDTH; i++)
        {
            int idx = GRID_SIZE - GRID_WIDTH + i;
            if (!AddSlotIfValid(gridService, idx, slotIndices, cropIDs))
                return null;
        }
        
        // Left column (5, 10, 15) - middle rows only
        for (int i = 1; i < GRID_WIDTH - 1; i++)
        {
            int idx = i * GRID_WIDTH;
            if (!AddSlotIfValid(gridService, idx, slotIndices, cropIDs))
                return null;
        }
        
        // Right column (9, 14, 19) - middle rows only
        for (int i = 1; i < GRID_WIDTH - 1; i++)
        {
            int idx = (i * GRID_WIDTH) + (GRID_WIDTH - 1);
            if (!AddSlotIfValid(gridService, idx, slotIndices, cropIDs))
                return null;
        }
        
        return PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName,
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: "Full Frame"
        );
    }
    
    private bool AddSlotIfValid(IGridService gridService, int slotIndex, List<int> slotIndices, List<CropID> cropIDs)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        if (slotData.IsWithered) return false;
        
        slotIndices.Add(slotIndex);
        cropIDs.Add(slotData.CropID);
        return true;
    }
}
