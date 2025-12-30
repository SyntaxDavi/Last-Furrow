using System.Collections.Generic;

// ===================================================================================
// ESTRATÉGIAS CONCRETAS (Regras de Negócio)
// ===================================================================================

// 1. Lógica de Plantar
public class PlantInteraction : ICardInteractionStrategy
{
    public bool CanInteract(CropState slotState, CardData card)
    {
        // Regra: Só pode plantar se o slot estiver vazio (sem CropID)
        return string.IsNullOrEmpty(slotState.CropID);
    }

    public InteractionResult Execute(CropState slotState, CardData card)
    {
        if (!CanInteract(slotState, card))
            return InteractionResult.Fail("O slot já está ocupado.");

        // Aplica os dados
        slotState.CropID = card.CropToPlant.ID;
        slotState.CurrentGrowth = 0;
        slotState.IsWatered = false;
        slotState.IsWithered = false;

        return InteractionResult.Ok();
    }
}

// 2. Lógica de Modificação (Ex: Regador, Adubo)
public class ModifyInteraction : ICardInteractionStrategy
{
    public bool CanInteract(CropState slotState, CardData card)
    {
        // Regra 1: Precisa ter uma planta para modificar
        if (string.IsNullOrEmpty(slotState.CropID)) return false;

        // Regra 2: Específico para Água
        if (card.ID == "card_water")
        {
            return !slotState.IsWatered; // Só pode regar se estiver seco
        }

        // Futuro: Adubos, pesticidas, etc.
        return false;
    }

    public InteractionResult Execute(CropState slotState, CardData card)
    {
        // Validação dupla para garantir integridade e retornar mensagem de erro correta
        if (string.IsNullOrEmpty(slotState.CropID))
            return InteractionResult.Fail("Não há planta aqui.");

        if (card.ID == "card_water")
        {
            if (slotState.IsWatered)
                return InteractionResult.Fail("Já está regado.");

            slotState.IsWatered = true;
            return InteractionResult.Ok();
        }

        return InteractionResult.Fail("Item desconhecido.");
    }
}

// ===================================================================================
// FACTORY (Padrão Flyweight)
// ===================================================================================

public static class InteractionFactory
{
    // Cache estático: cria as classes uma única vez na vida do jogo.
    // Isso economiza memória e processador (Zero Garbage Collection durante gameplay).
    private static readonly Dictionary<CardType, ICardInteractionStrategy> _strategies
        = new Dictionary<CardType, ICardInteractionStrategy>();

    // Construtor estático roda automaticamente na primeira vez que a classe é usada
    static InteractionFactory()
    {
        _strategies[CardType.Plant] = new PlantInteraction();
        _strategies[CardType.Modify] = new ModifyInteraction();

        // Quando criar colheita:
        // _strategies[CardType.Harvest] = new HarvestInteraction();
    }

    public static ICardInteractionStrategy GetStrategy(CardType type)
    {
        if (_strategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }

        // Se não achar estratégia (ex: carta de ataque), retorna null
        return null;
    }
}