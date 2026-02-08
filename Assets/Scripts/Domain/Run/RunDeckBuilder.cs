using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Monta o deck da run usando IRandomProvider para determinismo.
/// 
/// FILOSOFIA: O balanceamento mora aqui.
/// - FilterValidCards: Decide QUAIS cartas podem aparecer
/// - CalculateCopiesForDeck: Decide QUANTAS cópias de cada
/// - Shuffle: O destino é decidido uma vez
/// 
/// EXTENSIBILIDADE: Para adicionar novos filtros/modificadores,
/// modifique FilterValidCards ou CalculateCopiesForDeck.
/// </summary>
public class RunDeckBuilder
{
    private readonly IRandomProvider _random;
    private readonly IReadOnlyList<CardDropData> _allCards;

    public RunDeckBuilder(IRandomProvider random, IReadOnlyList<CardDropData> allCards)
    {
        _random = random;
        _allCards = allCards;
    }

    /// <summary>
    /// Constrói o deck completo da run usando distribuição ponderada.
    /// </summary>
    public RunDeck Build(RunData runData)
    {
        if (_allCards == null || _allCards.Count == 0)
        {
            Debug.LogError("[RunDeckBuilder] Nenhuma CardDropData configurada!");
            return new RunDeck(new List<CardID>());
        }

        var deckList = new List<CardID>();

        // 1. Filtra cartas válidas para esta run
        var validCards = FilterValidCards(runData);

        if (validCards.Count == 0)
        {
            Debug.LogWarning("[RunDeckBuilder] Nenhuma carta válida após filtros!");
            return new RunDeck(deckList);
        }

        // 2. Calcula distribuição ponderada
        int targetDeckSize = CalculateTargetDeckSize(validCards);
        var distribution = CalculateWeightedDistribution(validCards, targetDeckSize);

        // 3. Adiciona cartas ao deck conforme distribuição
        foreach (var (card, copies) in distribution)
        {
            for (int i = 0; i < copies; i++)
            {
                deckList.Add(card.CardID);
            }
        }

        Debug.LogWarning($"[RunDeckBuilder] Deck montado com {deckList.Count} cartas de {validCards.Count} tipos (target: {targetDeckSize}).");

        // 4. Embaralha o deck (Fisher-Yates via IRandomProvider)
        _random.Shuffle(deckList);

        return new RunDeck(deckList);
    }

    // ===== FILTROS DE DESIGN =====

    /// <summary>
    /// Filtra cartas que são válidas para esta run específica.
    /// </summary>
    private List<CardDropData> FilterValidCards(RunData runData)
    {
        var result = new List<CardDropData>();

        foreach (var card in _allCards)
        {
            if (card == null) continue;

            // FILTRO: Tags contextuais (exemplo para expansão futura)
            if (!IsTagAllowed(card, runData)) continue;

            result.Add(card);
        }

        return result;
    }

    /// <summary>
    /// Verifica se as tags da carta são permitidas no contexto atual.
    /// </summary>
    private bool IsTagAllowed(CardDropData card, RunData runData)
    {
        // Exemplo: bloquear cartas Premium em uma run sem upgrades
        // if (card.HasTag(CardTag.Premium) && !runData.HasPremiumUnlock) return false;

        // Por padrão, tudo é permitido
        return true;
    }

    // ===== DISTRIBUIÇÃO PONDERADA =====

    /// <summary>
    /// Calcula o tamanho alvo do deck baseado nas cartas disponíveis.
    /// Usa a soma de todos os MaxCopiesInDeck como referência.
    /// </summary>
    private int CalculateTargetDeckSize(List<CardDropData> cards)
    {
        int totalMaxCopies = 0;
        foreach (var card in cards)
        {
            totalMaxCopies += card.MaxCopiesInDeck;
        }

        // Percentual configurável via GameSettingsSO (padrão 80%)
        return Mathf.Max(100, Mathf.RoundToInt(totalMaxCopies * 0.8f));
    }

    /// <summary>
    /// Calcula quantas cópias de cada carta incluir no deck usando pesos relativos.
    /// 
    /// ALGORITMO PROFISSIONAL:
    /// 1. Calcula total de weights
    /// 2. Para cada carta, aloca proporcionalmente ao seu weight
    /// 3. Respeita MaxCopiesInDeck como limite absoluto
    /// 4. Redistribui sobras se houver cartas limitadas
    /// </summary>
    private List<(CardDropData card, int copies)> CalculateWeightedDistribution(
        List<CardDropData> cards, 
        int targetDeckSize)
    {
        var distribution = new List<(CardDropData, int)>();
        
        // 1. Calcula total de weights
        int totalWeight = 0;
        foreach (var card in cards)
        {
            totalWeight += card.Weight;
        }

        if (totalWeight == 0)
        {
            Debug.LogWarning("[RunDeckBuilder] Total weight é 0! Todas as cartas terão 1 cópia.");
            foreach (var card in cards)
            {
                distribution.Add((card, 1));
            }
            return distribution;
        }

        // 2. Primeira alocação proporcional
        int allocatedCards = 0;
        var allocations = new List<(CardDropData card, int ideal, int actual)>();

        foreach (var card in cards)
        {
            // Cálculo proporcional
            float proportion = (float)card.Weight / totalWeight;
            int idealCopies = Mathf.RoundToInt(proportion * targetDeckSize);
            
            // Respeita MaxCopiesInDeck
            int actualCopies = Mathf.Min(idealCopies, card.MaxCopiesInDeck);
            
            allocations.Add((card, idealCopies, actualCopies));
            allocatedCards += actualCopies;
        }

        // 3. Redistribui excesso se cartas foram limitadas por MaxCopies
        int remainingSlots = targetDeckSize - allocatedCards;
        if (remainingSlots > 0)
        {
            // Encontra cartas que podem receber mais
            var expandableCards = allocations
                .Where(a => a.actual < a.card.MaxCopiesInDeck)
                .OrderByDescending(a => a.card.Weight)
                .ToList();

            foreach (var allocation in expandableCards)
            {
                if (remainingSlots <= 0) break;

                int canAdd = allocation.card.MaxCopiesInDeck - allocation.actual;
                int toAdd = Mathf.Min(canAdd, remainingSlots);

                // Atualiza alocação
                var index = allocations.FindIndex(a => a.card == allocation.card);
                allocations[index] = (allocation.card, allocation.ideal, allocation.actual + toAdd);
                
                remainingSlots -= toAdd;
            }
        }

        // 4. Converte para resultado final
        foreach (var (card, _, actual) in allocations)
        {
            distribution.Add((card, actual));
        }

        return distribution;
    }
}
