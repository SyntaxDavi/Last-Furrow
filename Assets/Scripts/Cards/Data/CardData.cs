using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Last Furrow/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Identificação")]
    public CardID ID;
    public CropID CropID;
    public string Name;
    public Sprite Icon;

    [Header("Comportamento")]
    public CardType Type;

    [Header("Economia")]
    public int BaseSellValue = 2; // Valor se vender a carta na loja

    [Header("Efeito (Se for Plantio)")]
    public CropData CropToPlant;

    [Header("Efeito (Se for Modificador)")]
    public int GrowthAcceleration; // Ex: +1 dia
    public int ValueMultiplier;    // Ex: 2x valor
}