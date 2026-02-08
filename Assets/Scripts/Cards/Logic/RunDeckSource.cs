using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementação de ICardSourceStrategy e IRunDeckSource que usa o RunDeck.
/// Substitui SeasonalCardStrategy com aleatoriedade determinística.
/// </summary>
public class RunDeckSource : ICardSourceStrategy, IRunDeckSource
{
    private readonly RunDeck _deck;

    public RunDeckSource(RunDeck deck)
    {
        _deck = deck;
    }

    public int RemainingCards => _deck?.Remaining ?? 0;

    // IRunDeckSource
    public CardID DrawNext()
    {
        if (_deck == null)
        {
            Debug.LogError("[RunDeckSource] RunDeck é NULL!");
            return default;
        }
        return _deck.Draw();
    }

    public List<CardID> DrawNext(int amount)
    {
        if (_deck == null)
        {
            Debug.LogError("[RunDeckSource] RunDeck é NULL!");
            return new List<CardID>();
        }
        return _deck.Draw(amount);
    }

    // ICardSourceStrategy (compatibilidade com DailyHandSystem)
    public List<CardID> GetNextCardIDs(int amount, RunData currentRun)
    {
        return DrawNext(amount);
    }
}
