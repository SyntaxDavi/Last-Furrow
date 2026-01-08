using System;

public class PlayerEvents
{
    // Usando CardID (Struct) em vez de string!
    public event Action<CardID> OnCardConsumed;
    public event Action<CardInstance> OnCardAdded;
    public event Action<CardID, int> OnCardOverflow;
    public event Action<IInteractable> OnInteraction;
    public event Action OnAnyInput; // Útil para "Pressione qualquer tecla"
    public event Action<CardInstance> OnCardRemoved;

    public void TriggerCardConsumed(CardID cardID) => OnCardConsumed?.Invoke(cardID);
    public void TriggerInteraction(IInteractable obj) => OnInteraction?.Invoke(obj);
    public void TriggerAnyInput() => OnAnyInput?.Invoke();
    public void TriggerCardAdded(CardInstance instance) => OnCardAdded?.Invoke(instance);
    public void TriggerCardRemoved(CardInstance instance) => OnCardRemoved?.Invoke(instance);
    public void TriggerCardOverflow(CardID id, int value) => OnCardOverflow?.Invoke(id, value);
}