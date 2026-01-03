using System.Collections.Generic;
using UnityEngine; // Apenas para Random

public class SeasonalCardStrategy : ICardSourceStrategy
{
    // Em um cenário real, você injetaria uma tabela de drop rate aqui
    private readonly List<CardID> _possibleCards = new List<CardID>()
    {
        (CardID)"card_corn",
        (CardID)"card_carrot",
        (CardID)"card_water"
    };

    public List<CardID> GetNextCardIDs(int amount, RunData currentRun)
    {
        List<CardID> result = new List<CardID>();
        for (int i = 0; i < amount; i++)
        {
            // Lógica Pura: Seleciona IDs baseados em regras
            var randomID = _possibleCards[Random.Range(0, _possibleCards.Count)];
            result.Add(randomID);
        }
        return result;
    }
}