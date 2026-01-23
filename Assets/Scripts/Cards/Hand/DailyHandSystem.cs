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

    /// <summary>
    /// SOLID: Single Responsibility - Processa o draw di�rio de cartas.
    /// Idempotente: pode ser chamado m�ltiplas vezes sem duplicar cartas.
    /// </summary>
    public void ProcessDailyDraw(RunData runData)
    {
        if (runData == null)
        {
            Debug.LogError("[DailyHandSystem] RunData � NULL! N�o � poss�vel processar draw.");
            return;
        }

        // --- IDEMPOT�NCIA (GUARD CLAUSE) ---
        if (runData.HasDrawnDailyHand)
        {
            Debug.LogWarning($"[DailyHandSystem] Draw j� realizado para o dia {runData.CurrentDay}, semana {runData.CurrentWeek}. Pulando para evitar duplicatas.");
            return;
        }

        // ? VALIDA��O: Verificar se estrat�gia est� dispon�vel
        if (_strategy == null)
        {
            Debug.LogError("[DailyHandSystem] ICardSourceStrategy � NULL! N�o � poss�vel gerar cartas.");
            return;
        }

        Debug.Log($"[DailyHandSystem] Iniciando Draw do Dia {runData.CurrentDay}, Semana {runData.CurrentWeek}...");

        // 1. Obter IDs da estrat�gia
        List<CardID> nextCardIDs = _strategy.GetNextCardIDs(runData.CardsDrawPerDay, runData);

        if (nextCardIDs == null || nextCardIDs.Count == 0)
        {
            Debug.LogWarning("[DailyHandSystem] Estrat�gia n�o retornou cartas! Verifique a configura��o.");
            // Mesmo sem cartas, marcamos como feito para evitar re-execu��o
            runData.HasDrawnDailyHand = true;
            return;
        }

        int cardsAdded = 0;
        int cardsOverflowed = 0;

        foreach (CardID id in nextCardIDs)
        {
            // ? VALIDA��O: Verificar se ID � v�lido
            if (!id.IsValid)
            {
                Debug.LogWarning($"[DailyHandSystem] CardID inv�lido ignorado: {id}");
                continue;
            }

            CardInstance newInstance = new CardInstance(id);

            if (CanAddToHand(runData))
            {
                AddToHand(runData, newInstance);
                cardsAdded++;
            }
            else
            {
                HandleOverflow(id);
                cardsOverflowed++;
            }
        }

        // --- COMMIT DO ESTADO ---
        // Marcamos como feito. Se o jogo salvar agora e crashar depois, 
        // ao recarregar, este m�todo n�o dar� cartas extras.
        runData.HasDrawnDailyHand = true;
        Debug.Log($"[DailyHandSystem] ? Draw conclu�do: {cardsAdded} cartas adicionadas, {cardsOverflowed} em overflow. Flag HasDrawnDailyHand marcada.");
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