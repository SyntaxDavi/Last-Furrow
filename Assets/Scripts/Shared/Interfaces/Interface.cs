using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveManager
{
    void SaveGame();
    void LoadGame();
    GameData Data { get; }
    void DeleteSave();
}

public interface IRunManager
{
    void StartNewRun();
    void AdvanceDay();
    bool IsRunActive { get; }
}

public interface IInteractable
{
    void OnClick();
    void OnHoverEnter();
    void OnHoverExit();
}

public interface IDraggable
{
    void OnDragStart();
    void OnDragUpdate(Vector2 worldPosition);
    void OnDragEnd();
}

public interface IDropTarget
{
    bool CanReceive(IDraggable draggable);
    void OnReceive(IDraggable draggable);
}

public interface ICardReceiver
{
    bool CanReceiveCard(CardData card);
    void OnReceiveCard(CardData card);
}

// --- INTERFACE CORRIGIDA DO GRID SERVICE ---
public interface IGridService
{
    // Evento antigo (mantemos para UI simples que só quer dar refresh)
    event System.Action<int> OnSlotStateChanged;

    // NOVO EVENTO RICO (Para Feedback/Audio/Particles)
    event System.Action<int, GridEventType> OnSlotUpdated;

    event System.Action OnDataDirty;

    IReadOnlyCropState GetSlotReadOnly(int index);

    void ProcessNightCycleForSlot(int slotIndex);
    bool CanReceiveCard(int index, CardData card);
    InteractionResult ApplyCard(int index, CardData card);
}

public readonly struct InteractionResult
{
    // 1. Propriedades de Dados (Renomeado Success -> IsSuccess para evitar conflito)
    public bool IsSuccess { get; }
    public string Message { get; }
    public bool ShouldConsumeCard { get; }
    public GridEventType EventType { get; } 

    // 2. Construtor Privado
    private InteractionResult(bool success, string message, GridEventType type, bool consume)
    {
        IsSuccess = success;
        Message = message;
        EventType = type;
        ShouldConsumeCard = consume;
    }

    // --- MÉTODOS FÁBRICA ---

    public static InteractionResult Fail(string message)
        => new InteractionResult(false, message, GridEventType.GenericUpdate, false);

    // O método pode chamar "Success" agora porque a propriedade chama "IsSuccess"
    public static InteractionResult Success(string message, GridEventType type, bool consume = true)
        => new InteractionResult(true, message, type, consume);

    public static InteractionResult Ok()
        => new InteractionResult(true, "", GridEventType.GenericUpdate, true);
}

public interface ICardInteractionStrategy
{
    bool CanInteract(CropState slot, CardData card);
    InteractionResult Execute(CropState slot, CardData card, IGameLibrary library);
}
public interface ICardSourceStrategy
{
    List<CardID> GetNextCardIDs(int amount, RunData currentRun);
}
public interface IGameLibrary
{
    bool TryGetCrop(CropID id, out CropData data);
    bool TryGetCard(CardID id, out CardData data);
    IEnumerable<CropData> GetAllCrops();
    IEnumerable<CardData> GetAllCards();
}

public interface IReadOnlyCropState
{
    CropID CropID { get; }
    int CurrentGrowth { get; }
    int DaysMature { get; } 
    bool IsWatered { get; }
    bool IsWithered { get; }

    bool IsEmpty { get; }
}

public interface IEconomyService
{
    // Leitura
    int CurrentMoney { get; }

    // Ações
    void Earn(int amount, TransactionType source);
    bool TrySpend(int amount, TransactionType reason);

    // Eventos
    // Int: Novo Saldo, Int: Diferença (+/-), Type: Motivo
    event System.Action<int, int, TransactionType> OnBalanceChanged;
}