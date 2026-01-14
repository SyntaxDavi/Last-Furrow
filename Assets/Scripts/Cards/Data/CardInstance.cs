using System;

[Serializable]
public struct CardInstance
{
    public string UniqueID;   // GUID único desta instância
    public CardID TemplateID; // O ID do "Asset" (ex: card_corn)

    // Futuro: 
    // public int CurrentDurability;
    // public List<CardTrait> Traits;

    public CardInstance(CardID templateID)
    {
        UniqueID = System.Guid.NewGuid().ToString();
        TemplateID = templateID;
    }
}