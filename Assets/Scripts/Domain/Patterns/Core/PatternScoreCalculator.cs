using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Autoridade ÚNICA de pontuação do Pattern System.
/// 
/// RESPONSABILIDADE:
/// - Toda matemática de pontuação do sistema
/// - Aplicar fórmula base (CropValue, Maturity)
/// - Calcular sinergia global (soft cap logarítmico)
/// - Lidar com casos especiais (futuro: Arco-íris, Grid Perfeito)
/// - Retornar score final
/// 
/// REGRA CRÍTICA:
/// NENHUM IGridPattern pode conter lógica matemática além do BaseScore (inteiro fixo).
/// 
/// FÓRMULA ONDA 1 (simplificada, sem decay):
/// finalScore = baseScore × cropMultiplier × maturityBonus
/// 
/// FÓRMULA COMPLETA (Onda 2+):
/// finalScore = baseScore × (avgCropValue / 5.0) × (1 + 0.5 × maturityRatio) × decayMultiplier
/// synergyTotal = sumScores × (1.0 + 0.2 × log2(patternCount))
/// </summary>
public class PatternScoreCalculator
{
    private readonly IGameLibrary _library;
    
    public PatternScoreCalculator(IGameLibrary library)
    {
        _library = library;
    }
    
    /// <summary>
    /// Calcula o score total de todos os padrões detectados.
    /// Aplica sinergia logarítmica se houver múltiplos padrões.
    /// </summary>
    /// <param name="matches">Lista de padrões detectados</param>
    /// <param name="gridService">Serviço de grid para consultar estado dos slots</param>
    /// <returns>Pontuação total de padrões</returns>
    public int CalculateTotal(List<PatternMatch> matches, IGridService gridService)
    {
        if (matches == null || matches.Count == 0)
        {
            return 0;
        }
        
        float totalScore = 0f;
        
        foreach (var match in matches)
        {
            int matchScore = CalculateSingle(match, gridService);
            totalScore += matchScore;
            
            Debug.Log($"[PatternScoreCalculator] {match.DisplayName}: {match.BaseScore} base ? {matchScore} final");
        }
        
        // Aplicar sinergia se houver múltiplos padrões
        float synergyMultiplier = CalculateSynergyMultiplier(matches.Count);
        float finalTotal = totalScore * synergyMultiplier;
        
        if (matches.Count > 1)
        {
            Debug.Log($"[PatternScoreCalculator] Sinergia ({matches.Count} padrões): {synergyMultiplier:F2}x");
        }
        
        int result = Mathf.RoundToInt(finalTotal);
        Debug.Log($"[PatternScoreCalculator] === TOTAL: {result} pontos de padrões ===");
        
        return result;
    }
    
    /// <summary>
    /// Calcula o score de um único padrão.
    /// 
    /// FÓRMULA PADRÃO:
    /// score = baseScore × cropMultiplier × maturityBonus
    /// 
    /// FÓRMULAS ESPECIAIS (Onda 3):
    /// - Rainbow: score × diversityBonus (mais tipos = mais pontos)
    /// - PerfectGrid: score × diversityBonus (mais tipos = mais pontos)
    /// 
    /// Onde:
    /// - cropMultiplier = avgCropValue / 10.0 (normalizado)
    /// - maturityBonus = 1 + 0.5 × (matureCrops / totalCrops)
    /// - diversityBonus = 1 + 0.2 × (uniqueTypes - minRequired)
    /// </summary>
    public int CalculateSingle(PatternMatch match, IGridService gridService)
    {
        float score = match.BaseScore;
        
        // Calcular multiplicadores baseados nos slots
        float cropMultiplier = CalculateCropMultiplier(match.SlotIndices, gridService);
        float maturityBonus = CalculateMaturityBonus(match.SlotIndices, gridService);
        
        score *= cropMultiplier;
        score *= maturityBonus;
        
        // NOVO Onda 3: Fórmulas especiais para padrões de diversidade
        if (match.PatternID == "RAINBOW_LINE" || match.PatternID == "PERFECT_GRID")
        {
            float diversityBonus = CalculateDiversityBonus(match);
            score *= diversityBonus;
            
            Debug.Log($"[PatternScoreCalculator] {match.DisplayName}: diversityBonus = {diversityBonus:F2}x");
        }
        
        return Mathf.RoundToInt(score);
    }
    
