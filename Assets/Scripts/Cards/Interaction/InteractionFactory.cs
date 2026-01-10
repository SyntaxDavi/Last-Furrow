using System.Collections.Generic;

// Estratégia de Segurança (Null Object)
// Se alguém pedir um tipo de carta que não existe, isso roda e não quebra o jogo.
public class NullInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(CropState slot, CardData card) => false;

    public InteractionResult Execute(CropState slot, CardData card, IGameLibrary library)
    {
        return InteractionResult.Fail($"Nenhuma estratégia definida para o tipo: {card.Type}");
    }
}

public static class InteractionFactory
{
    private static readonly Dictionary<CardType, ICardInteractionStrategy> _strategies
        = new Dictionary<CardType, ICardInteractionStrategy>();

    // Instância estática da estratégia nula para evitar alocação de memória repetida
    private static readonly NullInteractionStrategy _nullStrategy = new NullInteractionStrategy();

    static InteractionFactory()
    {
        _strategies[CardType.Plant] = new PlantInteraction();
        _strategies[CardType.Modify] = new WaterInteractionStrategy();
        _strategies[CardType.Care] = new WaterInteractionStrategy();
        _strategies[CardType.Harvest] = new HarvestInteractionStrategy();
        _strategies[CardType.Clear] = new ClearInteractionStrategy();
    }

    public static ICardInteractionStrategy GetStrategy(CardType type)
    {
        if (_strategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }

        // CORREÇÃO: Nunca retorna null. Retorna um objeto que sabe dizer "não funciono".
        // Isso evita NullReferenceException no código que chama (GridService).
        return _nullStrategy;
    }
}