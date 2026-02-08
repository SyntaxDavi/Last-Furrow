using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Biblioteca centralizada de todas as configurações de drop de cartas.
/// Usada pelo RunDeckBuilder para montar o deck da run.
/// </summary>
[CreateAssetMenu(fileName = "Card Drop Library", menuName = "Last Furrow/Card Drop Library")]
public class CardDropLibrarySO : ScriptableObject
{
    [Header("Configurações de Drop")]
    [Tooltip("Lista de todas as CardDropData disponíveis no jogo.")]
    [SerializeField] private List<CardDropData> _allCardDrops = new();

    /// <summary>
    /// Retorna todas as configurações de drop de cartas.
    /// </summary>
    public IReadOnlyList<CardDropData> AllCardDrops => _allCardDrops;

    /// <summary>
    /// Retorna o número total de cartas configuradas.
    /// </summary>
    public int Count => _allCardDrops.Count;

    /// <summary>
    /// Tenta encontrar a configuração de drop para um CardID específico.
    /// </summary>
    public bool TryGetDropData(CardID cardID, out CardDropData dropData)
    {
        foreach (var drop in _allCardDrops)
        {
            if (drop != null && drop.CardID == cardID)
            {
                dropData = drop;
                return true;
            }
        }
        dropData = null;
        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Populate from Project")]
    private void AutoPopulateFromProject()
    {
        _allCardDrops.Clear();
        var guids = UnityEditor.AssetDatabase.FindAssets("t:CardDropData");
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDropData>(path);
            if (data != null && !_allCardDrops.Contains(data))
            {
                _allCardDrops.Add(data);
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[CardDropLibrarySO] Auto-populated {_allCardDrops.Count} card drops.");
    }
#endif
}
