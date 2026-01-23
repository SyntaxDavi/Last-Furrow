using UnityEngine;

/// <summary>
/// Comando específico para carta de Expansion.
/// </summary>
public class ExpansionCardCommand : CardCommand
{
    private readonly RunIdentityContext _context;

    public ExpansionCardCommand(CardInstance instance, CardData data, int slotIndex, RunIdentityContext context)
        : base(instance, data, slotIndex)
    {
        _context = context ?? throw new System.ArgumentNullException(nameof(context));
    }

    public override ValidationResult Validate(IGridService gridService, RunData runData)
    {
        if (TargetSlotIndex < 0 || TargetSlotIndex >= gridService.SlotCount)
            return ValidationResult.Fail("Slot inválido.");

        if (gridService.IsSlotUnlocked(TargetSlotIndex))
            return ValidationResult.Fail("Slot já está desbloqueado.");

        if (!gridService.CanUnlockSlot(TargetSlotIndex))
            return ValidationResult.Fail("Slot não pode ser desbloqueado (não está adjacente a slots desbloqueados).");

        return ValidationResult.Success();
    }

    public override CommandExecutionResult Execute(IGridService gridService, RunData runData)
    {
        if (!gridService.TryUnlockSlot(TargetSlotIndex))
        {
            return CommandExecutionResult.Fail("Falha ao desbloquear slot.");
        }

        var snapshot = CreateSnapshot(gridService, runData);
        return CommandExecutionResult.Success("Slot desbloqueado com sucesso!", snapshot, consumeCard: true);
    }

    public override void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot)
    {
        // Reverter desbloqueio (requer acesso ao GridService interno)
        // Por enquanto, apenas log
        Debug.LogWarning("[ExpansionCardCommand] Rollback de desbloqueio não implementado completamente.");
    }

    protected override StateSnapshot CreateSnapshot(IGridService gridService, RunData runData)
    {
        var slotState = gridService.GetSlotStateReadOnly(TargetSlotIndex);
        
        return new StateSnapshot(
            TargetSlotIndex,
            null,
            slotState != null ? new GridSlotState(slotState.IsUnlocked) : null,
            runData.Money,
            runData.Hand.Count
        );
    }
}
