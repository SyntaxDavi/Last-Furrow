using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Reorganiza as cartas da mão de acordo com diferentes critérios.
/// Delega a lógica de sorting para CardSortingStrategy.
/// Responsabilidade única: coordenar reorganização + animar para novas posições.
/// </summary>
public class HandOrganizer : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private HandLayoutConfig _layoutConfig;

    private HandManager _handManager;

    // ==============================================================================================
    // INICIALIZAÇÃO
    // ==============================================================================================

    public void Initialize(HandManager handManager)
    {
        _handManager = handManager;
    }

    /// <summary>
    /// Retorna a configuração de layout (usado por HandManualReorganizationManager)
    /// </summary>
    public HandLayoutConfig GetLayoutConfig() => _layoutConfig;

    // ==============================================================================================
    // API PÚBLICA: ESTRATÉGIAS DE ORGANIZAÇÃO
    // ==============================================================================================

    /// <summary>
    /// Reorganiza as cartas por tipo (Plant, Modify, Harvest, etc)
    /// </summary>
    public void OrganizeByType()
    {
        var cards = GetValidCards();
        if (cards == null) return;

        var strategy = new CardSortingStrategy(
            new CardTypeCriteria(),
            new CardNameCriteria()
        );
        strategy.Sort(cards);

        ApplyNewOrder(cards);
    }

    /// <summary>
    /// Reorganiza as cartas por valor (maior primeiro)
    /// </summary>
    public void OrganizeByValue()
    {
        var cards = GetValidCards();
        if (cards == null) return;

        var strategy = new CardSortingStrategy(
            new CardValueCriteria(),
            new CardNameCriteria()
        );
        strategy.Sort(cards);

        ApplyNewOrder(cards);
    }

    /// <summary>
    /// Reorganiza as cartas por aceleração de crescimento (modificadores primeiro)
    /// </summary>
    public void OrganizeByGrowthPower()
    {
        var cards = GetValidCards();
        if (cards == null) return;

        var strategy = new CardSortingStrategy(
            new CardGrowthAccelerationCriteria(),
            new CardValueMultiplierCriteria(),
            new CardNameCriteria()
        );
        strategy.Sort(cards);

        ApplyNewOrder(cards);
    }

    /// <summary>
    /// Reorganiza as cartas por nome (ordem alfabética)
    /// </summary>
    public void OrganizeByName()
    {
        var cards = GetValidCards();
        if (cards == null) return;

        var strategy = new CardSortingStrategy(
            new CardNameCriteria()
        );
        strategy.Sort(cards);

        ApplyNewOrder(cards);
    }

    /// <summary>
    /// Reorganiza as cartas com estratégia customizada.
    /// A estratégia deve implementar ICardSortingCriteria para máxima flexibilidade.
    /// </summary>
    public void OrganizeWith(CardSortingStrategy strategy)
    {
        var cards = GetValidCards();
        if (cards == null) return;

        strategy.Sort(cards);
        ApplyNewOrder(cards);
    }

    /// <summary>
    /// Embaralha as cartas usando Fisher-Yates shuffle.
    /// Evita alocação de LINQ e garante distribuição uniforme.
    /// </summary>
    public void Shuffle()
    {
        var cards = GetValidCards();
        if (cards == null) return;

        FisherYatesShuffle(cards);
        ApplyNewOrder(cards);
    }

    // ==============================================================================================
    // LÓGICA INTERNA
    // ==============================================================================================

    private void ApplyNewOrder(List<CardView> orderedCards)
    {
        if (orderedCards == null || orderedCards.Count == 0) return;

        // 1. Atualiza a ordem interna do HandManager
        // Isso garante que quando uma carta for removida, o RecalculateLayoutTargets
        // vai usar a ordem correta (shuffled/sorted) ao invés da ordem original
        _handManager.ReorderActiveCards(orderedCards);

        // 2. Atualiza os targets visuais de cada carta
        for (int i = 0; i < orderedCards.Count; i++)
        {
            RepositionCard(orderedCards[i], i, orderedCards.Count);
        }
    }

    private void RepositionCard(CardView card, int newIndex, int totalCards)
    {
        var target = HandLayoutCalculator.CalculateSlot(
            newIndex,
            totalCards,
            _layoutConfig,
            _handManager.GetHandCenterPosition()
        );

        card.UpdateLayoutTarget(target);
    }

    /// <summary>
    /// Obtém lista válida de cartas ativas.
    /// Retorna null se não houver cartas para não misturar logs com lógica.
    /// </summary>
    private List<CardView> GetValidCards()
    {
        var cards = _handManager?.GetActiveCards();

        if (cards == null || cards.Count == 0)
        {
            return null;
        }

        return cards;
    }

    /// <summary>
    /// Fisher-Yates shuffle - distribuição uniforme sem alocação LINQ.
    /// </summary>
    private void FisherYatesShuffle(List<CardView> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            // Swap
            var temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}

