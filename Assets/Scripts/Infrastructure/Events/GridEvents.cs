using System;

public class GridEvents
{
    public event Action<int> OnSlotUpdated;
    public event Action<int> OnAnalyzeSlot;

    // Quando uma carta é solta fisicamente em um slot
    public event Action<int, CardData> OnCardDropped;
    
    /// <summary>
    /// Disparado quando crop individual dá pontos passivos.
    /// Parâmetros: (slotIndex, pontos da planta, novo total, meta)
    /// </summary>
    public event Action<int, int, int, int> OnCropPassiveScore;

    public void TriggerSlotUpdated(int slotIndex) => OnSlotUpdated?.Invoke(slotIndex);
    public void TriggerAnalyzeSlot(int slotIndex) => OnAnalyzeSlot?.Invoke(slotIndex);
    public void TriggerCardDropped(int slotIndex, CardData data)
        => OnCardDropped?.Invoke(slotIndex, data);
    
    public void TriggerCropPassiveScore(int slotIndex, int cropPoints, int newTotal, int goal)
        => OnCropPassiveScore?.Invoke(slotIndex, cropPoints, newTotal, goal);
}