using System.Collections.Generic;

/// <summary>
/// Interface para a biblioteca de cartas e cultivos
/// </summary>
public interface IGameLibrary
{
    bool TryGetCrop(CropID id, out CropData data);
    bool TryGetCard(CardID id, out CardData data);
    List<CardData> GetRandomCards(int count, IRandomProvider random = null);
    IEnumerable<CropData> GetAllCrops();
    IEnumerable<CardData> GetAllCards();
}
