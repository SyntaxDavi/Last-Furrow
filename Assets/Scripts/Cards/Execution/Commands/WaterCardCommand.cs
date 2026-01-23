using UnityEngine;

/// <summary>
/// Comando específico para cartas de Water/Care.
/// </summary>
public class WaterCardCommand : CardCommand
{
    private readonly RunIdentityContext _identityContext;
    private readonly RunRuntimeContext _runtimeContext;

    public WaterCardCommand(
        CardInstance instance, 
        CardData data, 
        int slotIndex, 
        RunIdentityContext identityContext,
        RunRuntimeContext runtimeContext)
        : base(instance, data, slotIndex)
    {
        _identityContext = identityContext ?? throw new System.ArgumentNullException(nameof(identityContext));
        _runtimeContext = runtimeContext ?? throw new System.ArgumentNullException(nameof(runtimeContext));
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
            return ValidationResult.Fail("Não há planta aqui para regar.");

        if (slot.IsWithered)
            return ValidationResult.Fail("A planta está morta, regar não ajuda mais.");

        if (slot.IsWatered)
            return ValidationResult.Fail("A planta já está regada.");

        return ValidationResult.Success();
    }

    public override CommandExecutionResult Execute(IGridService gridService, RunData runData)
    {
        var slot = gridService.GetSlot(TargetSlotIndex);
        if (slot == null)
        {
            return CommandExecutionResult.Fail("Slot não encontrado.");
        }

        // Regar
        slot.IsWatered = true;

        // Aplicar aceleração se tiver crop
        if (!slot.IsEmpty && _identityContext.Library.TryGetCrop(slot.CropID, out CropData cropData))
        {
            var result = CropLogic.ApplyAcceleration(slot, cropData, 1);
            // TODO: Tratar eventos de crescimento
        }

        var snapshot = CreateSnapshot(gridService, runData);
        return CommandExecutionResult.Success("Planta regada com sucesso!", snapshot, consumeCard: true);
    }

    public override void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot)
    {
        if (snapshot?.CropState != null)
        {
            var slot = gridService.GetSlot(TargetSlotIndex);
            if (slot != null)
            {
                slot.IsWatered = snapshot.CropState.IsWatered;
                // TODO: Reverter crescimento (requer snapshot mais completo)
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
