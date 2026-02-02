using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum CropValidationSeverity { Info, Warning, Error }

[System.Serializable]
public struct CropValidationIssue
{
    public string Message;
    public CropValidationSeverity Severity;
    public string PropertyName;
}

[CreateAssetMenu(fileName = "New Crop", menuName = "Last Furrow/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identidade")]
    public CropID ID; 
    public string Name;
    [TextArea] public string Description;

    [Header("Regras de Maturação")]
    [Min(1)] public int DaysToMature;       
    [Min(0)] public int FreshnessWindow;

    [Header("Sistema de Metas")]
    [Tooltip("Pontos gerados por noite enquanto a planta estiver viva.")]
    public int BasePassiveScore = 10;

    [Tooltip("Multiplicador se a planta estiver madura.")]
    public float MatureScoreMultiplier = 1.5f;

    [Header("Economia")]
    public int BaseSellValue;

    [Header("Visual")]
    public Sprite SeedSprite;
    public Sprite[] GrowthStages;
    public Sprite MatureSprite;
    public Sprite NearlyOverripeSprite;
    public Sprite OverripeSprite;
    public Sprite WitheredSprite;

    private void OnValidate()
    {
        if (DaysToMature < 1) DaysToMature = 1;
        if (FreshnessWindow < 0) FreshnessWindow = 0;
        
        // Regra de integridade: O array de estágios deve ter tamanho DaysToMature - 1
        int targetSize = Mathf.Max(0, DaysToMature - 1);
        if (GrowthStages == null || GrowthStages.Length != targetSize)
        {
            System.Array.Resize(ref GrowthStages, targetSize);
        }
    }

    #region Domain Validation
    public List<CropValidationIssue> GetValidationIssues()
    {
        var issues = new List<CropValidationIssue>();

        // Validação de Sprites Obrigatórios
        if (SeedSprite == null) issues.Add(new CropValidationIssue { Message = "Semente (SeedSprite) é obrigatória.", Severity = CropValidationSeverity.Error, PropertyName = nameof(SeedSprite) });
        if (MatureSprite == null) issues.Add(new CropValidationIssue { Message = "Planta Madura (MatureSprite) é obrigatória.", Severity = CropValidationSeverity.Error, PropertyName = nameof(MatureSprite) });
        if (WitheredSprite == null) issues.Add(new CropValidationIssue { Message = "Planta Morta (WitheredSprite) é obrigatória.", Severity = CropValidationSeverity.Error, PropertyName = nameof(WitheredSprite) });

        // Validação de Ciclo
        int expectedStages = Mathf.Max(0, DaysToMature - 1);
        if (GrowthStages == null || GrowthStages.Length != expectedStages)
        {
            issues.Add(new CropValidationIssue { Message = $"Array de estágios (GrowthStages) dessincronizado. Esperado: {expectedStages}.", Severity = CropValidationSeverity.Error, PropertyName = nameof(GrowthStages) });
        }
        else
        {
            for (int i = 0; i < GrowthStages.Length; i++)
            {
                if (GrowthStages[i] == null)
                    issues.Add(new CropValidationIssue { Message = $"Estágio de crescimento {i} está faltando sprite.", Severity = CropValidationSeverity.Error, PropertyName = nameof(GrowthStages) });
            }
        }

        // Avisos (Não críticos)
        if (NearlyOverripeSprite == null) issues.Add(new CropValidationIssue { Message = "Sprite 'Quase Passada' (NearlyOverripe) não configurado.", Severity = CropValidationSeverity.Warning, PropertyName = nameof(NearlyOverripeSprite) });
        if (OverripeSprite == null) issues.Add(new CropValidationIssue { Message = "Sprite 'Passada' (Overripe) não configurado. Usará sprite maduro.", Severity = CropValidationSeverity.Warning, PropertyName = nameof(OverripeSprite) });
        if (BaseSellValue <= 0) issues.Add(new CropValidationIssue { Message = "Valor de venda é zero ou negativo.", Severity = CropValidationSeverity.Warning, PropertyName = nameof(BaseSellValue) });

        return issues;
    }
    #endregion

    /// <summary>
    /// Retorna o sprite baseado no estado completo.
    /// </summary>
    public Sprite GetSpriteForStage(int currentGrowth, int daysMature, bool isWithered)
    {
        if (isWithered) return WitheredSprite;

        // Fase de Maturação (inclui Janela de Frescor)
        if (currentGrowth >= DaysToMature)
        {
            // Estado Final: Podre
            if (daysMature >= FreshnessWindow && FreshnessWindow > 0 && OverripeSprite != null) 
                return OverripeSprite;
            
            // Estado Intermediário: Quase Podre (1 dia antes do FreshnessWindow)
            if (daysMature == FreshnessWindow - 1 && FreshnessWindow > 0 && NearlyOverripeSprite != null)
                return NearlyOverripeSprite;
            
            return MatureSprite;
        }

        // Fase de Crescimento
        if (currentGrowth == 0) return SeedSprite;
        
        if (GrowthStages == null || GrowthStages.Length == 0) return SeedSprite;
        
        int index = Mathf.Clamp(currentGrowth - 1, 0, GrowthStages.Length - 1);
        return GrowthStages[index];
    }
}
