using System.Collections.Generic;

/// <summary>
/// Padrão #2: Trio em Linha
/// 
/// DESCRIÇÃO: 3 crops iguais em linha (horizontal ou vertical)
/// PONTOS BASE: 10 pts
/// TIER: 1 (Iniciante)
/// DIFICULDADE: ?
/// 
/// REGRAS:
/// - Mesma crop em 3 slots consecutivos
/// - Linha = horizontal OU vertical
/// - Slots bloqueados QUEBRAM a linha (não é trio se tiver locked no meio)
/// - Diagonal NÃO conta
/// </summary>
public class TrioLinePattern : BaseGridPattern
{
    public TrioLinePattern(PatternDefinitionSO definition) : base(definition) { }
    
    public override List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Usar HashSet para evitar duplicatas
        var detectedTrios = new HashSet<string>();
        
        // Verificar linhas horizontais
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col <= cols - 3; col++)
            {
                TryDetectTrio(row, col, 0, 1, cols, gridService, matches, detectedTrios, "Row");
            }
        }
        
        // Verificar linhas verticais
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row <= rows - 3; row++)
            {
                TryDetectTrio(row, col, 1, 0, cols, gridService, matches, detectedTrios, "Col");
            }
        }
        
        return matches;
    }
    
    private void TryDetectTrio(int startRow, int startCol, int rowDir, int colDir, int cols,
        IGridService gridService, List<PatternMatch> matches, HashSet<string> detectedTrios, string lineType)
    {
        var indices = new List<int>();
        
        // Coletar 3 slots consecutivos
        for (int i = 0; i < 3; i++)
        {
            int row = startRow + (i * rowDir);
            int col = startCol + (i * colDir);
            int index = PatternHelper.CoordToIndex(row, col, cols);
            indices.Add(index);
        }
        
        // Verificar se todos os slots são válidos
        if (!PatternHelper.AllSlotsValid(indices, gridService))
            return;
        
        // Verificar se todos têm a mesma crop
        if (!PatternHelper.AllSameCrop(indices, gridService))
            return;
        
        // Criar chave única para evitar duplicatas
        string trioKey = $"{indices[0]}-{indices[1]}-{indices[2]}";
        if (detectedTrios.Contains(trioKey))
            return;
        
        detectedTrios.Add(trioKey);
        
        // Criar match
        var cropIDs = PatternHelper.CollectCropIDs(indices, gridService);
        int lineIndex = lineType == "Row" ? startRow : startCol;
        
        matches.Add(PatternMatch.Create(
            PatternID,
            DisplayName,
            indices,
            BaseScore,
            cropIDs,
            $"{lineType} {lineIndex}"
        ));
    }
}
