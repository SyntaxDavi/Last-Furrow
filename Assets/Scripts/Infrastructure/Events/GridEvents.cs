using System;

public class GridEvents
{
    public event Action<int> OnSlotUpdated;
    public event Action<int> OnAnalyzeSlot;

    // Quando uma carta é solta fisicamente em um slot
    public event Action<int, CardData> OnCardDropped;

    public void TriggerSlotUpdated(int slotIndex) => OnSlotUpdated?.Invoke(slotIndex);
    public void TriggerAnalyzeSlot(int slotIndex) => OnAnalyzeSlot?.Invoke(slotIndex);
    public void TriggerCardDropped(int slotIndex, CardData data)
        => OnCardDropped?.Invoke(slotIndex, data);
}