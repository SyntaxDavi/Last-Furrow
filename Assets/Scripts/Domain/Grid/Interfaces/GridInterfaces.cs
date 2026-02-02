using System;

/// <summary>
/// Interface para o serviço de grid/cultivo
/// </summary>
public interface IGridService
{
    // Evento antigo (mantemos para UI simples que só quer dar refresh)
    event Action<int> OnSlotStateChanged;

    // NOVO EVENTO RICO (Para Feedback/Audio/Particles)
    event Action<int, GridEventType> OnSlotUpdated;

    event Action OnDataDirty;

    GridSlotState GetSlotStateReadOnly(int index);
    
    // Acesso mutável para strategies (Idealmente seria internal, mas interfaces são publicas)
    CropState GetSlot(int index);
    
    // Acesso somente leitura
    int SlotCount { get; }
    GridConfiguration Config { get; }
    IReadOnlyCropState GetSlotReadOnly(int index);

    bool IsSlotUnlocked(int index);
    bool CanUnlockSlot(int index);
    bool TryUnlockSlot(int index);


    bool ProcessNightCycleForSlot(int slotIndex, out GridChangeEvent result, bool silent = false);
    void ForceVisualRefresh(int index);

    bool CanReceiveCard(int index, CardData card);
    float GetGridContaminationPercentage();
    InteractionResult ApplyCard(int index, CardData card);
}

/// <summary>
/// Interface somente leitura para estado de cultivo
/// </summary>
public interface IReadOnlyCropState
{
    CropID CropID { get; }
    int CurrentGrowth { get; }
    int DaysMature { get; } 
    bool IsWatered { get; }
    bool IsWithered { get; }

    bool IsEmpty { get; }
}

/// <summary>
/// Resultado estruturado de uma interação com o grid
/// </summary>
public readonly struct InteractionResult
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public bool ShouldConsumeCard { get; }
    public GridEventType EventType { get; } 

    private InteractionResult(bool success, string message, GridEventType type, bool consume)
    {
        IsSuccess = success;
        Message = message;
        EventType = type;
        ShouldConsumeCard = consume;
    }

    public static InteractionResult Fail(string message)
        => new InteractionResult(false, message, GridEventType.GenericUpdate, false);

    public static InteractionResult Success(string message, GridEventType type, bool consume = true)
        => new InteractionResult(true, message, type, consume);

    public static InteractionResult Ok()
        => new InteractionResult(true, "", GridEventType.GenericUpdate, true);
}

