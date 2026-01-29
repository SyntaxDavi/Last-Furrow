using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Serviço de biblioteca de jogo (Dados estáticos).
/// Wrapper sobre o GameDatabaseSO para acesso fácil via script.
/// </summary>
public class GameLibraryService : IGameLibrary
{
    private readonly GameDatabaseSO _database;
    private Dictionary<CropID, CropData> _cropMap;
    private Dictionary<CardID, CardData> _cardMap;

    public GameLibraryService(GameDatabaseSO database)
    {
        _database = database;
        InitializeDictionaries(database);
    }
    private void InitializeDictionaries(GameDatabaseSO database)
    {
        _cropMap = new Dictionary<CropID, CropData>();
        _cardMap = new Dictionary<CardID, CardData>();

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

    /// <summary>
    /// Retorna N cartas aleatórias usando o provedor fornecido.
    /// Se random == null, usa Guid (comportamento legado/não-determinístico) como fallback.
    /// </summary>
    public List<CardData> GetRandomCards(int count, IRandomProvider random = null)
    {
        var allCards = _cardMap.Values.ToList();
        
        if (random != null)
        {
            // Embaralhamento determinístico (Fisher-Yates via Provider)
            random.Shuffle(allCards);
        }
        else
        {
            // Fallback não-determinístico (apenas para debug ou fora de run)
            // Uso de Guid.NewGuid() para shuffle rápido
            allCards = allCards.OrderBy(x => System.Guid.NewGuid()).ToList();
        }

        return allCards.Take(count).ToList();
    }

    public bool TryGetCrop(CropID id, out CropData data) => _cropMap.TryGetValue(id, out data);
    public bool TryGetCard(CardID id, out CardData data) => _cardMap.TryGetValue(id, out data);
    
    public IEnumerable<CropData> GetAllCrops() => _cropMap.Values;
    public IEnumerable<CardData> GetAllCards() => _cardMap.Values;
}