    /// <summary>
    /// Calcula bonus de diversidade para padrões Rainbow e PerfectGrid.
    /// 
    /// FÓRMULA:
    /// - Rainbow: bonus = 1 + 0.25 × (uniqueTypes - 3)
    ///   - 3 tipos = 1.0x (mínimo)
    ///   - 4 tipos = 1.25x
    ///   - 5 tipos = 1.5x (máximo)
    /// 
    /// - PerfectGrid: bonus = 1 + 0.15 × (uniqueTypes - 4)
    ///   - 4 tipos = 1.0x (mínimo)
    ///   - 5 tipos = 1.15x
    ///   - 6+ tipos = escala continua
    /// </summary>
    private float CalculateDiversityBonus(PatternMatch match)
    {
        if (match.CropIDs == null || match.CropIDs.Count == 0)
            return 1f;
        
        // Contar tipos únicos
        var uniqueCrops = new HashSet<CropID>(match.CropIDs);
        int uniqueCount = uniqueCrops.Count;
        
        if (match.PatternID == "RAINBOW_LINE")
        {
            // Rainbow: mínimo 3 tipos, +25% por tipo extra
            int extraTypes = Mathf.Max(0, uniqueCount - 3);
            return 1f + (0.25f * extraTypes);
        }
        else if (match.PatternID == "PERFECT_GRID")
        {
            // PerfectGrid: mínimo 4 tipos, +15% por tipo extra
            int extraTypes = Mathf.Max(0, uniqueCount - 4);
            return 1f + (0.15f * extraTypes);
        }
        
        return 1f;
    }
    
    /// <summary>
    /// Calcula multiplicador baseado no valor médio das crops.
    /// Crops mais valiosas dão mais pontos.
    /// 
    /// Formula: avgCropValue / 5.0 (onde 5 é o valor base de referência)
    /// </summary>
    private float CalculateCropMultiplier(List<int> slotIndices, IGridService gridService)
    {
        if (slotIndices == null || slotIndices.Count == 0) return 1f;
        
        float totalValue = 0f;
        int validCrops = 0;
        
        foreach (int index in slotIndices)
        {
            var slot = gridService.GetSlotReadOnly(index);
            if (!slot.IsEmpty && slot.CropID.IsValid)
            {
                if (_library.TryGetCrop(slot.CropID, out CropData cropData))
                {
                    // Usar BasePassiveScore como proxy de valor
                    totalValue += cropData.BasePassiveScore;
                    validCrops++;
                }
            }
        }
        
        if (validCrops == 0) return 1f;
        
        float avgValue = totalValue / validCrops;
        // Normalizar: 10 é o valor base típico (CropData.BasePassiveScore)
        float multiplier = avgValue / 10f;
        
        // Clamp para evitar valores extremos
        return Mathf.Clamp(multiplier, 0.5f, 3f);
    }
    
    /// <summary>
    /// Calcula bonus de maturidade.
    /// Plantas maduras dão +50% bonus (proporcional).
    /// 
    /// Formula: 1 + 0.5 × (matureCrops / totalCrops)
    /// </summary>
    private float CalculateMaturityBonus(List<int> slotIndices, IGridService gridService)
    {
        if (slotIndices == null || slotIndices.Count == 0) return 1f;
        
        int matureCount = 0;
        int totalCount = 0;
        
        foreach (int index in slotIndices)
        {
            var slot = gridService.GetSlotReadOnly(index);
            if (!slot.IsEmpty && slot.CropID.IsValid)
            {
                totalCount++;
                
                if (_library.TryGetCrop(slot.CropID, out CropData cropData))
                {
                    // Planta madura = CurrentGrowth >= DaysToMature
                    if (slot.CurrentGrowth >= cropData.DaysToMature)
                    {
                        matureCount++;
                    }
                }
            }
        }
        
        if (totalCount == 0) return 1f;
        
        float maturityRatio = (float)matureCount / totalCount;
        return 1f + (0.5f * maturityRatio);
    }
    
    /// <summary>
    /// Calcula multiplicador de sinergia baseado no número de padrões.
    /// Usa soft cap logarítmico para evitar explosão numérica.
    /// 
    /// Formula: 1.0 + 0.2 × log2(patternCount)
    /// 
    /// Exemplos:
    /// - 1 padrão = 1.0x (sem bônus)
    /// - 2 padrões = 1.2x (+20%)
    /// - 4 padrões = 1.4x (+40%)
    /// - 8 padrões = 1.6x (+60%)
    /// </summary>
    private float CalculateSynergyMultiplier(int patternCount)
    {
        if (patternCount <= 1) return 1f;
        
        // log2(n) = ln(n) / ln(2)
        float logValue = Mathf.Log(patternCount) / Mathf.Log(2);
        return 1f + (0.2f * logValue);
    }
}
