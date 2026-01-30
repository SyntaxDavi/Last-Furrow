using System.Collections.Generic;
using LastFurrow.Traditions;

/// <summary>
/// Interface para a biblioteca de cartas, cultivos e tradições
/// </summary>
public interface IGameLibrary
{
    bool TryGetCrop(CropID id, out CropData data);
    bool TryGetCard(CardID id, out CardData data);
    bool TryGetTradition(TraditionID id, out TraditionData data);
    
    List<CardData> GetRandomCards(int count, IRandomProvider random = null);
    List<TraditionData> GetRandomTraditions(int count, IRandomProvider random = null);
    
    IEnumerable<CropData> GetAllCrops();
    IEnumerable<CardData> GetAllCards();
    IEnumerable<TraditionData> GetAllTraditions();
}
