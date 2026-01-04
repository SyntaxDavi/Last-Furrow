using System.Collections.Generic;
using UnityEngine;

public class GameLibraryService : IGameLibrary
{
    private readonly Dictionary<CropID, CropData> _cropMap;
    private readonly Dictionary<CardID, CardData> _cardMap;

    public GameLibraryService(GameDatabaseSO database)
    {
        _cropMap = new Dictionary<CropID, CropData>();
        _cardMap = new Dictionary<CardID, CardData>();

        if (database == null) return;

        foreach (var item in database.AllCrops)
        {
            if (item != null && item.ID.IsValid)
                _cropMap.TryAdd(item.ID, item);
        }

        foreach (var item in database.AllCards)
        {
            if (item != null && item.ID.IsValid)
                _cardMap.TryAdd(item.ID, item);
        }
    }

    public bool TryGetCrop(CropID id, out CropData data) => _cropMap.TryGetValue(id, out data);
    public bool TryGetCard(CardID id, out CardData data) => _cardMap.TryGetValue(id, out data);
    public IEnumerable<CropData> GetAllCrops() => _cropMap.Values;
    public IEnumerable<CardData> GetAllCards() => _cardMap.Values;
}