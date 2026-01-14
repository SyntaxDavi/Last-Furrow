using System.Collections.Generic;

/// <summary>
/// Interface que define um critério de ordenação de cartas.
/// Abstrai o acesso direto a propriedades específicas de CardData.
/// Permite extensão sem acoplamento com HandOrganizer.
/// </summary>
public interface ICardSortingCriteria
{
    int Compare(CardView a, CardView b);
}

/// <summary>
/// Estratégia base para ordenar cartas.
/// Usa comparadores encapsulados em vez de LINQ.
/// </summary>
public class CardSortingStrategy
{
    private readonly List<ICardSortingCriteria> _criteria;

    public CardSortingStrategy(params ICardSortingCriteria[] criteria)
    {
        _criteria = new List<ICardSortingCriteria>(criteria);
    }

    /// <summary>
    /// Ordena uma lista de cartas aplicando os critérios em sequência.
    /// Usa algoritmo de sorting eficiente em vez de LINQ.
    /// </summary>
    public void Sort(List<CardView> cards)
    {
        if (cards == null || cards.Count <= 1) return;

        cards.Sort((a, b) =>
        {
            foreach (var criteria in _criteria)
            {
                int result = criteria.Compare(a, b);
                if (result != 0) return result;
            }
            return 0;
        });
    }
}

// ==============================================================================================
// CRITÉRIOS DE ORDENAÇÃO
// ==============================================================================================

/// <summary>
/// Critério: Ordena por tipo de carta (Plant, Modify, Harvest, etc)
/// </summary>
public class CardTypeCriteria : ICardSortingCriteria
{
    public int Compare(CardView a, CardView b)
    {
        return a.Data.Type.CompareTo(b.Data.Type);
    }
}

/// <summary>
/// Critério: Ordena por nome (alfabético)
/// </summary>
public class CardNameCriteria : ICardSortingCriteria
{
    public int Compare(CardView a, CardView b)
    {
        return a.Data.Name.CompareTo(b.Data.Name);
    }
}

/// <summary>
/// Critério: Ordena por valor de venda (maior primeiro)
/// </summary>
public class CardValueCriteria : ICardSortingCriteria
{
    public int Compare(CardView a, CardView b)
    {
        // Descending: b comparado com a
        return b.Data.BaseSellValue.CompareTo(a.Data.BaseSellValue);
    }
}

/// <summary>
/// Critério: Ordena por aceleração de crescimento (maior primeiro)
/// </summary>
public class CardGrowthAccelerationCriteria : ICardSortingCriteria
{
    public int Compare(CardView a, CardView b)
    {
        // Descending: b comparado com a
        return b.Data.GrowthAcceleration.CompareTo(a.Data.GrowthAcceleration);
    }
}

/// <summary>
/// Critério: Ordena por multiplicador de valor (maior primeiro)
/// </summary>
public class CardValueMultiplierCriteria : ICardSortingCriteria
{
    public int Compare(CardView a, CardView b)
    {
        // Descending: b comparado com a
        return b.Data.ValueMultiplier.CompareTo(a.Data.ValueMultiplier);
    }
}
