using UnityEngine;

/// <summary>
/// Estrat�gia para limpeza de plantas mortas.
/// Recebe RunIdentityContext via inje��o.
/// 
/// Valida��es contra:
/// - Null references
/// - Estados inv�lidos
/// - Double cleanup
/// </summary>
public class ClearInteractionStrategy : ICardInteractionStrategy
{
    private readonly RunIdentityContext _context;

    public ClearInteractionStrategy(RunIdentityContext context)
    {
        // Context n�o � estritamente necess�rio, mas mant�m consist�ncia
        if (context.Library == null)
            Debug.LogWarning("[ClearInteractionStrategy] RunIdentityContext.Library � nulo, mas a estrat�gia funcionar�.");
        
        _context = context;
    }

    public bool CanInteract(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlotReadOnly(index);
        if (slot == null || card == null)
            return false;
        // Regra: Pá serve para remover APENAS coisas mortas (Withered)
        // Isso garante que a carta só fique transparente sobre plantas que podem ser removidas.
        return slot.IsWithered;
    }

    public InteractionResult Execute(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlot(index);
        if (slot == null) return InteractionResult.Fail("Slot inv�lido!");

        // Valida��es defensivas
        if (card == null)
            return InteractionResult.Fail("[ERRO] CardData � null!");

        if (slot.IsEmpty)
            return InteractionResult.Fail("O slot j� est� vazio, nada para limpar.");

        if (!slot.IsWithered)
            return InteractionResult.Fail("A planta ainda est� viva! Use a Foice (Harvest) para colher.");

        try
        {
            // Garante que limpa TUDO
            slot.Clear();

            return InteractionResult.Success(
                "Planta morta removida com sucesso!",
                GridEventType.GenericUpdate,
                consume: true
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ClearInteractionStrategy] Erro ao limpar slot: {ex.Message}");
            return InteractionResult.Fail("Erro ao processar limpeza. Tente novamente.");
        }
    }
}


