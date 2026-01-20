using System.Collections.Generic;

/// <summary>
/// Padrão #4: Linha Completa
/// 
/// DESCRIÇÃO: 5 crops iguais em linha inteira (horizontal ou vertical)
/// PONTOS BASE: 25 pts
/// TIER: 2 (Casual)
/// DIFICULDADE: ??
/// 
/// REGRAS:
/// - Mesma crop em TODOS os slots de uma linha/coluna
/// - Grid 5x5 = 5 slots consecutivos
/// - Slots bloqueados QUEBRAM a linha (não conta como completa)
/// - NÃO detecta se houver qualquer slot inválido
/// </summary>
public class FullLinePattern : BaseGridPattern
{
    public FullLinePattern(PatternDefinitionSO definition) : base(definition) { }
    
    public override List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Verificar todas as linhas horizontais
        for (int row = 0; row < rows; row++)
        {
            TryDetectFullRow(row, cols, gridService, matches);
        }
        
        // Verificar todas as colunas verticais
        for (int col = 0; col < cols; col++)
        {
            TryDetectFullColumn(col, rows, cols, gridService, matches);
        }
        
        return matches;
    }
    
    private void TryDetectFullRow(int row, int cols, IGridService gridService, List<PatternMatch> matches)
    {
        var indices = new List<int>();
        
        // Coletar todos os slots da linha
        for (int col = 0; col < cols; col++)
        {
            int index = PatternHelper.CoordToIndex(row, col, cols);
            indices.Add(index);
        }
        
        // Verificar se todos os slots são válidos
        if (!PatternHelper.AllSlotsValid(indices, gridService))
            return;
        
        // Verificar se todos têm a mesma crop
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
            $"Row {row}"
        ));
    }
    
    private void TryDetectFullColumn(int col, int rows, int cols, IGridService gridService, List<PatternMatch> matches)
    {
        var indices = new List<int>();
        
        // Coletar todos os slots da coluna
        for (int row = 0; row < rows; row++)
        {
            int index = PatternHelper.CoordToIndex(row, col, cols);
            indices.Add(index);
        }
        
        // Verificar se todos os slots são válidos
        if (!PatternHelper.AllSlotsValid(indices, gridService))
            return;
        
        // Verificar se todos têm a mesma crop
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
            $"Column {col}"
        ));
    }
}
