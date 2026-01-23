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

        // --- IDEMPOTÊNCIA (GUARD CLAUSE) ---
        if (runData.HasDrawnDailyHand)
        {
            Debug.LogWarning($"[DailyHandSystem] Draw já realizado para o dia {runData.CurrentDay}. Pulando para evitar duplicatas.");
            return;
        }

        Debug.Log($"[DailyHandSystem] Iniciando Draw do Dia {runData.CurrentDay}...");

        // 1. Obter IDs da estratégia
        List<CardID> nextCardIDs = _strategy.GetNextCardIDs(runData.CardsDrawPerDay, runData);

        foreach (CardID id in nextCardIDs)
        {
            CardInstance newInstance = new CardInstance(id);

            if (CanAddToHand(runData))
            {
                AddToHand(runData, newInstance);
            }
            else
            {
                HandleOverflow(id);
            }
        }

        // --- COMMIT DO ESTADO ---
        // Marcamos como feito. Se o jogo salvar agora e crashar depois, 
        // ao recarregar, este método não dará cartas extras.
        runData.HasDrawnDailyHand = true;
        Debug.Log("[DailyHandSystem] Draw concluído e flag HasDrawnDailyHand marcada.");
    }

    private bool CanAddToHand(RunData runData) => runData.Hand.Count < runData.MaxHandSize;

    private void AddToHand(RunData runData, CardInstance instance)
    {
        runData.Hand.Add(instance);
        // Evento Granular (Melhor que RefreshAll)
        _playerEvents.TriggerCardAdded(instance);
    }

    private void HandleOverflow(CardID templateID)
    {
        if (_library.TryGetCard(templateID, out CardData data))
        {
            int value = data.BaseSellValue;
            _economy.Earn(value, TransactionType.CardOverflow);
            _playerEvents.TriggerCardOverflow(templateID, value);
        }
    }
}