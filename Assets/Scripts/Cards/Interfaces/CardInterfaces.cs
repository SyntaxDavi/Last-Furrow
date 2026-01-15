using System.Collections.Generic;

/// <summary>
/// Interface para objetos que podem receber cartas
/// </summary>
public interface ICardReceiver
{
    bool CanReceiveCard(CardData card);
    void OnReceiveCard(CardData card);
}

/// <summary>
/// Interface para estratégias de interação de cartas
/// </summary>
public interface ICardInteractionStrategy
{
    bool CanInteract(CropState slot, CardData card);
    InteractionResult Execute(CropState slot, CardData card, IGameLibrary library);
}

/// <summary>
/// Interface para estratégias de origem de cartas
/// </summary>
public interface ICardSourceStrategy
{
    List<CardID> GetNextCardIDs(int amount, RunData currentRun);
}
