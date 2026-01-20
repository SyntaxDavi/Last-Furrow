using System.Collections.Generic;

/// <summary>
/// Padrão #9: Arco-íris (Rainbow Line)
/// 
/// DESCRIÇÃO: Linha completa com crops DIFERENTES (diversidade)
/// PONTOS BASE: 55 pts
/// TIER: 3 (Dedicado)
/// DIFICULDADE: ????
/// 
/// REGRAS:
/// - Linha completa (5 slots) com TODAS as crops diferentes
/// - Mínimo 3 tipos diferentes para contar como arco-íris
/// - 5 tipos diferentes = bonus máximo (fórmula especial no Calculator)
/// - Funciona em linhas horizontais E verticais
/// - Slots bloqueados INVALIDAM a linha
/// - Todas as crops devem estar vivas (não withered)
/// 
/// EXEMPLO VÁLIDO (5 tipos):
/// [??][??][??][??][??] = Arco-íris perfeito!
/// 
/// EXEMPLO VÁLIDO (3 tipos):
/// [??][??][??][??][??] = Arco-íris básico (3 tipos: ??????)
/// 
/// EXEMPLO INVÁLIDO (2 tipos):
/// [??][??][??][??][??] = NÃO conta (só 2 tipos)
/// 
/// NOTA: O score é calculado com fórmula especial no PatternScoreCalculator
/// baseado no número de tipos únicos (diversityBonus).
/// </summary>
public class RainbowLinePattern : IGridPattern
{
    public string PatternID => "RAINBOW_LINE";
    public string DisplayName => "Arco-íris";
    public int BaseScore => 55;
    
    /// <summary>
    /// Número mínimo de tipos de crops diferentes para contar como arco-íris.
    /// </summary>
    private const int MIN_UNIQUE_CROPS = 3;
    
    public List<PatternMatch> DetectAll(IGridService gridService)
    {
        var matches = new List<PatternMatch>();
        var config = gridService.Config;
        int rows = config.Rows;
        int cols = config.Columns;
        
        // Verificar todas as linhas horizontais
        for (int row = 0; row < rows; row++)
        {
            TryDetectRainbowRow(row, cols, gridService, matches);
        }
        
        // Verificar todas as colunas verticais
        for (int col = 0; col < cols; col++)
        {
            TryDetectRainbowColumn(col, rows, cols, gridService, matches);
        }
        
        return matches;
    }
    
    private void TryDetectRainbowRow(int row, int cols, IGridService gridService, List<PatternMatch> matches)
    {
        var indices = new List<int>();
        
        // Coletar todos os slots da linha
        for (int col = 0; col < cols; col++)
        {
            int index = PatternHelper.CoordToIndex(row, col, cols);
            indices.Add(index);
        }
        
        TryCreateRainbowMatch(indices, gridService, matches, $"Row {row}");
    }
    
    private void TryDetectRainbowColumn(int col, int rows, int cols, IGridService gridService, List<PatternMatch> matches)
    {
        var indices = new List<int>();
        
        // Coletar todos os slots da coluna
        for (int row = 0; row < rows; row++)
        {
            int index = PatternHelper.CoordToIndex(row, col, cols);
            indices.Add(index);
        }
        
        TryCreateRainbowMatch(indices, gridService, matches, $"Col {col}");
    }
    
    private void TryCreateRainbowMatch(List<int> indices, IGridService gridService, 
        List<PatternMatch> matches, string debugDesc)
    {
        // Verificar se todos os slots são válidos
        if (!PatternHelper.AllSlotsValid(indices, gridService))
            return;
        
        // Contar tipos únicos de crops
        var uniqueCrops = new HashSet<CropID>();
        var cropIDs = new List<CropID>();
        
        foreach (int index in indices)
        {
            CropID cropID = PatternHelper.GetCropID(index, gridService);
            cropIDs.Add(cropID);
            uniqueCrops.Add(cropID);
        }
        
        // Precisa de pelo menos MIN_UNIQUE_CROPS tipos diferentes
        if (uniqueCrops.Count < MIN_UNIQUE_CROPS)
            return;
        
        // Criar match com metadata de diversidade
        matches.Add(PatternMatch.Create(
            PatternID,
            DisplayName,
            indices,
            BaseScore,
            cropIDs,
            $"{debugDesc} ({uniqueCrops.Count} tipos)"
        ));
    }
}
