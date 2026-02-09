using System.Collections.Generic;

/// <summary>
/// Padr�o #9: Arco-�ris (Rainbow Line)
/// 
/// DESCRI��O: Linha completa com crops DIFERENTES (diversidade)
/// PONTOS BASE: 55 pts
/// TIER: 3 (Dedicado)
/// DIFICULDADE: ????
/// 
/// REGRAS:
/// - Linha completa (5 slots) com TODAS as crops diferentes
/// - M�nimo 3 tipos diferentes para contar como arco-�ris
/// - 5 tipos diferentes = bonus m�ximo (f�rmula especial no Calculator)
/// - Funciona em linhas horizontais E verticais
/// - Slots bloqueados INVALIDAM a linha
/// - Todas as crops devem estar vivas (n�o withered)
/// 
/// EXEMPLO V�LIDO (5 tipos):
/// [??][??][??][??][??] = Arco-�ris perfeito!
/// 
/// EXEMPLO V�LIDO (3 tipos):
/// [??][??][??][??][??] = Arco-�ris b�sico (3 tipos: ??????)
/// 
/// EXEMPLO INV�LIDO (2 tipos):
/// [??][??][??][??][??] = N�O conta (s� 2 tipos)
/// 
/// NOTA: O score � calculado com f�rmula especial no PatternScoreCalculator
/// baseado no n�mero de tipos �nicos (diversityBonus).
/// </summary>
public class RainbowLinePattern : BaseGridPattern
{
    public RainbowLinePattern(PatternDefinitionSO definition) : base(definition) { }
    
    /// <summary>
    /// N�mero m�nimo de tipos de crops diferentes para contar como arco-�ris.
    /// </summary>
    private const int MIN_UNIQUE_CROPS = 5;
    
    public override List<PatternMatch> DetectAll(IGridService gridService)
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
        // Verificar se todos os slots s�o v�lidos
        if (!PatternHelper.AllSlotsValid(indices, gridService))
            return;
        
        // Contar tipos �nicos de crops
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
        string desc = $"{debugDesc} ({uniqueCrops.Count} tipos)";
        matches.Add(CreateMatch(indices, cropIDs, desc));
    }
}
