using UnityEngine;

/// <summary>
/// Evento de domínio representando mudança atômica no grid.
/// 
/// Um evento = UMA transação de gameplay completa.
/// Listeners não precisam consultar GridService para entender o que houve.
/// </summary>
public class GridChangeEvent
{
    public int SlotIndex { get; private set; }
    public GridEventType EventType { get; private set; }
    public GridChangeImpact Impact { get; private set; }
    public GridSlotSnapshot FinalState { get; private set; }

    private GridChangeEvent() { }

    public static GridChangeEvent Create(
        int slotIndex,
        GridEventType eventType,
        GridChangeImpact impact,
        GridSlotSnapshot finalState)
    {
        return new GridChangeEvent
        {
            SlotIndex = slotIndex,
            EventType = eventType,
            Impact = impact,
            FinalState = finalState
        };
    }

    public static GridChangeEvent Simple(int slotIndex, GridEventType eventType)
    {
        return Create(
            slotIndex,
            eventType,
            new GridChangeImpact { RequiresVisualUpdate = true, RequiresSave = true },
            GridSlotSnapshot.Empty
        );
    }
}

public struct GridChangeImpact
{
    public bool RequiresVisualUpdate;
    public bool RequiresSave;
    public bool AffectsScore;
}

public struct GridSlotSnapshot
{
    public bool IsEmpty;
    public bool IsWatered;
    public bool IsMature;
    public bool IsWithered;
    public CropID CropID;

    public static GridSlotSnapshot Empty => new GridSlotSnapshot { IsEmpty = true };

    public static GridSlotSnapshot FromCropState(CropState state)
    {
        if (state == null) return Empty;

        return new GridSlotSnapshot
        {
            IsEmpty = state.IsEmpty,
            IsWatered = state.IsWatered,
            IsMature = state.DaysMature > 0,
            IsWithered = state.IsWithered,
            CropID = state.CropID
        };
    }
}
