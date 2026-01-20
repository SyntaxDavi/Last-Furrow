using System.Collections.Generic;

/// <summary>
/// Padrão #8: Moldura (Frame)
/// 
/// DESCRIÇÃO: Todas as bordas do grid com a mesma crop (16 slots em grid 5x5)
/// PONTOS BASE: 50 pts
/// TIER: 3 (Dedicado)
/// DIFICULDADE: ???
/// 
/// REGRAS:
/// - Mesma crop em TODOS os 16 slots de borda
/// - Grid 5x5: bordas = row 0, row 4, col 0, col 4 (sem duplicar cantos)
/// - Slots bloqueados INVALIDAM a moldura
/// - Todas as crops devem estar vivas (não withered)
/// 
/// FORMA (X = borda):
/// [X][X][X][X][X]
/// [X][ ][ ][ ][X]
/// [X][ ][ ][ ][X]
/// [X][ ][ ][ ][X]
/// [X][X][X][X][X]
/// 
/// Total: 5+5+3+3 = 16 slots (cantos contam uma vez)
/// </summary>
public class FramePattern : IGridPattern
{
    public string PatternID => "FRAME";
    public string DisplayName => "Moldura";
    public int BaseScore => 50;
    
    public List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Precisa de pelo menos 3x3 para ter moldura
        if (rows < 3 || cols < 3)
            return matches;
        
        var borderIndices = CollectBorderIndices(rows, cols);
        
        // Verificar se todos os slots da borda são válidos
        if (!PatternHelper.AllSlotsValid(borderIndices, gridService))
            return matches;
        
        // Verificar se todos têm a mesma crop
        if (!PatternHelper.AllSameCrop(borderIndices, gridService))
            return matches;
        
        // Criar match
        var cropIDs = PatternHelper.CollectCropIDs(borderIndices, gridService);
        
        matches.Add(PatternMatch.Create(
            PatternID,
            DisplayName,
            borderIndices,
            BaseScore,
            cropIDs,
            $"{borderIndices.Count} slots de borda"
        ));
        
        return matches;
    }
    
    /// <summary>
    /// Coleta todos os índices da borda do grid (sem duplicatas).
    /// Ordem: top row ? right column ? bottom row ? left column
    /// </summary>
    private List<int> CollectBorderIndices(int rows, int cols)
    {
        var indices = new List<int>();
        var added = new HashSet<int>();
        
        // Top row (row 0)
        for (int col = 0; col < cols; col++)
        {
            int index = PatternHelper.CoordToIndex(0, col, cols);
            if (added.Add(index))
                indices.Add(index);
        }
        
        // Right column (col = cols-1), excluindo canto já adicionado
        for (int row = 1; row < rows; row++)
        {
            int index = PatternHelper.CoordToIndex(row, cols - 1, cols);
            if (added.Add(index))
                indices.Add(index);
        }
        
        // Bottom row (row = rows-1), excluindo canto já adicionado
        for (int col = cols - 2; col >= 0; col--)
        {
            int index = PatternHelper.CoordToIndex(rows - 1, col, cols);
            if (added.Add(index))
                indices.Add(index);
        }
        
        // Left column (col = 0), excluindo cantos já adicionados
        for (int row = rows - 2; row > 0; row--)
        {
            int index = PatternHelper.CoordToIndex(row, 0, cols);
            if (added.Add(index))
                indices.Add(index);
        }
        
        return indices;
    }
}
