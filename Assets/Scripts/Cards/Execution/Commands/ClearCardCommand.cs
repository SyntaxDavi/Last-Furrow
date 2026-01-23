using UnityEngine;

/// <summary>
/// Comando específico para carta de Clear.
/// </summary>
public class ClearCardCommand : CardCommand
{
    private readonly RunIdentityContext _context;

    public ClearCardCommand(CardInstance instance, CardData data, int slotIndex, RunIdentityContext context)
        : base(instance, data, slotIndex)
    {
        _context = context ?? throw new System.ArgumentNullException(nameof(context));
    }

    public override ValidationResult Validate(IGridService gridService, RunData runData)
    {
        if (TargetSlotIndex < 0 || TargetSlotIndex >= gridService.SlotCount)
            return ValidationResult.Fail("Slot inválido.");

        if (!gridService.IsSlotUnlocked(TargetSlotIndex))
            return ValidationResult.Fail("Slot bloqueado.");

        var slot = gridService.GetSlotReadOnly(TargetSlotIndex);
        if (slot == null)
            return ValidationResult.Fail("Slot não encontrado.");

        if (slot.IsEmpty)
            return ValidationResult.Fail("Não há nada para limpar.");

        return ValidationResult.Success();
    }

    public override CommandExecutionResult Execute(IGridService gridService, RunData runData)
    {
        var slot = gridService.GetSlot(TargetSlotIndex);
        if (slot == null)
        {
            return CommandExecutionResult.Fail("Slot não encontrado.");
        }

        var snapshot = CreateSnapshot(gridService, runData);
        slot.Clear();

        return CommandExecutionResult.Success("Slot limpo com sucesso!", snapshot, consumeCard: true);
    }

    public override void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot)
    {
        if (snapshot?.CropState != null)
        {
            var slot = gridService.GetSlot(TargetSlotIndex);
            if (slot != null)
            {
                // Restaura estado do slot
                slot.CropID = snapshot.CropState.CropID;
                slot.CurrentGrowth = snapshot.CropState.CurrentGrowth;
                slot.DaysMature = snapshot.CropState.DaysMature;
                slot.IsWatered = snapshot.CropState.IsWatered;
                slot.IsWithered = snapshot.CropState.IsWithered;
            }
        }
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
