using UnityEngine;

public struct GridChangeEvent
{
    public int SlotIndex { get; private set; }
    public GridEventType EventType { get; private set; }
    public GridChangeImpact Impact { get; private set; }
    public GridSlotSnapshot Snapshot { get; private set; }

    public static GridChangeEvent Create(int slotIndex, GridEventType eventType, GridChangeImpact impact, GridSlotSnapshot snapshot)
    {
        return new GridChangeEvent
        {
            SlotIndex = slotIndex,
            EventType = eventType,
            Impact = impact,
            Snapshot = snapshot
        };
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
    public bool IsEmpty { get; private set; }
    public bool IsWatered { get; private set; }
    public bool IsMature { get; private set; }
    public bool IsWithered { get; private set; }
    
    // Growth Data for Visuals
    public int CurrentGrowth { get; private set; }
    public int DaysMature { get; private set; }
    
    public CropID CropID { get; private set; }

    public static GridSlotSnapshot Empty => new GridSlotSnapshot { IsEmpty = true };

    public static GridSlotSnapshot FromCropState(IReadOnlyCropState state)
    {
        if (state == null) return Empty;
        return new GridSlotSnapshot
        {
            IsEmpty = !state.CropID.IsValid,
            IsWatered = state.IsWatered,
            IsMature = state.DaysMature > 0,
            IsWithered = state.IsWithered,
            CurrentGrowth = state.CurrentGrowth,
            DaysMature = state.DaysMature,
            CropID = state.CropID
        };
    }
}
