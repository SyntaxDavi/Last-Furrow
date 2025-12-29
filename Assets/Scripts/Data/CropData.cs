using UnityEngine;

[CreateAssetMenu(fileName = "New Crop", menuName = "Last Furrow/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identidade")]
    public string ID; 
    public string Name;
    [TextArea] public string Description;

    [Header("Regras de Cultivo")]
    public int DaysToMature; 
    public int BaseSellValue;

    public bool HarvestableOnlyOnDeadLine;

    [Header("Visual")]
    public Sprite SeedSprite;
    public Sprite[] GrowthStages; 
    public Sprite MatureSprite;
    public Sprite WitheredSprite; 

    // Método auxiliar para não dar erro se não tiver sprite suficiente
    public Sprite GetSpriteForStage(int currentGrowthDay)
    {
        if (currentGrowthDay >= DaysToMature) return MatureSprite;
        if (GrowthStages == null || GrowthStages.Length == 0) return SeedSprite;

        // Mapeia os dias de crescimento para o array de sprites
        int index = Mathf.Clamp(currentGrowthDay, 0, GrowthStages.Length - 1);
        return GrowthStages[index];
    }
}