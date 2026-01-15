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
/// Interface para estratégias de interação de cartas.
/// 
/// Cada estratégia recebe RunContext via injeção (no construtor).
/// Execute NÃO precisa de parâmetros adicionais - tem tudo via contexto.
/// </summary>
public interface ICardInteractionStrategy
{
    bool CanInteract(CropState slot, CardData card);
    InteractionResult Execute(CropState slot, CardData card);
}

/// <summary>
/// Interface para estratégias de origem de cartas
/// </summary>
public interface ICardSourceStrategy
{
    List<CardID> GetNextCardIDs(int amount, RunData currentRun);
}

