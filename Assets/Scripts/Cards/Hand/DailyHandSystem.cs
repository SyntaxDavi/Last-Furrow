using UnityEngine;
using System.Collections.Generic;

public class DailyHandSystem
{
    private readonly IGameLibrary _library;
    private readonly IEconomyService _economy;
    private readonly ICardSourceStrategy _strategy;
    private readonly PlayerEvents _playerEvents; 

    public DailyHandSystem(
        IGameLibrary library,
        IEconomyService economy,
        ICardSourceStrategy strategy,
        PlayerEvents playerEvents)
    {
        _library = library;
        _economy = economy;
        _strategy = strategy;
        _playerEvents = playerEvents;
    }

    public void ProcessDailyDraw(RunData runData)
    {
        if (runData == null) return;

        // 1. Obter IDs da estratégia (Leve, sem assets)
        List<CardID> nextCardIDs = _strategy.GetNextCardIDs(runData.CardsDrawPerDay, runData);

        Debug.Log($"[DailyHand] Processando draw de {nextCardIDs.Count} cartas...");

        foreach (CardID id in nextCardIDs)
        {
            // Cria a instância (Identidade Única)
            CardInstance newInstance = new CardInstance(id);

            if (CanAddToHand(runData))
            {
                // Caminho Feliz: Adiciona
                AddToHand(runData, newInstance);
            }   
            else
            {
                // Caminho de Overflow: Vende
                HandleOverflow(id); // Passamos o TemplateID para saber o valor
            }
        }
    }

    private bool CanAddToHand(RunData runData)
    {
        return runData.Hand.Count < runData.MaxHandSize;
    }

    private void AddToHand(RunData runData, CardInstance instance)
    {
        runData.Hand.Add(instance);
        // Evento Granular (Melhor que RefreshAll)
        _playerEvents.TriggerCardAdded(instance);
    }

    private void HandleOverflow(CardID templateID)
    {
        // Precisamos consultar a Library para saber quanto vale essa carta
        if (_library.TryGetCard(templateID, out CardData data))
        {
            int value = data.BaseSellValue;

            // SEMÂNTICA CORRETA: CardOverflow
            _economy.Earn(value, TransactionType.CardOverflow);

            // Evento Granular para UI mostrar "+$2 (Overflow)"
            _playerEvents.TriggerCardOverflow(templateID, value);
        }
    }
}