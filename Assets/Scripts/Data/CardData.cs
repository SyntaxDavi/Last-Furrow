using UnityEngine;

public enum CardType
{
    Plant,      // Planta algo
    Modify,     // Acelera, Fertiliza
    Harvest,    // Colhe manualmente
    Clear       // Remove/Limpa slot
}

[CreateAssetMenu(fileName = "New Card", menuName = "Last Furrow/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Info")]
    public string ID;
    public string CardName;
    [TextArea] public string Tooltip;
    public Sprite Icon;

    [Header("Comportamento")]
    public CardType Type;
    public int Cost; // Se tiver custo de mana/ouro

    [Header("Efeito (Se for Plantio)")]
    public CropData CropToPlant; // Se Type == Plant, usa isso.

    [Header("Efeito (Se for Modificador)")]
    public int GrowthAcceleration; // Ex: +1 dia
    public int ValueMultiplier;    // Ex: 2x valor
}