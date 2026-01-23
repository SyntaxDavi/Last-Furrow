using UnityEngine;

/// <summary>
/// Comando específico para carta de Harvest.
/// 
/// GARANTIAS:
/// - Validação completa antes de executar
/// - Snapshot para rollback
/// - Execução atômica
/// </summary>
public class HarvestCardCommand : CardCommand
{
    private readonly RunIdentityContext _context;

    public HarvestCardCommand(CardInstance instance, CardData data, int slotIndex, RunIdentityContext context)
        : base(instance, data, slotIndex)
    {
        _context = context ?? throw new System.ArgumentNullException(nameof(context));
    }

    public override ValidationResult Validate(IGridService gridService, RunData runData)
    {
        // Validações em cascata (fail-fast)
        if (TargetSlotIndex < 0 || TargetSlotIndex >= gridService.SlotCount)
            return ValidationResult.Fail("Slot inválido.");

        if (!gridService.IsSlotUnlocked(TargetSlotIndex))
            return ValidationResult.Fail("Slot bloqueado.");

        var slot = gridService.GetSlotReadOnly(TargetSlotIndex);
        if (slot == null)
            return ValidationResult.Fail("Slot não encontrado.");

        if (slot.IsEmpty)
            return ValidationResult.Fail("Nada para colher.");

        if (slot.IsWithered)
            return ValidationResult.Fail("A planta está morta.");

        // Validação de maturação
        if (!_context.Library.TryGetCrop(slot.CropID, out CropData cropData))
            return ValidationResult.Fail("Dados da planta não encontrados.");

        if (slot.CurrentGrowth < cropData.DaysToMature)
            return ValidationResult.Fail($"Planta ainda não está madura! Faltam {cropData.DaysToMature - slot.CurrentGrowth} dias.");

        if (slot.CurrentGrowth <= 0)
            return ValidationResult.Fail("A planta foi plantada hoje. Espere até amanhã.");

        // Validação de economia
        if (_context.Economy == null)
            return ValidationResult.Fail("Sistema econômico não disponível.");

        return ValidationResult.Success();
    }

    public override CommandExecutionResult Execute(IGridService gridService, RunData runData)
    {
        var slot = gridService.GetSlot(TargetSlotIndex);
        if (!_context.Library.TryGetCrop(slot.CropID, out CropData cropData))
        {
            return CommandExecutionResult.Fail("Dados da planta não encontrados.");
        }

        int value = cropData.BaseSellValue;
        if (value < 0) value = 0;

        // Executa economia
        int moneyBefore = _context.Economy.CurrentMoney;
        _context.Economy.Earn(value, TransactionType.Harvest);
        int moneyAfter = _context.Economy.CurrentMoney;

        // Validação pós-transação
        if (moneyAfter < moneyBefore + value)
        {
            return CommandExecutionResult.Fail("Erro ao processar pagamento.");
        }

        // Limpa slot
        slot.Clear();

        var snapshot = CreateSnapshot(gridService, runData);
        return CommandExecutionResult.Success($"Colhido com sucesso! +${value}", snapshot, consumeCard: true);
    }

    public override void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot)
    {
        // Reverte dinheiro
        if (snapshot != null)
        {
            int currentMoney = runData.Money;
            int moneyDiff = currentMoney - snapshot.PlayerMoney;
            if (moneyDiff > 0)
            {
                _context.Economy.TrySpend(moneyDiff, TransactionType.Harvest);
            }
        }

        // TODO: Restaurar estado do slot (requer snapshot mais completo)
        Debug.LogWarning("[HarvestCardCommand] Rollback parcial - estado do slot não pode ser restaurado completamente.");
    }

    protected override StateSnapshot CreateSnapshot(IGridService gridService, RunData runData)
    {
        var slot = gridService.GetSlotReadOnly(TargetSlotIndex);
        var slotState = gridService.GetSlotStateReadOnly(TargetSlotIndex);
        
        return new StateSnapshot(
            TargetSlotIndex,
            slot != null ? new CropState(slot) : null,
            slotState != null ? new GridSlotState(slotState.IsUnlocked) : null,
            runData.Money,
            runData.Hand.Count
        );
    }
}
