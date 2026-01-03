using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Last Furrow/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Identificação")]
    public CardID ID; 
    public string Name;
    public Sprite Icon;

    [Header("Comportamento")]
    public CardType Type;

    [Header("Custo")]
    public int Cost;
    public int BaseSellValue = 2;

    [Header("Efeito (Se for Plantio)")]
    public CropData CropToPlant; 

    [Header("Efeito (Se for Modificador)")]
    public int GrowthAcceleration; // Ex: +1 dia
    public int ValueMultiplier;    // Ex: 2x valor
}