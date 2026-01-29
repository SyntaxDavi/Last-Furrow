    using UnityEngine;

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
    public Sprite WitheredSprite;

    private void OnValidate()
    {
        if (DaysToMature < 1) DaysToMature = 1;
        if (FreshnessWindow < 0) FreshnessWindow = 0;
    }

    /// <summary>
    /// Retorna o sprite baseado no estado completo.
    /// </summary>
    public Sprite GetSpriteForStage(int currentGrowth, int daysMature, bool isWithered)
    {
        if (isWithered) return WitheredSprite;

        // Fase de Maturação (inclui Janela de Frescor)
        if (currentGrowth >= DaysToMature)
        {
            // Opcional: Se quiser um sprite de "quase podre" no último dia
            // if (daysMature >= FreshnessWindow && FreshnessWindow > 0) return OverripeSprite;
            return MatureSprite;
        }

        // Fase de Crescimento
        if (GrowthStages == null || GrowthStages.Length == 0) return SeedSprite;
        int index = Mathf.Clamp(currentGrowth, 0, GrowthStages.Length - 1);
        return GrowthStages[index];
    }
}