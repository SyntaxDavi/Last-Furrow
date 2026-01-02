
public class WaterInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(CropState slot, CardData card)
    {
        return !slot.IsEmpty && !slot.IsWithered && !slot.IsWatered;
    }

    public InteractionResult Execute(CropState slot, CardData card, IGameLibrary library)
    {
        // 1. Resolve Dependência
        if (!library.TryGetCrop(slot.CropID, out CropData cropData))
            return InteractionResult.Fail("Dados da planta não encontrados na biblioteca.");
        
        // 2. Aplica Visual do Slot (Estado da terra)
        slot.IsWatered = true;

        // 3. Delega Lógica Biológica para o Core (Sem duplicar regra!)
        int acceleration = card.GrowthAcceleration > 0 ? card.GrowthAcceleration : 1;
        var simResult = CropLogic.ApplyAcceleration(slot, cropData, acceleration);

        // 4. Traduz resultado da simulação para Feedback do Jogador
        switch (simResult.EventType)
        {
            case GrowthEventType.WitheredByOverdose:
                return InteractionResult.SuccessResult("Você regou demais e a planta apodreceu instantaneamente!");

            case GrowthEventType.Matured:
                return InteractionResult.SuccessResult("A planta cresceu e amadureceu!");

            default:
                return InteractionResult.SuccessResult("Planta regada e acelerada.");
        }
    }
}