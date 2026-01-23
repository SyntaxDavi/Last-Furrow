using UnityEngine;

/// <summary>
/// FACTORY PATTERN + TYPE SAFETY
/// 
/// Cria comandos type-safe baseados no tipo de carta.
/// 
/// GARANTIAS:
/// - Cada tipo de carta tem seu comando específico
/// - Type safety em compile-time
/// - Fácil adicionar novos tipos sem quebrar código existente
/// 
/// SOLID: Open/Closed - Aberto para extensão, fechado para modificação
/// </summary>
public static class CardCommandFactory
{
    /// <summary>
    /// Cria um comando apropriado para o tipo de carta.
    /// </summary>
    public static CardCommand CreateCommand(
        CardInstance instance,
        CardData data,
        int slotIndex,
        RunIdentityContext identityContext,
        RunRuntimeContext runtimeContext)
    {
        if (data == null)
        {
            Debug.LogError("[CardCommandFactory] CardData é null!");
            return null;
        }

        // TYPE SAFETY: Cada tipo de carta tem seu comando específico
        switch (data.Type)
        {
            case CardType.Plant:
                return new PlantCardCommand(instance, data, slotIndex, identityContext);

            case CardType.Modify:
            case CardType.Care:
                return new WaterCardCommand(instance, data, slotIndex, identityContext, runtimeContext);

            case CardType.Harvest:
                return new HarvestCardCommand(instance, data, slotIndex, identityContext);

            case CardType.Clear:
                return new ClearCardCommand(instance, data, slotIndex, identityContext);

            case CardType.Expansion:
                return new ExpansionCardCommand(instance, data, slotIndex, identityContext);

            default:
                Debug.LogWarning($"[CardCommandFactory] Tipo de carta desconhecido: {data.Type}. Usando NullCommand.");
                return new NullCardCommand(instance, data, slotIndex);
        }
    }
}

/// <summary>
/// Comando nulo (Null Object Pattern) para tipos desconhecidos.
/// </summary>
public class NullCardCommand : CardCommand
{
    public NullCardCommand(CardInstance instance, CardData data, int slotIndex)
        : base(instance, data, slotIndex) { }

    public override ValidationResult Validate(IGridService gridService, RunData runData)
    {
        return ValidationResult.Fail($"Tipo de carta desconhecido: {CardData.Type}");
    }

    public override CommandExecutionResult Execute(IGridService gridService, RunData runData)
    {
        return CommandExecutionResult.Fail($"Tipo de carta desconhecido: {CardData.Type}");
    }

    public override void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot)
    {
        // Nada para reverter
    }

    protected override StateSnapshot CreateSnapshot(IGridService gridService, RunData runData)
    {
        return new StateSnapshot(TargetSlotIndex, null, null, runData.Money, runData.Hand.Count);
    }
}
