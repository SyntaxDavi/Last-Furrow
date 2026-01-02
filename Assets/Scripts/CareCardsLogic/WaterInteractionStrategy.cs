public class WaterInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(CropState slot, CardData card)
    {
        return !slot.IsEmpty && !slot.IsWithered && !slot.IsWatered;
    }

    public InteractionResult Execute(CropState slot, CardData card, IGameLibrary library)
    {
        if (!library.TryGetCrop(slot.CropID, out CropData cropData))
            return InteractionResult.Fail("Dados da planta não encontrados.");

        slot.IsWatered = true;

        int acceleration = card.GrowthAcceleration > 0 ? card.GrowthAcceleration : 1;

        // Lógica Biológica
        var simResult = CropLogic.ApplyAcceleration(slot, cropData, acceleration);

        GridEventType finalEvent;
        string msg;

        switch (simResult.EventType)
        {
            case GrowthEventType.WitheredByOverdose:
                finalEvent = GridEventType.Withered;
                msg = "Você regou demais e a planta morreu!";
                break;

            case GrowthEventType.Matured:
                finalEvent = GridEventType.Matured;
                msg = "A aceleração fez a planta amadurecer!";
                break;

            default:
                finalEvent = GridEventType.Watered;
                msg = "Planta regada.";
                break;
        }

        return InteractionResult.Success(msg, finalEvent, consume: true);
    }
}