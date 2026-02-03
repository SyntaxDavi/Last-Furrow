using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public enum CropValidationSeverity { Info, Warning, Error }

[System.Serializable]
public struct CropValidationIssue
{
    public string Message;
    public CropValidationSeverity Severity;
    public string PropertyName;
}

/// <summary>
/// Estados de vida da planta (domínio puro, sem visual).
/// Usado para separar regras de jogo de rendering.
/// </summary>
public enum PlantLifeStage
{
    Seed,            // Dia 0
    Growing,         // Entre plantio e maturação
    Mature,          // Amadureceu, dentro da janela de frescor
    NearlyOverripe,  // Último dia antes de passar
    Overripe,        // Passou da janela de frescor
    Withered         // Morte (prioridade máxima)
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

        // Validação de ID Único
        CheckForDuplicateID(issues);

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

    private void CheckForDuplicateID(List<CropValidationIssue> issues)
    {
        if (!ID.IsValid)
        {
            issues.Add(new CropValidationIssue 
            { 
                Message = "ID da planta é inválido ou vazio.", 
                Severity = CropValidationSeverity.Error, 
                PropertyName = nameof(ID) 
            });
            return;
        }

#if UNITY_EDITOR
        // Resources.FindObjectsOfTypeAll é pesado - apenas em Editor
        var allCrops = Resources.FindObjectsOfTypeAll<CropData>();
        var duplicates = allCrops.Where(c => c != this && c.ID == this.ID).ToList();
        
        if (duplicates.Count > 0)
        {
            var duplicateNames = string.Join(", ", duplicates.Select(c => c.Name));
            issues.Add(new CropValidationIssue 
            { 
                Message = $"ID '{ID.Value}' duplicado! Usado também em: {duplicateNames}", 
                Severity = CropValidationSeverity.Error, 
                PropertyName = nameof(ID) 
            });
        }
#endif
    }
    #endregion

    #region Life Stage Logic (Domain)
    /// <summary>
    /// Resolve o estado da planta baseado em regras de jogo.
    /// DOMÍNIO PURO: não sabe nada sobre Sprite, só decide estado.
    /// Isso separa gameplay de visual, reduzindo bugs de balanceamento.
    /// </summary>
    /// <param name="currentGrowth">Dias desde o plantio (idade total da planta).</param>
    /// <param name="daysMature">Dias desde que a planta atingiu maturidade (deve ser max(0, currentGrowth - DaysToMature)).</param>
    /// <param name="isWithered">Se a planta está morta/murcha.</param>
    /// <returns>Estado de vida da planta.</returns>
    public PlantLifeStage ResolveLifeStage(int currentGrowth, int daysMature, bool isWithered)
    {
        // Validação defensiva: não confia que outros sistemas sempre mandam valores limpos
        currentGrowth = Mathf.Max(0, currentGrowth);
        daysMature = Mathf.Max(0, daysMature);

        // 1. Withered tem prioridade máxima (morte sobrescreve tudo)
        if (isWithered) return PlantLifeStage.Withered;

        // 2. Antes de amadurecer
        if (currentGrowth < DaysToMature)
        {
            // Seed existe apenas no dia 0 (definição de design)
            return currentGrowth == 0 ? PlantLifeStage.Seed : PlantLifeStage.Growing;
        }

        // 3. Planta madura (currentGrowth >= DaysToMature)
        // Se FreshnessWindow == 0, planta fica Mature para sempre
        if (FreshnessWindow == 0) return PlantLifeStage.Mature;

        // 4. Passou da janela de frescor → Overripe
        if (daysMature >= FreshnessWindow) return PlantLifeStage.Overripe;

        // 5. Último dia antes de passar → NearlyOverripe
        if (daysMature == FreshnessWindow - 1) return PlantLifeStage.NearlyOverripe;

        // 6. Dentro da janela de frescor → Mature
        return PlantLifeStage.Mature;
    }
    #endregion

    #region Visual Mapping (Rendering)
    /// <summary>
    /// Mapeia estado da planta → sprite.
    /// SEM REGRAS DE JOGO: apenas escolhe sprite baseado no estado já resolvido.
    /// Para Growing, usa array de estágios indexado por currentGrowth.
    /// </summary>
    public Sprite GetSpriteForStage(PlantLifeStage stage, int currentGrowth)
    {
        switch (stage)
        {
            case PlantLifeStage.Seed:
                return SeedSprite;

            case PlantLifeStage.Growing:
                if (GrowthStages == null || GrowthStages.Length == 0) return SeedSprite;
                int index = Mathf.Clamp(currentGrowth - 1, 0, GrowthStages.Length - 1);
                return GrowthStages[index];

            case PlantLifeStage.Mature:
                return MatureSprite;

            case PlantLifeStage.NearlyOverripe:
                return NearlyOverripeSprite ?? MatureSprite; // Fallback para Mature

            case PlantLifeStage.Overripe:
                return OverripeSprite ?? MatureSprite; // Fallback para Mature

            case PlantLifeStage.Withered:
                return WitheredSprite;

            default:
                Debug.LogWarning($"[CropData] Estado desconhecido: {stage}. Usando SeedSprite.");
                return SeedSprite;
        }
    }
    #endregion
}
