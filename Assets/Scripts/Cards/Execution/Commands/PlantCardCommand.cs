using UnityEngine;

/// <summary>
/// Comando específico para carta de Plant.
/// </summary>
public class PlantCardCommand : CardCommand
{
    private readonly RunIdentityContext _context;

    public PlantCardCommand(CardInstance instance, CardData data, int slotIndex, RunIdentityContext context)
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

        if (!slot.IsEmpty)
            return ValidationResult.Fail("Slot já possui uma planta.");

        // Validação de crop ID na carta
        if (CardData.CropID == null || !CardData.CropID.IsValid)
            return ValidationResult.Fail("Carta não possui crop válido.");

        if (!_context.Library.TryGetCrop(CardData.CropID, out _))
            return ValidationResult.Fail("Crop não encontrado na biblioteca.");

        return ValidationResult.Success();
    }

    public override CommandExecutionResult Execute(IGridService gridService, RunData runData)
    {
        var slot = gridService.GetSlot(TargetSlotIndex);
        if (slot == null)
        {
            return CommandExecutionResult.Fail("Slot não encontrado.");
        }

        // Planta o crop
        slot.Plant(CardData.CropID);

        var snapshot = CreateSnapshot(gridService, runData);
        return CommandExecutionResult.Success("Planta plantada com sucesso!", snapshot, consumeCard: true);
    }

    public override void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot)
    {
        if (snapshot?.CropState != null)
        {
            var slot = gridService.GetSlot(TargetSlotIndex);
            if (slot != null)
            {
                slot.Clear(); // Remove a planta
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
