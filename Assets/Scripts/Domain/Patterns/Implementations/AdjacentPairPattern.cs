using System.Collections.Generic;

/// <summary>
/// Padrão #1: Par Adjacente
/// 
/// DESCRIÇÃO: 2 crops iguais lado a lado (horizontal ou vertical)
/// PONTOS BASE: 5 pts
/// TIER: 1 (Iniciante)
/// DIFICULDADE: ?
/// 
/// REGRAS:
/// - Mesma crop em 2 slots adjacentes
/// - Adjacência = horizontal (esquerda/direita) OU vertical (cima/baixo)
/// - Diagonal NÃO conta
/// - Slots bloqueados quebram adjacência
/// </summary>
public class AdjacentPairPattern : IGridPattern
{
    public string PatternID => "ADJACENT_PAIR";
    public string DisplayName => "Par Adjacente";
    public int BaseScore => 5;
    
    public List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Usar HashSet para evitar detectar o mesmo par duas vezes
        var detectedPairs = new HashSet<string>();
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int currentIndex = PatternHelper.CoordToIndex(row, col, cols);
                
                if (!PatternHelper.IsSlotValidForPattern(currentIndex, gridService))
                    continue;
                
                // Verificar vizinho à direita (horizontal)
                if (col + 1 < cols)
                {
                    int rightIndex = PatternHelper.CoordToIndex(row, col + 1, cols);
                    TryAddPair(currentIndex, rightIndex, gridService, matches, detectedPairs, "H");
                }
                
                // Verificar vizinho abaixo (vertical)
                if (row + 1 < rows)
                {
                    int belowIndex = PatternHelper.CoordToIndex(row + 1, col, cols);
                    TryAddPair(currentIndex, belowIndex, gridService, matches, detectedPairs, "V");
                }
            }
        }
        
        return matches;
    }
    
    private void TryAddPair(int index1, int index2, IGridService gridService, 
        List<PatternMatch> matches, HashSet<string> detectedPairs, string direction)
    {
        // Verificar se o segundo slot é válido
        if (!PatternHelper.IsSlotValidForPattern(index2, gridService))
            return;
        
        // Verificar se têm a mesma crop
        if (!PatternHelper.HaveSameCrop(index1, index2, gridService))
            return;
        
        // Criar chave única para evitar duplicatas
        int minIndex = System.Math.Min(index1, index2);
        int maxIndex = System.Math.Max(index1, index2);
        string pairKey = $"{minIndex}-{maxIndex}";
        
        if (detectedPairs.Contains(pairKey))
            return;
        
        detectedPairs.Add(pairKey);
        
        // Criar match
        var slotIndices = new List<int> { index1, index2 };
        var cropIDs = PatternHelper.CollectCropIDs(slotIndices, gridService);
        
        string desc = direction == "H" ? "Horizontal" : "Vertical";
        
        matches.Add(PatternMatch.Create(
            PatternID,
            DisplayName,
            slotIndices,
            BaseScore,
            cropIDs,
            desc
        ));
    }
}
