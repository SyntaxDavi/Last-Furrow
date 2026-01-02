using System.Collections.Generic;

// ===================================================================================
// ESTRATÉGIAS CONCRETAS (Regras de Negócio)
// ===================================================================================

// 1. Lógica de Plantar
public class PlantInteraction : ICardInteractionStrategy
{
    public bool CanInteract(CropState slotState, CardData card)
    {
        return slotState.IsEmpty;
    }

    public InteractionResult Execute(CropState slotState, CardData card, IGameLibrary library)
    {
        if (!CanInteract(slotState, card))
            return InteractionResult.Fail("O slot já está ocupado.");

        slotState.CropID = card.CropToPlant.ID;
        slotState.CurrentGrowth = 0;
        slotState.IsWatered = false;
        slotState.IsWithered = false;

        // RETORNA O EVENTO ESPECÍFICO
        return InteractionResult.Success(
            "Plantado com sucesso!",
            GridEventType.Planted,
            consume: true
        );
    }
}

// 2. Lógica de Modificação (Ex: Regador, Adubo)
public class ModifyInteraction : ICardInteractionStrategy
{
    public bool CanInteract(CropState slotState, CardData card)
    {
        // Se slot vazio, não tem o que modificar
        return !slotState.IsEmpty;
    }

    public InteractionResult Execute(CropState slotState, CardData card, IGameLibrary library)
    {
        if (slotState.IsEmpty)
            return InteractionResult.Fail("Não há planta aqui.");

        // Se for água, usamos a estratégia específica (Strategy dentro de Strategy ou Factory resolve antes)
        // Mas para manter compatibilidade com seu código antigo de factory:
        if (card.ID.Value == "card_water") // Aqui string ainda é ok se CardID não tiver consts
        {
            // O ideal é a Factory já retornar a WaterInteractionStrategy, 
            // mas se cair aqui, redirecionamos:
            var waterStrategy = new WaterInteractionStrategy();
            return waterStrategy.Execute(slotState, card, library);
        }

        return InteractionResult.Fail("Item modificador desconhecido.");
    }
}

// ===================================================================================
// FACTORY (Padrão Flyweight)
// ===================================================================================

public static class InteractionFactory
{
    private static readonly Dictionary<CardType, ICardInteractionStrategy> _strategies
        = new Dictionary<CardType, ICardInteractionStrategy>();

    static InteractionFactory()
    {
        _strategies[CardType.Plant] = new PlantInteraction();

        // CUIDADO: Se você tiver cartas de "Modify" que NÃO são água, 
        // precisará de uma lógica melhor aqui (ex: um Dictionary de CardID -> Strategy).
        // Por enquanto, vamos assumir que Modify = Água ou usar a ModifyInteraction genérica.

        // _strategies[CardType.Modify] = new WaterInteractionStrategy(); // Opção A: Força água
        _strategies[CardType.Modify] = new ModifyInteraction();       // Opção B: Genérico
    }

    public static ICardInteractionStrategy GetStrategy(CardType type)
    {
        if (_strategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }
        return null;
    }
}