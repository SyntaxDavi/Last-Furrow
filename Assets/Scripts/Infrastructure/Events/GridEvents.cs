using System;

public class GridEvents
{
    public event Action<int> OnAnalyzeSlot;
    public void TriggerAnalyzeSlot(int slotIndex) => OnAnalyzeSlot?.Invoke(slotIndex);

    // Quando uma carta é solta fisicamente em um slot
    public event Action<int, CardData> OnCardDropped;
    public void TriggerCardDropped(int slotIndex, CardData data) => OnCardDropped?.Invoke(slotIndex, data);

    public event Action<int, int, int, int> OnCropPassiveScore;
    public void TriggerCropPassiveScore(int slotIndex, int points, int newTotal, int goal) => OnCropPassiveScore?.Invoke(slotIndex, points, newTotal, goal);

    //  NOVO: Evento de Grid Unificado
    public event Action<GridChangeEvent> OnGridChanged;
    public void TriggerGridChanged(GridChangeEvent evt) => OnGridChanged?.Invoke(evt);
}
