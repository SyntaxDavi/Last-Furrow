using UnityEngine;
using System;

/// <summary>
/// Concretização do IGameStateProvider.
/// Gerencia o estado global do jogo e sincroniza o Time.timeScale.
/// </summary>
public class GameStateManager : MonoBehaviour, IGameStateProvider
{
    public GameState CurrentState { get; private set; } = GameState.Initialization;
    public GameState PreviousState { get; private set; }

    public event Action<GameState> OnStateChanged;

    public void Initialize()
    {
        SetState(GameState.MainMenu);
        Debug.Log("[GameStateManager] Initialized at MainMenu");
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        PreviousState = CurrentState;
        CurrentState = newState;

        HandleTimeScale(newState);
        
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[GameState] State changed: {PreviousState} -> {CurrentState}");
    }

    private void HandleTimeScale(GameState state)
    {
        // Se estiver pausado ou no menu, para o tempo da engine
        if (state == GameState.Paused || state == GameState.MainMenu)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    public bool IsGameplayActive()
    {
        return CurrentState == GameState.Playing;
    }
}