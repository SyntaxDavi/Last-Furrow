using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public GameState CurrentState { get; private set; } = GameState.Initialization;
    public GameState PreviousState { get; private set; } 

    public void Initialize()
    {
        SetState(GameState.MainMenu);
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        PreviousState = CurrentState;
        CurrentState = newState;

        // Dispara evento global
        AppCore.Instance.Events.GameState.TriggerStateChanged(newState);

        // Lógica de controle de tempo automática (Opcional, mas útil)
        HandleTimeScale(newState);
    }

    private void HandleTimeScale(GameState state)
    {
        // Se estiver pausado ou no menu, para o tempo da engine
        if (state == GameState.Paused || state == GameState.MainMenu)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    // Helper para outros scripts checarem se podem interagir
    public bool IsGameplayActive()
    {
        return CurrentState == GameState.Playing;
    }
}