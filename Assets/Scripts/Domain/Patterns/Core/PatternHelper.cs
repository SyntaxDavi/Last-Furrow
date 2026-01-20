using System.Collections.Generic;

/// <summary>
/// Utilitários compartilhados para detecção de padrões.
/// 
/// Fornece helpers para:
/// - Navegação 2D no grid
/// - Validação de slots (locked, withered, empty)
/// - Verificação de crops iguais
/// </summary>
public static class PatternHelper
{
    /// <summary>
    /// Converte índice linear para coordenadas 2D (row, col).
    /// </summary>
    public static (int row, int col) IndexToCoord(int index, int columns)
    {
        return (index / columns, index % columns);
    }
    
    /// <summary>
    /// Converte coordenadas 2D para índice linear.
    /// </summary>
    public static int CoordToIndex(int row, int col, int columns)
    {
        return row * columns + col;
    }
    
    /// <summary>
    /// Verifica se coordenadas estão dentro do grid.
    /// </summary>
    public static bool IsValidCoord(int row, int col, int rows, int columns)
    {
        return row >= 0 && row < rows && col >= 0 && col < columns;
    }
    
    /// <summary>
    /// Verifica se um slot é válido para formar padrões.
    /// Um slot é válido se:
    /// - Está desbloqueado (unlocked)
    /// - Tem uma planta viva (não vazia, não morta)
    /// </summary>
    public static bool IsSlotValidForPattern(int index, IGridService gridService)
    {
        // Verificar se está desbloqueado
        if (!gridService.IsSlotUnlocked(index)) return false;
        
        var slot = gridService.GetSlotReadOnly(index);
        
        // Verificar se tem planta viva
        if (slot.IsEmpty) return false;
        if (slot.IsWithered) return false;
        if (!slot.CropID.IsValid) return false;
        
        return true;
    }
    
    /// <summary>
    /// Verifica se dois slots têm a mesma crop (para padrões de mesma crop).
    /// </summary>
    public static bool HaveSameCrop(int index1, int index2, IGridService gridService)
    {
        var slot1 = gridService.GetSlotReadOnly(index1);
        var slot2 = gridService.GetSlotReadOnly(index2);
        
        return slot1.CropID == slot2.CropID;
    }
    
    /// <summary>
    /// Obtém a CropID de um slot.
    /// </summary>
    public static CropID GetCropID(int index, IGridService gridService)
    {
        return gridService.GetSlotReadOnly(index).CropID;
    }
    
    /// <summary>
    /// Coleta os CropIDs de uma lista de slots.
    /// </summary>
    public static List<CropID> CollectCropIDs(List<int> indices, IGridService gridService)
    {
        var cropIDs = new List<CropID>();
        foreach (int index in indices)
        {
            cropIDs.Add(GetCropID(index, gridService));
        }
        return cropIDs;
    }
    
    /// <summary>
    /// Verifica se todos os slots em uma lista têm a mesma crop.
    /// </summary>
    public static bool AllSameCrop(List<int> indices, IGridService gridService)
    {
        if (indices == null || indices.Count < 2) return true;
        
        CropID firstCrop = GetCropID(indices[0], gridService);
        
        for (int i = 1; i < indices.Count; i++)
        {
            if (GetCropID(indices[i], gridService) != firstCrop)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Verifica se todos os slots em uma lista são válidos para padrões.
    /// </summary>
    public static bool AllSlotsValid(List<int> indices, IGridService gridService)
    {
        foreach (int index in indices)
        {
            if (!IsSlotValidForPattern(index, gridService))
            {
                return false;
            }
        }
        return true;
    }
}
