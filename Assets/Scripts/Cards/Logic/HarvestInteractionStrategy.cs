using UnityEngine;

public class HarvestInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(CropState slot, CardData card)
    {
        return !slot.IsEmpty && !slot.IsWithered;
    }

    public InteractionResult Execute(CropState slot, CardData card, IGameLibrary library)
    {
        if (slot.IsEmpty) return InteractionResult.Fail("Nada para colher.");

        // 1. Busca dados
        if (!library.TryGetCrop(slot.CropID, out CropData cropData))
            return InteractionResult.Fail("Dados da planta desconhecidos.");

        // 2. Valida Maturação
        bool isMature = slot.CurrentGrowth >= cropData.DaysToMature;
        if (!isMature)
            return InteractionResult.Fail($"A planta ainda não está madura!");

        // 3. Economia
        int value = cropData.BaseSellValue;

        // TODO: Injetar IEconomyService no método Execute no futuro para remover acoplamento global
        AppCore.Instance.EconomyService.Earn(value, TransactionType.Harvest);

        // 4. Limpeza (A MUDANÇA ESTÁ AQUI)
        // Antes: 5 linhas de código repetitivo e frágil.
        // Agora: Uma chamada robusta.
        slot.Clear();

        return InteractionResult.Success(
            $"Colhido! +${value}",
            GridEventType.Harvested,
            consume: true
        );
    }
}