using System;
using UnityEngine;

/// <summary>
/// Gerencia o estado global do jogo e sincroniza o Time.timeScale.
/// 
/// SOLID (SRP): Apenas gerencia estado, não sabe sobre UI ou gameplay.
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
        
        Debug.Log($"[GameState] State changed: {PreviousState} -> {CurrentState}");
        OnStateChanged?.Invoke(newState);
    }

    private void HandleTimeScale(GameState state)
    {
        // IMPORTANTE: RunEnded e ShowingResult NÃO pausam para permitir delays/animações
        switch (state)
        {
            case GameState.Paused:
            case GameState.MainMenu:
                Time.timeScale = 0f;
                break;
                
            default:
                Time.timeScale = 1f;
                break;
        }
    }

    public bool IsGameplayActive()
    {
        return CurrentState == GameState.Playing;
    }
}