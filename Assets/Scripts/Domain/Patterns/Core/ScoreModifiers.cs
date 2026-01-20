using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modificador de Crop Value - Crops mais valiosas dão mais pontos.
/// 
/// FÓRMULA: avgCropValue / 10.0 (normalizado)
/// Clamp: [0.5, 3.0] para evitar extremos
/// </summary>
public class CropValueModifier : IScoreModifier
{
    private readonly IGameLibrary _library;
    
    public string Name => "CropValue";
    
    public CropValueModifier(IGameLibrary library)
    {
        _library = library;
    }
    
    public bool IsApplicable(PatternMatch match)
    {
        return match.SlotIndices != null && match.SlotIndices.Count > 0;
    }
    
    public float Calculate(PatternMatch match, IGridService gridService)
    {
        if (!IsApplicable(match)) return 1f;
        
        float totalValue = 0f;
        int validCrops = 0;
        
        foreach (int index in match.SlotIndices)
        {
            var slot = gridService.GetSlotReadOnly(index);
            if (!slot.IsEmpty && slot.CropID.IsValid)
            {
                if (_library.TryGetCrop(slot.CropID, out CropData cropData))
                {
                    totalValue += cropData.BasePassiveScore;
                    validCrops++;
                }
            }
        }
        
        if (validCrops == 0) return 1f;
        
        float avgValue = totalValue / validCrops;
        float multiplier = avgValue / 10f;
        
        return Mathf.Clamp(multiplier, 0.5f, 3f);
    }
}

/// <summary>
/// Modificador de Maturidade - Plantas maduras dão +50% bonus (proporcional).
/// 
/// FÓRMULA: 1 + 0.5 × (matureCrops / totalCrops)
/// </summary>
public class MaturityModifier : IScoreModifier
{
    private readonly IGameLibrary _library;
    
    public string Name => "Maturity";
    
    public MaturityModifier(IGameLibrary library)
    {
        _library = library;
    }
    
    public bool IsApplicable(PatternMatch match)
    {
        return match.SlotIndices != null && match.SlotIndices.Count > 0;
    }
    
    public float Calculate(PatternMatch match, IGridService gridService)
    {
        if (!IsApplicable(match)) return 1f;
        
        int matureCount = 0;
        int totalCount = 0;
        
        foreach (int index in match.SlotIndices)
        {
            var slot = gridService.GetSlotReadOnly(index);
            if (!slot.IsEmpty && slot.CropID.IsValid)
            {
                totalCount++;
                
                if (_library.TryGetCrop(slot.CropID, out CropData cropData))
                {
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
}

/// <summary>
/// Modificador de Decay - Padrões perdem força com o tempo.
/// 
/// FÓRMULA: 0.9^(DaysActive - 1)
/// Dia 1: 1.0x | Dia 2: 0.9x | Dia 3: 0.81x | Dia 4: 0.729x
/// </summary>
public class DecayModifier : IScoreModifier
{
    public string Name => "Decay";
    
    public bool IsApplicable(PatternMatch match)
    {
        return match.DaysActive > 1;
    }
    
    public float Calculate(PatternMatch match, IGridService gridService)
    {
        if (!IsApplicable(match)) return 1f;
        
        return Mathf.Pow(0.9f, match.DaysActive - 1);
    }
}

/// <summary>
/// Modificador de Recreation Bonus - Padrões recriados ganham +10%.
/// </summary>
public class RecreationBonusModifier : IScoreModifier
{
    public string Name => "RecreationBonus";
    
    public bool IsApplicable(PatternMatch match)
    {
        return match.HasRecreationBonus;
    }
    
    public float Calculate(PatternMatch match, IGridService gridService)
    {
        return IsApplicable(match) ? 1.1f : 1f;
    }
}

/// <summary>
/// Modificador de Diversidade - Bonus para Rainbow e PerfectGrid.
/// 
/// FÓRMULA:
/// - Rainbow: 1 + 0.25 × (uniqueTypes - 3)
/// - PerfectGrid: 1 + 0.15 × (uniqueTypes - 4)
/// </summary>
public class DiversityModifier : IScoreModifier
{
    public string Name => "Diversity";
    
    public bool IsApplicable(PatternMatch match)
    {
        return PatternIDs.RequiresDiversityBonus(match.PatternID);
    }
    
    public float Calculate(PatternMatch match, IGridService gridService)
    {
        if (!IsApplicable(match)) return 1f;
        if (match.CropIDs == null || match.CropIDs.Count == 0) return 1f;
        
        var uniqueCrops = new HashSet<CropID>(match.CropIDs);
        int uniqueCount = uniqueCrops.Count;
        
        if (match.PatternID == PatternIDs.RAINBOW_LINE)
        {
            int extraTypes = Mathf.Max(0, uniqueCount - 3);
            return 1f + (0.25f * extraTypes);
        }
        else if (match.PatternID == PatternIDs.PERFECT_GRID)
        {
            int extraTypes = Mathf.Max(0, uniqueCount - 4);
            return 1f + (0.15f * extraTypes);
        }
        
        return 1f;
    }
}
