using System;

// --- 1. Eventos de Estado Global ---
public class GameStateEvents
{
    public event Action<GameState> OnStateChanged;

    public void TriggerStateChanged(GameState newState)
        => OnStateChanged?.Invoke(newState);
}

public class GameEvents
{
    // Instâncias dos canais (ReadOnly para ninguém substituir o canal inteiro)
    public readonly GameStateEvents GameState = new GameStateEvents();
    public readonly TimeEvents Time = new TimeEvents();
    public readonly GridEvents Grid = new GridEvents();
    public readonly PlayerEvents Player = new PlayerEvents();
    public readonly UIEvents UI = new UIEvents();

    // Construtor (opcional, mas bom para clareza)
    public GameEvents() { }
}