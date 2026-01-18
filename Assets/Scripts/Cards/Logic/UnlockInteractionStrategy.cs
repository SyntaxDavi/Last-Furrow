using UnityEngine;

public class UnlockInteractionStrategy : ICardInteractionStrategy
{
    private readonly RunIdentityContext _context;

    public UnlockInteractionStrategy(RunIdentityContext context)
    {
        _context = context;
    }

    public bool CanInteract(int index, IGridService grid, CardData card)
    {
        if (grid == null || card == null) return false;
        
        // Verifica se é desbloqueável (adjacente e bloqueado)
        return grid.CanUnlockSlot(index);
    }

    public InteractionResult Execute(int index, IGridService grid, CardData card)
    {
        if (grid.TryUnlockSlot(index))
        {
            return InteractionResult.Success("Terreno desbloqueado!", GridEventType.GenericUpdate);
        }

        return InteractionResult.Fail("Não é possível desbloquear este terreno aqui.");
    }
}
