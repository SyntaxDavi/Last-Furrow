using UnityEngine;

[CreateAssetMenu(fileName = "New Crop", menuName = "Last Furrow/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identidade")]
    public CropID ID; 
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

    /// <summary>
    /// Retorna o sprite correto baseado no dia e se está morto.
    /// </summary>
    public Sprite GetSpriteForStage(int currentGrowthDay, bool isWithered = false)
    {
        // 1. Se morreu, retorna visual de morto
        if (isWithered) return WitheredSprite;

        // 2. Se já madurou, retorna visual maduro
        if (currentGrowthDay >= DaysToMature) return MatureSprite;

        // 3. Validação de segurança para arrays vazios
        if (GrowthStages == null || GrowthStages.Length == 0) return SeedSprite;

        // 4. Mapeia o crescimento para o array
        // Mathf.Clamp garante que não estoure o array se o dia for maior que a quantidade de sprites
        int index = Mathf.Clamp(currentGrowthDay, 0, GrowthStages.Length - 1);
        return GrowthStages[index];
    }
}