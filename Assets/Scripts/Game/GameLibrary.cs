using UnityEngine;
using System.Collections.Generic;

public class GameLibrary : MonoBehaviour
{
    public static GameLibrary Instance { get; private set; }

    private Dictionary<string, CropData> _cropDict;
    private Dictionary<string, CardData> _cardDict;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadLibrary();
    }

    private void LoadLibrary()
    {
        _cropDict = new Dictionary<string, CropData>();
        _cardDict = new Dictionary<string, CardData>();

        var crops = Resources.LoadAll<CropData>("Crops");
        var cards = Resources.LoadAll<CardData>("Cards");

        foreach (var c in crops)
        {
            if (_cropDict.ContainsKey(c.ID.Value))
                Debug.LogWarning($"ID de Crop duplicado: {c.ID}");
            else
                _cropDict.Add(c.ID.Value, c);
        }

        foreach (var c in cards)
        {
            // CORREÇÃO: Use c.ID.Value
            if (_cardDict.ContainsKey(c.ID.Value))
                Debug.LogWarning($"ID de Card duplicado: {c.ID}");
            else
                _cardDict.Add(c.ID.Value, c);
        }

        Debug.Log($"[GameLibrary] Carregado: {crops.Length} Plantas | {cards.Length} Cartas");
    }

    public CropData GetCrop(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _cropDict.TryGetValue(id, out var data);
        return data;
    }

    public CardData GetCard(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _cardDict.TryGetValue(id, out var data);
        return data;
    }
}