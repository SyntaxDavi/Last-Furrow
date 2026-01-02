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
    // Evento para atualizar UI de Slots
    event Action<int> OnSlotStateChanged;
    event Action OnDataDirty;

    CropState GetSlotReadOnly(int index);
    void ProcessNightCycleForSlot(int slotIndex);

    bool CanReceiveCard(int index, CardData card);
    InteractionResult ApplyCard(int index, CardData card);
}

public struct InteractionResult
{
    public bool IsSuccess;
    public string Message;

    private InteractionResult(bool success, string message)
    {
        IsSuccess = success;
        Message = message;
    }

    public bool Success => IsSuccess;

    public static InteractionResult Fail(string message) => new InteractionResult(false, message);
    public static InteractionResult Ok() => new InteractionResult(true, "");
    public static InteractionResult SuccessResult(string message) => new InteractionResult(true, message);
}

public interface ICardInteractionStrategy
{
    bool CanInteract(CropState slot, CardData card);
    InteractionResult Execute(CropState slot, CardData card, IGameLibrary library);
}

public interface IGameLibrary
{
    bool TryGetCrop(CropID id, out CropData data);
    bool TryGetCard(CardID id, out CardData data);
    IEnumerable<CropData> GetAllCrops();
    IEnumerable<CardData> GetAllCards();
}