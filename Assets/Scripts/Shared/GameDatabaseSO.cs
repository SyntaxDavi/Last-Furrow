using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using LastFurrow.Traditions;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GameDatabase", menuName = "Data/Game Database")]
public class GameDatabaseSO : ScriptableObject
{
    public List<CropData> AllCrops = new List<CropData>();
    public List<CardData> AllCards = new List<CardData>();
    public List<TraditionData> AllTraditions = new List<TraditionData>();

#if UNITY_EDITOR
    [ContextMenu("Auto Populate")]
    private void AutoPopulate()
    {
        AllCrops = FindAssetsByType<CropData>();
        AllCards = FindAssetsByType<CardData>();
        AllTraditions = FindAssetsByType<TraditionData>();
        EditorUtility.SetDirty(this);
        Debug.Log($"Database populado: {AllCrops.Count} crops, {AllCards.Count} cards, {AllTraditions.Count} traditions");
    }

    private List<T> FindAssetsByType<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return guids.Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).ToList();
    }
#endif
}