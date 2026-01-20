using System.Collections.Generic;

/// <summary>
/// Padrão #6: Cruz Simples
/// 
/// DESCRIÇÃO: 5 crops iguais formando + (centro + 4 adjacentes)
/// PONTOS BASE: 30 pts
/// TIER: 2 (Casual)
/// DIFICULDADE: ??
/// 
/// REGRAS:
/// - Mesma crop no centro e nos 4 vizinhos cardeais (N/S/E/W)
/// - Centro NÃO pode estar na borda do grid (precisa ter 4 vizinhos)
/// - Slots bloqueados invalidam a cruz
/// 
/// FORMA:
///     [ ]
///   [ ][X][ ]
///     [ ]
/// 
/// CENTROS VÁLIDOS (grid 5x5):
/// - Qualquer slot que não esteja na borda (rows 1-3, cols 1-3)
/// - Total de 9 posições possíveis para o centro
/// 
/// ONDA 5: Migrado para BaseGridPattern + PatternDefinitionSO
/// </summary>
public class GridCrossPattern : BaseGridPattern
{
    public GridCrossPattern(PatternDefinitionSO definition) : base(definition) { }
    
    // Direções cardeais: Norte, Sul, Leste, Oeste
    private readonly int[,] _directions = new int[,]
    {
        { -1, 0 },  // Norte (row - 1)
        { 1, 0 },   // Sul (row + 1)
        { 0, 1 },   // Leste (col + 1)
        { 0, -1 }   // Oeste (col - 1)
    };
    
    
    public override List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Iterar apenas pelos slots que podem ser centro (não nas bordas)
        for (int row = 1; row < rows - 1; row++)
        {
            for (int col = 1; col < cols - 1; col++)
            {
                TryDetectCross(row, col, rows, cols, gridService, matches);
            }
        }
        
        return matches;
    }
    
    private void TryDetectCross(int centerRow, int centerCol, int rows, int cols,
        IGridService gridService, List<PatternMatch> matches)
    {
        int centerIndex = PatternHelper.CoordToIndex(centerRow, centerCol, cols);
        
        // Verificar se o centro é válido
        if (!PatternHelper.IsSlotValidForPattern(centerIndex, gridService))
            return;
        
        var indices = new List<int> { centerIndex };
        
        // Verificar os 4 vizinhos cardeais
        for (int i = 0; i < 4; i++)
        {
            int adjRow = centerRow + _directions[i, 0];
            int adjCol = centerCol + _directions[i, 1];
            
            // Verificar bounds (teoricamente sempre válido se não estamos na borda, mas por segurança)
            if (!PatternHelper.IsValidCoord(adjRow, adjCol, rows, cols))
                return;
            
            int adjIndex = PatternHelper.CoordToIndex(adjRow, adjCol, cols);
            
            // Verificar se o vizinho é válido
            if (!PatternHelper.IsSlotValidForPattern(adjIndex, gridService))
                return;
            
            indices.Add(adjIndex);
        }
        
        // Verificar se todos têm a mesma crop
        if (!PatternHelper.AllSameCrop(indices, gridService))
            return;
        
        // Criar match
        var cropIDs = PatternHelper.CollectCropIDs(indices, gridService);
        string desc = $"Center ({centerRow},{centerCol})";
        
        matches.Add(CreateMatch(indices, cropIDs, desc));
    }
}
