using UnityEngine;

/// <summary>
/// ADAPTER PATTERN
/// 
/// Adapta o novo sistema de comandos para o sistema antigo de InteractionResult.
/// Permite migração gradual sem quebrar código existente.
/// 
/// SOLID: Open/Closed - Sistema antigo continua funcionando, novo sistema é opcional
/// </summary>
public static class CardCommandAdapter
{
    /// <summary>
    /// Executa uma carta usando o novo sistema de comandos e retorna InteractionResult.
    /// </summary>
    public static InteractionResult ExecuteCardWithCommand(
        CardInstance instance,
        CardData cardData,
        int slotIndex,
        IGridService gridService,
        RunData runData,
        RunIdentityContext identityContext,
        RunRuntimeContext runtimeContext)
    {
        // Validações básicas
        if (cardData == null)
            return InteractionResult.Fail("CardData é null.");

        if (gridService == null)
            return InteractionResult.Fail("GridService é null.");

        if (runData == null)
            return InteractionResult.Fail("RunData é null.");

        // Criar executor
        var audit = new CardExecutionAudit();
        var executor = new CardCommandExecutor(gridService, runData, audit);

        // Criar comando
        var command = CardCommandFactory.CreateCommand(
            instance,
            cardData,
            slotIndex,
            identityContext,
            runtimeContext
        );

        if (command == null)
        {
            return InteractionResult.Fail("Falha ao criar comando para esta carta.");
        }

        // Executar
        var result = executor.ExecuteCommand(command);

        // Converter para InteractionResult
        if (result.IsSuccess)
        {
            return InteractionResult.Success(
                result.Message,
                GridEventType.GenericUpdate,
                result.ShouldConsumeCard
            );
        }
        else
        {
            return InteractionResult.Fail(result.Message);
        }
    }
}
