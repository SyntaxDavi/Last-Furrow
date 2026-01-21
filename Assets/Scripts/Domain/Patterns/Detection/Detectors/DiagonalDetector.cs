using System.Collections.Generic;

/// <summary>
/// Detecta diagonais completas (5 crops em diagonal).
/// Tier 3: padrão avançado. Detecta ambas diagonais (\\ e /).
/// Usa PatternDefinitionSO existente (DIAGONAL).
/// </summary>
public class DiagonalDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int GRID_WIDTH = 5;
    private const int GRID_SIZE = 25;
    
    public DiagonalDetector(PatternDefinitionSO definition)
    {
        Definition = definition;
    }
    
    public bool CanDetectAt(IGridService gridService, int slotIndex)
    {
        if (!gridService.IsSlotUnlocked(slotIndex)) return false;
        
        var slotData = gridService.GetSlotReadOnly(slotIndex);
        if (slotData == null || !slotData.CropID.IsValid) return false;
        if (slotData.IsWithered) return false;
        
        // Só detectar se estamos no início de uma diagonal
        return slotIndex == 0 || slotIndex == (GRID_WIDTH - 1);
    }
    
    public PatternMatch DetectAt(IGridService gridService, int slotIndex, int[] allSlotIndices)
    {
        if (!CanDetectAt(gridService, slotIndex)) return null;
        
        // Tentar diagonal principal (\\ - de top-left para bottom-right)
        if (slotIndex == 0)
        {
            var mainDiag = TryDetectMainDiagonal(gridService, allSlotIndices);
            if (mainDiag != null) return mainDiag;
        }
        
        // Tentar diagonal secundária (/ - de top-right para bottom-left)
        if (slotIndex == (GRID_WIDTH - 1))
        {
            var antiDiag = TryDetectAntiDiagonal(gridService, allSlotIndices);
            if (antiDiag != null) return antiDiag;
        }
        
        return null;
    }
    
    private PatternMatch TryDetectMainDiagonal(IGridService gridService, int[] allSlotIndices)
    {
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        // Diagonal principal: 0, 6, 12, 18, 24 (step = GRID_WIDTH + 1)
        for (int i = 0; i < GRID_WIDTH; i++)
        {
            int checkIndex = i * (GRID_WIDTH + 1);
            
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
            displayName: Definition.DisplayName + " (\\)",
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: "Main Diagonal (\\)"
        );
    }
    
    private PatternMatch TryDetectAntiDiagonal(IGridService gridService, int[] allSlotIndices)
    {
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        // Diagonal secundária: 4, 8, 12, 16, 20 (step = GRID_WIDTH - 1)
        for (int i = 0; i < GRID_WIDTH; i++)
        {
            int checkIndex = (GRID_WIDTH - 1) + (i * (GRID_WIDTH - 1));
            
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
            displayName: Definition.DisplayName + " (/)",
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: "Anti-Diagonal (/)"
        );
    }
}
