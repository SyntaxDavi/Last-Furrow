public class HarvestInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(CropState slot, CardData card)
    {
        // Só pode colher se tiver planta
        return !slot.IsEmpty;
    }

    public InteractionResult Execute(CropState slot, CardData card, IGameLibrary library)
    {
        if (slot.IsEmpty) return InteractionResult.Fail("Nada para colher.");

        // 1. Busca dados da planta para saber o valor
        if (!library.TryGetCrop(slot.CropID, out CropData cropData))
            return InteractionResult.Fail("Dados da planta desconhecidos.");

        // 2. Verifica regra de maturação (opcional)
        bool isMature = slot.CurrentGrowth >= cropData.DaysToMature;

        if (!isMature)
            return InteractionResult.Fail("A planta ainda não está madura!");

        // 3. Calcula valor (Aqui entrariam modificadores de adubo/qualidade no futuro)
        int value = cropData.BaseSellValue;

        // 4. Executa a Transação Econômica
        // O GridService não mexe com dinheiro direto, ele usa o serviço global.
        AppCore.Instance.EconomyService.Earn(value, TransactionType.Harvest);

        // 5. Limpa o Slot (Trade-off: Perdeu a fonte de pontos)
        slot.CropID = CropID.Empty;
        slot.CurrentGrowth = 0;
        slot.DaysMature = 0;
        slot.IsWatered = false;
        slot.IsWithered = false;

        // 6. Retorna Sucesso e Evento Rico
        return InteractionResult.Success(
            $"Colhido! +${value}",
            GridEventType.Harvested,
            consume: true
        );
    }
}