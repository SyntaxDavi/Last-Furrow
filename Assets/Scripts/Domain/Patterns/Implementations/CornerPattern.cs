using System.Collections.Generic;

/// <summary>
/// Padrão #3: Cantinho
/// 
/// DESCRIÇÃO: 3 crops iguais formando L nos cantos do grid
/// PONTOS BASE: 8 pts
/// TIER: 1 (Iniciante)
/// DIFICULDADE: ?
/// 
/// REGRAS:
/// - Mesma crop em 3 slots formando L
/// - DEVE estar em um dos 4 cantos do grid
/// - Forma: canto + 2 adjacentes formando o L
/// 
/// CANTOS VÁLIDOS (grid 5x5):
/// - Top-Left (0,0): slots [0,1,5]
/// - Top-Right (0,4): slots [4,3,9]
/// - Bottom-Left (4,0): slots [20,21,15]
/// - Bottom-Right (4,4): slots [24,23,19]
/// </summary>
public class GridCornerPattern : IGridPattern
{
    public string PatternID => "CORNER";
    public string DisplayName => "Cantinho";
    public int BaseScore => 8;
    
    public List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Top-Left corner (0,0)
        TryDetectCorner(0, 0, new int[,] {{0,1}, {1,0}}, cols, gridService, matches, "Top-Left");
        
        // Top-Right corner (0, cols-1)
        TryDetectCorner(0, cols - 1, new int[,] {{0,-1}, {1,0}}, cols, gridService, matches, "Top-Right");
        
        // Bottom-Left corner (rows-1, 0)
        TryDetectCorner(rows - 1, 0, new int[,] {{0,1}, {-1,0}}, cols, gridService, matches, "Bottom-Left");
        
        // Bottom-Right corner (rows-1, cols-1)
        TryDetectCorner(rows - 1, cols - 1, new int[,] {{0,-1}, {-1,0}}, cols, gridService, matches, "Bottom-Right");
        
        return matches;
    }
    
    private void TryDetectCorner(int cornerRow, int cornerCol, int[,] offsets, int cols,
        IGridService gridService, List<PatternMatch> matches, string cornerName)
    {
        int cornerIndex = PatternHelper.CoordToIndex(cornerRow, cornerCol, cols);
        
        // Verificar se o canto é válido
        if (!PatternHelper.IsSlotValidForPattern(cornerIndex, gridService))
            return;
        
        // Calcular os dois slots adjacentes do L
        int adj1Row = cornerRow + offsets[0, 0];
        int adj1Col = cornerCol + offsets[0, 1];
        int adj1Index = PatternHelper.CoordToIndex(adj1Row, adj1Col, cols);
        
        int adj2Row = cornerRow + offsets[1, 0];
        int adj2Col = cornerCol + offsets[1, 1];
        int adj2Index = PatternHelper.CoordToIndex(adj2Row, adj2Col, cols);
        
        // Verificar se os adjacentes são válidos
        if (!PatternHelper.IsSlotValidForPattern(adj1Index, gridService))
            return;
        if (!PatternHelper.IsSlotValidForPattern(adj2Index, gridService))
            return;
        
        // Verificar se todos têm a mesma crop
        var indices = new List<int> { cornerIndex, adj1Index, adj2Index };
        if (!PatternHelper.AllSameCrop(indices, gridService))
            return;
        
        // Criar match
        var cropIDs = PatternHelper.CollectCropIDs(indices, gridService);
        
        matches.Add(PatternMatch.Create(
            PatternID,
            DisplayName,
            indices,
            BaseScore,
            cropIDs,
            cornerName
        ));
    }
}
