using System;
using UnityEngine;

// --- Interfaces de Core Systems ---
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

// --- Interfaces de Interação (Input System) ---
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
    /// <summary>
    /// Verifica se o alvo aceita este objeto especifico arrastável.
    /// </summary>
    bool CanReceive(IDraggable draggable);

    /// <summary>
    /// Recebe o objeto e executa a lógica de drop.
    /// </summary>
    void OnReceive(IDraggable draggable);
}

// Interface auxiliar legada ou para UI genérica (Manter por segurança)
public interface ICardReceiver
{
    bool CanReceiveCard(CardData card);
    void OnReceiveCard(CardData card);
}

// --- NOVAS ESTRUTURAS (Arquitetura Grid/Card) ---

/// <summary>
/// Define o contrato para o serviço de lógica do Grid.
/// Isso permite que o Controller não saiba que existe um SaveManager ou RunData por trás.
/// </summary>
public interface IGridService
{
    // Eventos
    event Action<int> OnSlotStateChanged;
    event Action OnDataDirty; 

    // Métodos de Leitura
    CropState GetSlotReadOnly(int index);

    // Métodos de Ação
    bool CanReceiveCard(int index, CardData card);
    InteractionResult ApplyCard(int index, CardData card);
}

/// <summary>
/// Estrutura de retorno para operações de jogo.
/// Evita apenas retornar true/false, permitindo feedback visual (ex: "Sem água!", "Já plantado!").
/// </summary>
public struct InteractionResult
{
    public bool Success;
    public string Message;

    public static InteractionResult Fail(string msg) => new InteractionResult { Success = false, Message = msg };
    public static InteractionResult Ok() => new InteractionResult { Success = true };
}

/// <summary>
/// Strategy Pattern para cartas. 
/// Define como cada tipo de carta se comporta ao interagir com um slot.
/// </summary>
public interface ICardInteractionStrategy
{
    bool CanInteract(CropState slotState, CardData card);
    InteractionResult Execute(CropState slotState, CardData card);
}