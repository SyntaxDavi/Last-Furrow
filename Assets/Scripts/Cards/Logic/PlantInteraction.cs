using UnityEngine;

/// <summary>
/// Estratégia para plantar sementes.
/// Recebe RunIdentityContext via injeção.
/// 
/// Validações contra:
/// - Null references
/// - Dados inválidos de crop
/// - CardData corrompido
/// </summary>
public class PlantInteraction : ICardInteractionStrategy
{
    private readonly RunIdentityContext _context;

    public PlantInteraction(RunIdentityContext context)
    {
        if (context.Library == null)
            throw new System.ArgumentNullException(nameof(context), 
                "[PlantInteraction] RunIdentityContext.Library é nulo!");
        
        _context = context;
    }

    public bool CanInteract(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlotReadOnly(index);
        if (slot == null || card == null)
            return false;

        return slot.IsEmpty;
    }

    public InteractionResult Execute(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlot(index);

        // Validações defensivas
        if (slot == null)
            return InteractionResult.Fail("[ERRO] CropState é null!");

        if (card == null)
            return InteractionResult.Fail("[ERRO] CardData é null!");

        if (!slot.IsEmpty)
            return InteractionResult.Fail("Já há algo plantado aqui.");

        // Assume que card.CropToPlant existe e tem um CropID válido
        if (card.CropToPlant == null)
        {
            Debug.LogWarning($"[PlantInteraction] Card {card.Name} tem CropToPlant nulo!");
            return InteractionResult.Fail("Semente inválida ou corrompida.");
        }

        // Valida se o crop existe no library
        if (!_context.Library.TryGetCrop(card.CropToPlant.ID, out CropData cropData))
        {
            Debug.LogWarning($"[PlantInteraction] Crop desconhecido: {card.CropToPlant.ID}");
            return InteractionResult.Fail("Tipo de planta não reconhecido pelo sistema.");
        }

        if (cropData == null)
            return InteractionResult.Fail("Dados de crop corrompidos.");

        // Planta a semente
        slot.CropID = card.CropToPlant.ID;
        slot.CurrentGrowth = 0;
        slot.DaysMature = 0;
        slot.IsWithered = false;
        slot.IsWatered = false;

        return InteractionResult.Success(
            $"Plantado {cropData.Name} com sucesso!",
            GridEventType.Planted,
            consume: true
        );
    }
}



