using System;

public class PlayerEvents
{
    public event Action<CardView> OnCardClicked;

    public event Action<CardID> OnCardConsumed;      // Estatísticas
    public event Action<CardInstance> OnCardRemoved; // Lógica da Mão (Importante!)
    public event Action<CardInstance> OnCardAdded;
    public event Action<CardID, int> OnCardOverflow;

    public event Action<IInteractable> OnInteraction;
    public event Action OnAnyInput;

    // Triggers
    public void TriggerCardClicked(CardView card) => OnCardClicked?.Invoke(card); 
    public void TriggerCardConsumed(CardID cardID) => OnCardConsumed?.Invoke(cardID);
    public void TriggerCardRemoved(CardInstance instance) => OnCardRemoved?.Invoke(instance);
    public void TriggerCardAdded(CardInstance instance) => OnCardAdded?.Invoke(instance);
    public void TriggerCardOverflow(CardID id, int value) => OnCardOverflow?.Invoke(id, value);
    public void TriggerInteraction(IInteractable obj) => OnInteraction?.Invoke(obj);
    public void TriggerAnyInput() => OnAnyInput?.Invoke();
}