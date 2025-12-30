using System;
using UnityEngine;

public class GameEvents
{
    // --- ESTADO DO JOGO ---
    public event Action<GameState> OnGameStateChanged;

    // --- TEMPO / LOOP ---
    public event Action<int> OnDayChanged;
    public event Action<int> OnWeekChanged;
    public event Action OnWeekendStarted;
    public event Action OnRunStarted;

    public event Action<RunEndReason> OnRunEnded;
    public event Action OnResolutionSequenceStarted;
    public event Action<int> OnAnalyzeCropSlot; 
    public event Action OnResolutionSequenceEnded;

    // --- UI & FEEDBACK ---
    public event Action<bool> OnToggleHandVisibility;

    public event Action<IInteractable> OnInteraction;
    public event Action<int, CardData> OnCardDroppedOnSlot;
    public event Action<string> OnCardConsumed;
    public event Action<int> OnGridSlotUpdated;
    public event Action OnAnyInput;

    // --- TRIGGERS ---

    public void TriggerGameStateChanged(GameState newState)
    {
        Debug.Log($"[Event] GameState: {newState}");
        OnGameStateChanged?.Invoke(newState);
    }

    public void TriggerDayChanged(int day) => OnDayChanged?.Invoke(day);
    public void TriggerWeekChanged(int week) => OnWeekChanged?.Invoke(week);
    public void TriggerWeekendStarted() => OnWeekendStarted?.Invoke();
    public void TriggerRunStarted() => OnRunStarted?.Invoke();
    public void TriggerResolutionSequenceStarted() => OnResolutionSequenceStarted?.Invoke();
    public void TriggerCardDroppedOnSlot(int slotIndex, CardData data)
       => OnCardDroppedOnSlot?.Invoke(slotIndex, data);

    public void TriggerCardConsumed(string cardID)
        => OnCardConsumed?.Invoke(cardID);

    public void TriggerGridSlotUpdated(int slotIndex)
        => OnGridSlotUpdated?.Invoke(slotIndex);
    public void TriggerAnalyzeCropSlot(int index) => OnAnalyzeCropSlot?.Invoke(index);

    public void TriggerResolutionSequenceEnded() => OnResolutionSequenceEnded?.Invoke();
    public void TriggerRunEnded(RunEndReason reason)
    {
        Debug.Log($"[Event] Run Encerrada. Motivo: {reason}");
        OnRunEnded?.Invoke(reason);
    }

    public void TriggerToggleHand(bool show) => OnToggleHandVisibility?.Invoke(show);

    public void TriggerInteraction(IInteractable obj) => OnInteraction?.Invoke(obj);

    public void TriggerAnyInput() => OnAnyInput?.Invoke();

    public void ResetAllListeners()
    {
        OnGameStateChanged = null;
        OnDayChanged = null;
        OnWeekChanged = null;
        OnWeekendStarted = null;
        OnRunStarted = null;
        OnRunEnded = null;
        OnToggleHandVisibility = null;
        OnInteraction = null;
        OnAnyInput = null;
        Debug.Log("[GameEvents] Todos os ouvintes foram resetados.");
    }
}