using System.Collections.Generic;

/// <summary>
/// Interface para fontes de cartas da run.
/// Abstrai a origem das cartas para o DailyHandSystem e outros consumidores.
/// </summary>
public interface IRunDeckSource
{
    /// <summary>
    /// Saca a próxima carta do deck.
    /// </summary>
    CardID DrawNext();

    /// <summary>
    /// Saca múltiplas cartas do deck.
    /// </summary>
    List<CardID> DrawNext(int amount);

    /// <summary>
    /// Cartas restantes no deck.
    /// </summary>
    int RemainingCards { get; }
}
