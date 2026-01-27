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
    /// SOLID: Single Responsibility - Processa o draw diário de cartas.
    /// Idempotente: pode ser chamado múltiplas vezes sem duplicar cartas.
    /// </summary>
    public void ProcessDailyDraw(RunData runData)
    {
        if (runData == null)
        {
            Debug.LogError("[DailyHandSystem] RunData é NULL! Não é possível processar draw.");
            return;
        }

        // --- IDEMPOTÊNCIA (GUARD CLAUSE) ---
        // Agora usamos comparação cronológica robusta
        if (runData.HasDrawnDailyHand)
        {
            Debug.LogWarning($"[DailyHandSystem] Draw já realizado para o dia {runData.CurrentDay}, semana {runData.CurrentWeek}. Pulando para evitar duplicatas.");
            return;
        }

        // ? VALIDAÇÃO: Verificar se estratégia está disponível
        if (_strategy == null)
        {
            Debug.LogError("[DailyHandSystem] ICardSourceStrategy é NULL! Não é possível gerar cartas.");
            return;
        }

        Debug.Log($"[DailyHandSystem] Iniciando Draw do Dia {runData.CurrentDay}, Semana {runData.CurrentWeek}...");

        // 1. Obter IDs da estratégia
        List<CardID> nextCardIDs = _strategy.GetNextCardIDs(runData.CardsDrawPerDay, runData);

        if (nextCardIDs == null || nextCardIDs.Count == 0)
        {
            Debug.LogWarning("[DailyHandSystem] Estratégia não retornou cartas! Verifique a configuração.");
            // Mesmo sem cartas, marcamos a data atual como processada para evitar re-execução infinita
            CommitDrawTime(runData);
            return;
        }

        int cardsAdded = 0;
        int cardsOverflowed = 0;

        foreach (CardID id in nextCardIDs)
        {
            // ? VALIDAÇÃO: Verificar se ID é válido
            if (!id.IsValid)
            {
                Debug.LogWarning($"[DailyHandSystem] CardID inválido ignorado: {id}");
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
        // Marcamos a data atual como a última em que houve draw.
        CommitDrawTime(runData);
        Debug.Log($"[DailyHandSystem] ✓ Draw concluído: {cardsAdded} cartas adicionadas, {cardsOverflowed} em overflow.");
    }

    private void CommitDrawTime(RunData runData)
    {
        runData.LastDrawnDay = runData.CurrentDay;
        runData.LastDrawnWeek = runData.CurrentWeek;
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