using System;
using UnityEngine;

/// <summary>
/// COMMAND PATTERN + TRANSACTION PATTERN
/// 
/// Cada execução de carta é um Command imutável que pode ser:
/// - Validado antes de executar
/// - Executado atomicamente
/// - Revertido se falhar
/// - Auditado para debug
/// 
/// SOLID: Single Responsibility - Apenas encapsula uma ação de carta
/// </summary>
public abstract class CardCommand
{
    public readonly CardInstance CardInstance;
    public readonly CardData CardData;
    public readonly int TargetSlotIndex;
    public readonly DateTime Timestamp;
    public readonly string CommandID;

    protected CardCommand(CardInstance instance, CardData data, int slotIndex)
    {
        CardInstance = instance;
        CardData = data ?? throw new ArgumentNullException(nameof(data));
        TargetSlotIndex = slotIndex;
        Timestamp = DateTime.UtcNow;
        CommandID = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Valida se o comando pode ser executado (sem efeitos colaterais).
    /// </summary>
    public abstract ValidationResult Validate(IGridService gridService, RunData runData);

    /// <summary>
    /// Executa o comando. Retorna snapshot do estado antes para rollback.
    /// </summary>
    public abstract CommandExecutionResult Execute(IGridService gridService, RunData runData);

    /// <summary>
    /// Reverte o comando usando o snapshot.
    /// </summary>
    public abstract void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot);

    /// <summary>
    /// Obtém snapshot do estado atual (para rollback).
    /// </summary>
    protected abstract StateSnapshot CreateSnapshot(IGridService gridService, RunData runData);
}

/// <summary>
/// Snapshot do estado para rollback.
/// </summary>
public class StateSnapshot
{
    public readonly int SlotIndex;
    public readonly CropState CropState;
    public readonly GridSlotState SlotState;
    public readonly int PlayerMoney;
    public readonly int HandSize;

    public StateSnapshot(int slotIndex, CropState cropState, GridSlotState slotState, int money, int handSize)
    {
        SlotIndex = slotIndex;
        CropState = cropState != null ? new CropState(cropState) : null; // Deep copy
        SlotState = slotState != null ? new GridSlotState(slotState.IsUnlocked) : null;
        PlayerMoney = money;
        HandSize = handSize;
    }
}

/// <summary>
/// Resultado da validação.
/// </summary>
public class ValidationResult
{
    public readonly bool IsValid;
    public readonly string ErrorMessage;

    public static ValidationResult Success() => new ValidationResult(true, null);
    public static ValidationResult Fail(string message) => new ValidationResult(false, message);

    private ValidationResult(bool isValid, string errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Resultado da execução.
/// </summary>
public class CommandExecutionResult
{
    public readonly bool IsSuccess;
    public readonly string Message;
    public readonly StateSnapshot Snapshot;
    public readonly bool ShouldConsumeCard;

    public static CommandExecutionResult Success(string message, StateSnapshot snapshot, bool consumeCard = true)
        => new CommandExecutionResult(true, message, snapshot, consumeCard);

    public static CommandExecutionResult Fail(string message)
        => new CommandExecutionResult(false, message, null, false);

    private CommandExecutionResult(bool isSuccess, string message, StateSnapshot snapshot, bool consumeCard)
    {
        IsSuccess = isSuccess;
        Message = message;
        Snapshot = snapshot;
        ShouldConsumeCard = consumeCard;
    }
}
