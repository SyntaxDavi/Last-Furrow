using System.Collections.Generic;

/// <summary>
/// Padrão #5: Xadrez 2x2
/// 
/// DESCRIÇÃO: 4 crops alternadas em quadrado 2x2 (padrão ABAB)
/// PONTOS BASE: 20 pts
/// TIER: 2 (Casual)
/// DIFICULDADE: ??
/// 
/// REGRAS:
/// - Quadrado 2x2 com crops ALTERNADAS
/// - Padrão: A B
///           B A
/// - Onde A e B são crops DIFERENTES
/// - Slots bloqueados invalidam o padrão
/// - Todas as 4 crops devem estar vivas (não withered)
/// 
/// EXEMPLO VÁLIDO:
/// [??][??]
/// [??][??]
/// 
/// EXEMPLO INVÁLIDO:
/// [??][??]  ? Não é alternado
/// [??][??]
/// </summary>
public class CheckerPattern : IGridPattern
{
    public string PatternID => "CHECKER_2X2";
    public string DisplayName => "Xadrez 2x2";
    public int BaseScore => 20;
    
    public List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // HashSet para evitar duplicatas
        var detectedCheckers = new HashSet<string>();
        
        // Iterar por todas as posições que podem ser top-left de um 2x2
        for (int row = 0; row < rows - 1; row++)
        {
            for (int col = 0; col < cols - 1; col++)
            {
                TryDetectChecker(row, col, cols, gridService, matches, detectedCheckers);
            }
        }
        
        return matches;
    }
    
    private void TryDetectChecker(int topRow, int leftCol, int cols,
        IGridService gridService, List<PatternMatch> matches, HashSet<string> detectedCheckers)
    {
        // Calcular os 4 índices do quadrado 2x2
        int topLeft = PatternHelper.CoordToIndex(topRow, leftCol, cols);
        int topRight = PatternHelper.CoordToIndex(topRow, leftCol + 1, cols);
        int bottomLeft = PatternHelper.CoordToIndex(topRow + 1, leftCol, cols);
        int bottomRight = PatternHelper.CoordToIndex(topRow + 1, leftCol + 1, cols);
        
        var indices = new List<int> { topLeft, topRight, bottomLeft, bottomRight };
        
        // Verificar se todos os slots são válidos (desbloqueados, com planta viva)
        if (!PatternHelper.AllSlotsValid(indices, gridService))
            return;
        
        // Obter as CropIDs
        CropID cropTopLeft = PatternHelper.GetCropID(topLeft, gridService);
        CropID cropTopRight = PatternHelper.GetCropID(topRight, gridService);
        CropID cropBottomLeft = PatternHelper.GetCropID(bottomLeft, gridService);
        CropID cropBottomRight = PatternHelper.GetCropID(bottomRight, gridService);
        
        // Verificar padrão ABAB (xadrez)
        // TopLeft == BottomRight (diagonal principal)
        // TopRight == BottomLeft (diagonal secundária)
        // TopLeft != TopRight (devem ser crops diferentes)
        
        bool diagonalPrincipalMatch = cropTopLeft == cropBottomRight;
        bool diagonalSecundariaMatch = cropTopRight == cropBottomLeft;
        bool isDifferentCrops = cropTopLeft != cropTopRight;
        
        if (!diagonalPrincipalMatch || !diagonalSecundariaMatch || !isDifferentCrops)
            return;
        
        // Criar chave única para evitar duplicatas
        string checkerKey = $"{topLeft}-{topRight}-{bottomLeft}-{bottomRight}";
        if (detectedCheckers.Contains(checkerKey))
            return;
        
        detectedCheckers.Add(checkerKey);
        
        // Criar match
        var cropIDs = PatternHelper.CollectCropIDs(indices, gridService);
        
        matches.Add(PatternMatch.Create(
            PatternID,
            DisplayName,
            indices,
            BaseScore,
            cropIDs,
            $"({topRow},{leftCol}) - {cropTopLeft.Value}/{cropTopRight.Value}"
        ));
    }
}
