using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GameDatabase", menuName = "Data/Game Database")]
public class GameDatabaseSO : ScriptableObject
{
    public List<CropData> AllCrops = new List<CropData>();
    public List<CardData> AllCards = new List<CardData>();

#if UNITY_EDITOR
    [ContextMenu("Auto Populate")]
    private void AutoPopulate()
    {
        AllCrops = FindAssetsByType<CropData>();
        AllCards = FindAssetsByType<CardData>();
        EditorUtility.SetDirty(this);
        Debug.Log("Database populado automaticamente!");
    }

    private List<T> FindAssetsByType<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return guids.Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).ToList();
    }
#endif
}