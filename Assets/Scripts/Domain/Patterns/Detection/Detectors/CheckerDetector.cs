using System.Collections.Generic;

/// <summary>
/// Detecta padrão de xadrez 2x2 (ABAB pattern).
/// Tier 2: padrão intermediário. 4 crops em padrão alternado.
/// Usa PatternDefinitionSO existente (CHECKER).
/// 
/// Layout:
///   [A][B]
///   [B][A]
/// </summary>
public class CheckerDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int GRID_WIDTH = 5;
    
    public CheckerDetector(PatternDefinitionSO definition)
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
        
        int topLeft = slotIndex;
        int topRight = topLeft + 1;
        int bottomLeft = topLeft + GRID_WIDTH;
        int bottomRight = bottomLeft + 1;
        
        // Verificar limites
        if (topRight >= allSlotIndices.Length) return null;
        if (bottomRight >= allSlotIndices.Length) return null;
        
        // Verificar se não cruza borda
        int topLeftRow = topLeft / GRID_WIDTH;
        int topRightRow = topRight / GRID_WIDTH;
        if (topLeftRow != topRightRow) return null;
        
        // Verificar se todos os slots estão válidos e vivos
        if (!IsValidAndAlive(gridService, topLeft)) return null;
        if (!IsValidAndAlive(gridService, topRight)) return null;
        if (!IsValidAndAlive(gridService, bottomLeft)) return null;
        if (!IsValidAndAlive(gridService, bottomRight)) return null;
        
        // Pegar os CropIDs
        var tlCrop = gridService.GetSlotReadOnly(topLeft).CropID;
        var trCrop = gridService.GetSlotReadOnly(topRight).CropID;
        var blCrop = gridService.GetSlotReadOnly(bottomLeft).CropID;
        var brCrop = gridService.GetSlotReadOnly(bottomRight).CropID;
        
        // Verificar padrão xadrez: TL == BR && TR == BL && TL != TR
        if (tlCrop == brCrop && trCrop == blCrop && tlCrop != trCrop)
        {
            var slotIndices = new List<int> { topLeft, topRight, bottomLeft, bottomRight };
            var cropIDs = new List<CropID> { tlCrop, trCrop, blCrop, brCrop };
            
            return PatternMatch.Create(
                patternID: Definition.PatternID,
                displayName: Definition.DisplayName,
                slotIndices: slotIndices,
                baseScore: Definition.BaseScore,
                cropIDs: cropIDs,
                debugDescription: $"Checker at {topLeft}"
            );
        }
        
        return null;
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
