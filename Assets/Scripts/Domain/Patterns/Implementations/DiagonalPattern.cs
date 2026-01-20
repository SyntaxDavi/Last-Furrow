using System.Collections.Generic;

/// <summary>
/// Padrão #7: Diagonal
/// 
/// DESCRIÇÃO: 5 crops iguais em diagonal completa (\ ou /)
/// PONTOS BASE: 40 pts
/// TIER: 3 (Dedicado)
/// DIFICULDADE: ???
/// 
/// REGRAS:
/// - Mesma crop em TODOS os 5 slots de uma diagonal
/// - Grid 5x5 tem exatamente 2 diagonais completas:
///   - Principal (\): slots [0,6,12,18,24]
///   - Secundária (/): slots [4,8,12,16,20]
/// - Slots bloqueados QUEBRAM a diagonal
/// - Todas as crops devem estar vivas (não withered)
/// 
/// FORMAS:
/// Diagonal Principal (\):     Diagonal Secundária (/):
/// [X][ ][ ][ ][ ]            [ ][ ][ ][ ][X]
/// [ ][X][ ][ ][ ]            [ ][ ][ ][X][ ]
/// [ ][ ][X][ ][ ]            [ ][ ][X][ ][ ]
/// [ ][ ][ ][X][ ]            [ ][X][ ][ ][ ]
/// [ ][ ][ ][ ][X]            [X][ ][ ][ ][ ]
/// </summary>
public class DiagonalPattern : BaseGridPattern
{
    public DiagonalPattern(PatternDefinitionSO definition) : base(definition) { }
    
    public override List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Só funciona em grids quadrados (5x5)
        if (rows != cols || rows < 5)
            return matches;
        
        // Diagonal Principal (\) - top-left para bottom-right
        TryDetectDiagonal(0, 0, 1, 1, rows, cols, gridService, matches, "Principal (\\)");
        
        // Diagonal Secundária (/) - top-right para bottom-left
        TryDetectDiagonal(0, cols - 1, 1, -1, rows, cols, gridService, matches, "Secundária (/)");
        
        return matches;
    }
    
    private void TryDetectDiagonal(int startRow, int startCol, int rowDir, int colDir, 
        int rows, int cols, IGridService gridService, List<PatternMatch> matches, string diagName)
    {
        var indices = new List<int>();
        
        // Coletar todos os slots da diagonal
        int row = startRow;
        int col = startCol;
        
        while (PatternHelper.IsValidCoord(row, col, rows, cols))
        {
            int index = PatternHelper.CoordToIndex(row, col, cols);
            indices.Add(index);
            
            row += rowDir;
            col += colDir;
        }
        
        // Verificar se temos 5 slots (diagonal completa em grid 5x5)
        if (indices.Count != 5)
            return;
        
        // Verificar se todos os slots são válidos
        if (!PatternHelper.AllSlotsValid(indices, gridService))
            return;
        
        // Verificar se todos têm a mesma crop
        if (!PatternHelper.AllSameCrop(indices, gridService))
            return;
        
        
        // Criar match
        var cropIDs = PatternHelper.CollectCropIDs(indices, gridService);
        
        matches.Add(CreateMatch(indices, cropIDs, diagName));
    }
}
