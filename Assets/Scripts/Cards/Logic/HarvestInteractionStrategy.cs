using UnityEngine;

public class HarvestInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(CropState slot, CardData card)
    {
        // Proteções básicas
        if (slot.IsEmpty || slot.IsWithered)
            return false;

        // IMPORTANTE: Não apenas verificar CurrentGrowth,
        // mas garantir que a planta foi plantada há pelo menos 1 dia
        // (ou seja, já passou por pelo menos 1 ciclo noturno)
        // Uma planta no DIA 0 não pode ser colhida mesmo que acelerada
        return slot.DaysMature >= 0 && slot.CurrentGrowth > 0;
    }

    public InteractionResult Execute(CropState slot, CardData card, IGameLibrary library)
    {
        if (slot.IsEmpty) return InteractionResult.Fail("Nada para colher.");

        // 1. Busca dados
        if (!library.TryGetCrop(slot.CropID, out CropData cropData))
            return InteractionResult.Fail("Dados da planta desconhecidos.");

        // 2. Validação de Maturação
        // Verifica: crescimento >= dias necessários
        bool isMature = slot.CurrentGrowth >= cropData.DaysToMature;
        if (!isMature)
            return InteractionResult.Fail($"A planta ainda não está madura! Faltam {cropData.DaysToMature - slot.CurrentGrowth} dias.");

        // 3. Validação de Tempo Mínimo
        // Garante que pelo menos 1 dia passou (CurrentGrowth > 0)
        // Isso evita exploit de plantar e colher instantaneamente com aceleradores
        if (slot.CurrentGrowth <= 0)
            return InteractionResult.Fail("A planta foi plantada hoje. Espere até amanhã.");

        // 4. Economia
        int value = cropData.BaseSellValue;

        // TODO: Injetar IEconomyService no método Execute no futuro para remover acoplamento global
        AppCore.Instance.EconomyService.Earn(value, TransactionType.Harvest);

        // 5. Limpeza
        slot.Clear();

        return InteractionResult.Success(
            $"Colhido! +${value}",
            GridEventType.Harvested,
            consume: true
        );
    }
}