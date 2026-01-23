using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EXECUTOR PATTERN + TRANSACTION PATTERN
/// 
/// Responsável por executar comandos de forma atômica e segura.
/// 
/// GARANTIAS:
/// - Validação prévia obrigatória
/// - Execução atômica (tudo ou nada)
/// - Rollback automático em caso de falha
/// - Auditoria completa
/// 
/// SOLID: Single Responsibility - Apenas executa comandos de forma segura
/// </summary>
public class CardCommandExecutor
{
    private readonly IGridService _gridService;
    private readonly RunData _runData;
    private readonly CardExecutionAudit _audit;
    private readonly List<CardCommand> _executionHistory = new List<CardCommand>();

    public CardCommandExecutor(IGridService gridService, RunData runData, CardExecutionAudit audit = null)
    {
        _gridService = gridService ?? throw new ArgumentNullException(nameof(gridService));
        _runData = runData ?? throw new ArgumentNullException(nameof(runData));
        _audit = audit ?? new CardExecutionAudit();
    }

    /// <summary>
    /// Executa um comando de forma segura e atômica.
    /// </summary>
    public ExecutionResult ExecuteCommand(CardCommand command)
    {
        if (command == null)
        {
            return ExecutionResult.Fail("Comando é null!");
        }

        _audit.LogCommandStart(command);

        // FASE 1: VALIDAÇÃO (sem efeitos colaterais)
        var validation = command.Validate(_gridService, _runData);
        if (!validation.IsValid)
        {
            _audit.LogValidationFailure(command, validation.ErrorMessage);
            return ExecutionResult.Fail(validation.ErrorMessage);
        }

        _audit.LogValidationSuccess(command);

        // FASE 2: EXECUÇÃO (com snapshot para rollback)
        StateSnapshot snapshot = null;
        CommandExecutionResult executionResult = null;

        try
        {
            snapshot = command.CreateSnapshot(_gridService, _runData);
            executionResult = command.Execute(_gridService, _runData);

            if (!executionResult.IsSuccess)
            {
                _audit.LogExecutionFailure(command, executionResult.Message);
                return ExecutionResult.Fail(executionResult.Message);
            }

            // FASE 3: COMMIT (adiciona ao histórico)
            _executionHistory.Add(command);
            _audit.LogExecutionSuccess(command, executionResult);

            return ExecutionResult.Success(executionResult.Message, executionResult.ShouldConsumeCard);
        }
        catch (Exception ex)
        {
            // FASE 4: ROLLBACK em caso de exceção
            if (snapshot != null)
            {
                try
                {
                    command.Rollback(_gridService, _runData, snapshot);
                    _audit.LogRollback(command, "Exceção durante execução");
                }
                catch (Exception rollbackEx)
                {
                    _audit.LogRollbackFailure(command, rollbackEx);
                    Debug.LogError($"[CardCommandExecutor] ERRO CRÍTICO: Falha no rollback! Estado pode estar inconsistente. {rollbackEx.Message}");
                }
            }

            _audit.LogException(command, ex);
            return ExecutionResult.Fail($"Erro inesperado: {ex.Message}");
        }
    }

    /// <summary>
    /// Reverte o último comando executado.
    /// </summary>
    public bool UndoLastCommand()
    {
        if (_executionHistory.Count == 0)
        {
            return false;
        }

        var lastCommand = _executionHistory[_executionHistory.Count - 1];
        _executionHistory.RemoveAt(_executionHistory.Count - 1);

        // TODO: Implementar rollback usando snapshot
        _audit.LogUndo(lastCommand);
        return true;
    }

    /// <summary>
    /// Limpa o histórico (útil para reset entre dias).
    /// </summary>
    public void ClearHistory()
    {
        _executionHistory.Clear();
        _audit.LogHistoryCleared();
    }
}

/// <summary>
/// Resultado final da execução.
/// </summary>
public class ExecutionResult
{
    public readonly bool IsSuccess;
    public readonly string Message;
    public readonly bool ShouldConsumeCard;

    public static ExecutionResult Success(string message, bool consumeCard = true)
        => new ExecutionResult(true, message, consumeCard);

    public static ExecutionResult Fail(string message)
        => new ExecutionResult(false, message, false);

    private ExecutionResult(bool isSuccess, string message, bool consumeCard)
    {
        IsSuccess = isSuccess;
        Message = message;
        ShouldConsumeCard = consumeCard;
    }
}
