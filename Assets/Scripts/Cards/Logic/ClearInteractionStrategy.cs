using UnityEngine;

/// <summary>
/// Estratégia para limpeza de plantas mortas.
/// Recebe RunIdentityContext via injeção.
/// 
/// Validações contra:
/// - Null references
/// - Estados inválidos
/// - Double cleanup
/// </summary>
public class ClearInteractionStrategy : ICardInteractionStrategy
{
    private readonly RunIdentityContext _context;

    public ClearInteractionStrategy(RunIdentityContext context)
    {
        // Context não é estritamente necessário, mas mantém consistência
        if (context.Library == null)
            Debug.LogWarning("[ClearInteractionStrategy] RunIdentityContext.Library é nulo, mas a estratégia funcionará.");
        
        _context = context;
    }

    public bool CanInteract(CropState slot, CardData card)
    {
        if (slot == null || card == null)
            return false;

        // Regra: Pá serve para remover coisas mortas
        return !slot.IsEmpty && slot.IsWithered;
    }

    public InteractionResult Execute(CropState slot, CardData card)
    {
        // Validações defensivas
        if (slot == null)
            return InteractionResult.Fail("[ERRO] CropState é null!");

        if (card == null)
            return InteractionResult.Fail("[ERRO] CardData é null!");

        if (slot.IsEmpty)
            return InteractionResult.Fail("O slot já está vazio, nada para limpar.");

        if (!slot.IsWithered)
            return InteractionResult.Fail("A planta ainda está viva! Use a Foice (Harvest) para colher.");

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

