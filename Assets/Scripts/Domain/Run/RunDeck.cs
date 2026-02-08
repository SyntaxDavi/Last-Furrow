using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O "destino" da run - uma fila embaralhada de cartas.
/// 
/// ARQUITETURA: O deck é gerado UMA VEZ no início da run pelo RunDeckBuilder.
/// Todas as fontes de cartas (daily draw, shop, eventos) consomem desta fila.
/// Isso garante:
/// - Determinismo (mesma seed = mesma experiência)
/// - Progressão controlada
/// - Fim do "azar infinito"
/// 
/// O deck é serializado no save para robustez.
/// </summary>
[Serializable]
public class RunDeck
{
    [SerializeField] private List<string> _cardIDs;
    [SerializeField] private int _drawIndex;

    /// <summary>
    /// Cartas restantes no deck.
    /// </summary>
    public int Remaining => Mathf.Max(0, _cardIDs.Count - _drawIndex);

    /// <summary>
    /// Total de cartas no deck (incluindo já sacadas).
    /// </summary>
    public int TotalSize => _cardIDs.Count;

    /// <summary>
    /// Cartas já sacadas.
    /// </summary>
    public int Drawn => _drawIndex;

    /// <summary>
    /// Construtor a partir de lista embaralhada.
    /// </summary>
    public RunDeck(List<CardID> shuffledCards)
    {
        _cardIDs = new List<string>();
        foreach (var card in shuffledCards)
        {
            _cardIDs.Add(card.Value);
        }
        _drawIndex = 0;
    }

    /// <summary>
    /// Construtor de deserialização (para carregar do save).
    /// </summary>
    public RunDeck(List<string> serializedIDs, int drawIndex)
    {
        _cardIDs = serializedIDs ?? new List<string>();
        _drawIndex = Mathf.Clamp(drawIndex, 0, _cardIDs.Count);
    }

    /// <summary>
    /// Saca a próxima carta do deck.
    /// Retorna CardID.Invalid se o deck estiver vazio.
    /// </summary>
    public CardID Draw()
    {
        if (_drawIndex >= _cardIDs.Count)
        {
            Debug.LogWarning("[RunDeck] Deck vazio! Retornando CardID inválido.");
            return default;
        }

        string id = _cardIDs[_drawIndex];
        _drawIndex++;
        return (CardID)id;
    }

    /// <summary>
    /// Saca múltiplas cartas do deck.
    /// </summary>
    public List<CardID> Draw(int amount)
    {
        var result = new List<CardID>();
        for (int i = 0; i < amount; i++)
        {
            var card = Draw();
            if (!card.IsValid) break;
            result.Add(card);
        }
        return result;
    }

    /// <summary>
    /// Espia a próxima carta sem removê-la.
    /// </summary>
    public CardID Peek()
    {
        if (_drawIndex >= _cardIDs.Count)
            return default;
        return (CardID)_cardIDs[_drawIndex];
    }

    /// <summary>
    /// Espia as próximas N cartas sem removê-las.
    /// </summary>
    public List<CardID> Peek(int amount)
    {
        var result = new List<CardID>();
        for (int i = 0; i < amount && (_drawIndex + i) < _cardIDs.Count; i++)
        {
            result.Add((CardID)_cardIDs[_drawIndex + i]);
        }
        return result;
    }

    // --- SERIALIZAÇÃO ---

    /// <summary>
    /// Retorna os dados para serialização no save.
    /// </summary>
    public (List<string> cardIDs, int drawIndex) GetSerializationData()
    {
        return (_cardIDs, _drawIndex);
    }
}
