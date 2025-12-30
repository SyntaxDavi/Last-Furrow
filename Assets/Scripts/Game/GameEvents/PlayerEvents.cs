using System;

public class PlayerEvents
{
    // Usando CardID (Struct) em vez de string!
    public event Action<CardID> OnCardConsumed;
    public event Action<IInteractable> OnInteraction;
    public event Action OnAnyInput; // Útil para "Pressione qualquer tecla"

    public void TriggerCardConsumed(CardID cardID) => OnCardConsumed?.Invoke(cardID);
    public void TriggerInteraction(IInteractable obj) => OnInteraction?.Invoke(obj);
    public void TriggerAnyInput() => OnAnyInput?.Invoke();
}