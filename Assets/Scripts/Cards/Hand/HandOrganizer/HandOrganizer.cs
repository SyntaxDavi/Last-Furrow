using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Reorganiza as cartas da mão de acordo com diferentes estratégias.
/// Funciona em conjunto com HandManager para animar as cartas para novas posições.
/// </summary>
public class HandOrganizer : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private HandLayoutConfig _layoutConfig;

    private HandManager _handManager;
    private List<CardView> _activeCards;

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
        if (!TryGetCards(out _activeCards)) return;

        var sorted = _activeCards
            .OrderBy(c => c.Data.Type)
            .ThenBy(c => c.Data.Name)
            .ToList();

        ApplyNewOrder(sorted);
    }

    /// <summary>
    /// Reorganiza as cartas por valor (maior primeiro)
    /// </summary>
    public void OrganizeByValue()
    {
        if (!TryGetCards(out _activeCards)) return;

        var sorted = _activeCards
            .OrderByDescending(c => c.Data.BaseSellValue)
            .ThenBy(c => c.Data.Name)
            .ToList();

        ApplyNewOrder(sorted);
    }

    /// <summary>
    /// Reorganiza as cartas por aceleração de crescimento (modificadores primeiro)
    /// </summary>
    public void OrganizeByGrowthPower()
    {
        if (!TryGetCards(out _activeCards)) return;

        var sorted = _activeCards
            .OrderByDescending(c => c.Data.GrowthAcceleration)
            .ThenByDescending(c => c.Data.ValueMultiplier)
            .ThenBy(c => c.Data.Name)
            .ToList();

        ApplyNewOrder(sorted);
    }

    /// <summary>
    /// Reorganiza as cartas por nome (ordem alfabética)
    /// </summary>
    public void OrganizeByName()
    {
        if (!TryGetCards(out _activeCards)) return;

        var sorted = _activeCards
            .OrderBy(c => c.Data.Name)
            .ToList();

        ApplyNewOrder(sorted);
    }

    /// <summary>
    /// Reorganiza as cartas customizadamente
    /// </summary>
    public void OrganizeWith(ICardOrganizationStrategy strategy)
    {
        if (!TryGetCards(out _activeCards)) return;

        var sorted = strategy.Sort(_activeCards);
        ApplyNewOrder(sorted);
    }

    /// <summary>
    /// Embaralha as cartas
    /// </summary>
    public void Shuffle()
    {
        if (!TryGetCards(out _activeCards)) return;

        var shuffled = _activeCards
            .OrderBy(_ => Random.value)
            .ToList();

        ApplyNewOrder(shuffled);
    }

    // ==============================================================================================
    // LÓGICA INTERNA
    // ==============================================================================================

    private void ApplyNewOrder(List<CardView> newOrder)
    {
        if (newOrder == null || newOrder.Count == 0) return;

        // Reposiciona cada carta para o novo índice
        for (int i = 0; i < newOrder.Count; i++)
        {
            var card = newOrder[i];
            RepositionCard(card, i, newOrder.Count);
        }
    }

    private void RepositionCard(CardView card, int newIndex, int totalCards)
    {
        // Usa o HandLayoutCalculator existente para calcular o novo target
        var target = HandLayoutCalculator.CalculateSlot(
            newIndex,
            totalCards,
            _layoutConfig,
            _handManager.GetHandCenterPosition()
        );

        // Atualiza o target da carta (ela animará para lá automaticamente via CardMovementController)
        card.UpdateLayoutTarget(target);
    }

    private bool TryGetCards(out List<CardView> cards)
    {
        cards = _handManager?.GetActiveCards();

        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("[HandOrganizer] Nenhuma carta ativa na mão.");
            return false;
        }

        return true;
    }
}

/// <summary>
/// Interface para estratégias customizadas de organização
/// Permite que o usuário implemente sua própria lógica
/// </summary>
public interface ICardOrganizationStrategy
{
    List<CardView> Sort(List<CardView> cards);
}

/// <summary>
/// Exemplo de estratégia customizada: Ordena apenas tipos Plant
/// </summary>
public class PlantFirstStrategy : ICardOrganizationStrategy
{
    public List<CardView> Sort(List<CardView> cards)
    {
        return cards
            .OrderByDescending(c => c.Data.Type == CardType.Plant)
            .ThenBy(c => c.Data.Name)
            .ToList();
    }
}

/// <summary>
/// Exemplo de estratégia customizada: Modificadores primeiro (maior efeito)
/// </summary>
public class ModifierFirstStrategy : ICardOrganizationStrategy
{
    public List<CardView> Sort(List<CardView> cards)
    {
        return cards
            .Where(c => c.Data.Type == CardType.Modify)
            .OrderByDescending(c => c.Data.GrowthAcceleration + c.Data.ValueMultiplier)
            .Concat(cards.Where(c => c.Data.Type != CardType.Modify))
            .ToList();
    }
}
