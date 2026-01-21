using System.Collections.Generic;

/// <summary>
/// Detecta padrão de cruz: centro + 4 adjacentes (cima, baixo, esquerda, direita).
/// Tier 3: padrão avançado. TODAS as plantas devem estar vivas (NÃO withered).
/// Usa PatternDefinitionSO existente (CROSS).
/// 
/// Layout (exemplo grid 5x5):
///   [1]
/// [3][0][4]
///   [2]
/// </summary>
public class CrossPatternDetector : IPatternDetector
{
    public PatternDefinitionSO Definition { get; private set; }
    
    private const int GRID_WIDTH = 5;
    
    public CrossPatternDetector(PatternDefinitionSO definition)
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
        
        int center = slotIndex;
        
        int top = center - GRID_WIDTH;
        int bottom = center + GRID_WIDTH;
        int left = center - 1;
        int right = center + 1;
        
        if (top < 0 || bottom >= allSlotIndices.Length) return null;
        if (left < 0 || right >= allSlotIndices.Length) return null;
        
        // Verificar se formam cruz válida (não cruzar bordas)
        int centerRow = center / GRID_WIDTH;
        int leftRow = left / GRID_WIDTH;
        int rightRow = right / GRID_WIDTH;
        
        if (leftRow != centerRow || rightRow != centerRow) return null;
        
        // Verificar se todos os slots têm crops VIVAS
        int[] crossIndices = { center, top, bottom, left, right };
        var slotIndices = new List<int>();
        var cropIDs = new List<CropID>();
        
        foreach (int idx in crossIndices)
        {
            if (!gridService.IsSlotUnlocked(idx)) return null;
            
            var slotData = gridService.GetSlotReadOnly(idx);
            if (slotData == null || !slotData.CropID.IsValid) return null;
            
            // CRÍTICO: Todos devem estar vivos!
            if (slotData.IsWithered) return null;
            
            slotIndices.Add(idx);
            cropIDs.Add(slotData.CropID);
        }
        
        // Cruz encontrada!
        var match = PatternMatch.Create(
            patternID: Definition.PatternID,
            displayName: Definition.DisplayName,
            slotIndices: slotIndices,
            baseScore: Definition.BaseScore,
            cropIDs: cropIDs,
            debugDescription: $"Centro: {center}"
        );
        
        match.SetTrackingData(daysActive: 1, hasRecreationBonus: false);
        return match;
    }
}

