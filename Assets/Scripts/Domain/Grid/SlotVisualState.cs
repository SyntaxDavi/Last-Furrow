using UnityEngine;

/// <summary>
/// Value Object imutável que representa o estado visual de um slot.
/// Substitui múltiplos parâmetros bool por um objeto semântico.
/// </summary>
public readonly struct SlotVisualState
{
    public readonly Sprite PlantSprite;
    public readonly bool IsWatered;
    public readonly PlantMaturity Maturity;

    public SlotVisualState(Sprite plantSprite, bool isWatered, PlantMaturity maturity)
    {
        PlantSprite = plantSprite;
        IsWatered = isWatered;
        Maturity = maturity;
    }

    /// <summary>
    /// Factory method para criar estado a partir de booleans legados.
    /// Facilita migração gradual.
    /// </summary>
    public static SlotVisualState FromLegacy(Sprite plantSprite, bool isWatered, bool isMature, bool isWithered)
    {
        PlantMaturity maturity = PlantMaturity.None;
        
        if (plantSprite != null)
        {
            if (isWithered)
                maturity = PlantMaturity.Withered;
            else if (isMature)
                maturity = PlantMaturity.Mature;
            else
                maturity = PlantMaturity.Growing;
        }

        return new SlotVisualState(plantSprite, isWatered, maturity);
    }
}