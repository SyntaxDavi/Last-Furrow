using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AUDIT PATTERN
/// 
/// Registra todas as execuções de cartas para:
/// - Debug
/// - Análise de bugs
/// - Replay de ações
/// - Métricas
/// 
/// SOLID: Single Responsibility - Apenas audita execuções
/// </summary>
public class CardExecutionAudit
{
    private readonly List<AuditEntry> _entries = new List<AuditEntry>();
    private const int MAX_ENTRIES = 1000; // Previne memory leak

    public void LogCommandStart(CardCommand command)
    {
        AddEntry(AuditEventType.CommandStart, command, null);
    }

    public void LogValidationSuccess(CardCommand command)
    {
        AddEntry(AuditEventType.ValidationSuccess, command, null);
    }

    public void LogValidationFailure(CardCommand command, string reason)
    {
        AddEntry(AuditEventType.ValidationFailure, command, reason);
    }

    public void LogExecutionSuccess(CardCommand command, CommandExecutionResult result)
    {
        AddEntry(AuditEventType.ExecutionSuccess, command, result.Message);
    }

    public void LogExecutionFailure(CardCommand command, string reason)
    {
        AddEntry(AuditEventType.ExecutionFailure, command, reason);
    }

    public void LogRollback(CardCommand command, string reason)
    {
        AddEntry(AuditEventType.Rollback, command, reason);
    }

    public void LogRollbackFailure(CardCommand command, Exception ex)
    {
        AddEntry(AuditEventType.RollbackFailure, command, ex.Message);
    }

    public void LogException(CardCommand command, Exception ex)
    {
        AddEntry(AuditEventType.Exception, command, ex.Message);
    }

    public void LogUndo(CardCommand command)
    {
        AddEntry(AuditEventType.Undo, command, null);
    }

    public void LogHistoryCleared()
    {
        AddEntry(AuditEventType.HistoryCleared, null, null);
    }

    private void AddEntry(AuditEventType type, CardCommand command, string message)
    {
        if (_entries.Count >= MAX_ENTRIES)
        {
            _entries.RemoveAt(0); // Remove mais antigo
        }

        _entries.Add(new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = type,
            CommandID = command?.CommandID,
            CardID = command?.CardData?.ID?.ToString(),
            SlotIndex = command?.TargetSlotIndex ?? -1,
            Message = message
        });

        // Debug log apenas para eventos importantes
        if (type == AuditEventType.Exception || type == AuditEventType.RollbackFailure)
        {
            Debug.LogError($"[CardAudit] {type}: {message} (Command: {command?.CommandID})");
        }
    }

    /// <summary>
    /// Obtém últimas N entradas para debug.
    /// </summary>
    public List<AuditEntry> GetRecentEntries(int count = 10)
    {
        int start = Mathf.Max(0, _entries.Count - count);
        return _entries.GetRange(start, _entries.Count - start);
    }

    /// <summary>
    /// Limpa o histórico.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }
}

public enum AuditEventType
{
    CommandStart,
    ValidationSuccess,
    ValidationFailure,
    ExecutionSuccess,
    ExecutionFailure,
    Rollback,
    RollbackFailure,
    Exception,
    Undo,
    HistoryCleared
}

public class AuditEntry
{
    public DateTime Timestamp;
    public AuditEventType EventType;
    public string CommandID;
    public string CardID;
    public int SlotIndex;
    public string Message;
}
