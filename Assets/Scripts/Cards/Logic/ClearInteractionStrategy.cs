public class ClearInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(CropState slot, CardData card)
    {
        // Regra: Pá serve para remover coisas mortas.
        return !slot.IsEmpty && slot.IsWithered;
    }

    public InteractionResult Execute(CropState slot, CardData card, IGameLibrary library)
    {
        if (slot.IsEmpty) return InteractionResult.Fail("O slot já está vazio.");

        if (!slot.IsWithered) return InteractionResult.Fail("A planta ainda está viva! Use a Foice.");

        // A MUDANÇA:
        // Garante que limpa TUDO, igualzinho à colheita.
        slot.Clear();

        return InteractionResult.Success(
            "Limpeza concluída.",
            GridEventType.GenericUpdate, // Poderia criar GridEventType.Cleared
            consume: true
        );
    }
}