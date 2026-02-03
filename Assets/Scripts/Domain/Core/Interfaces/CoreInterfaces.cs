using System;

/// <summary>
/// Interface para salvamento e carregamento de dados
/// </summary>
public interface ISaveManager
{
    void SaveGame();
    void LoadGame();
    GameData Data { get; }
    void DeleteSave();
}

/// <summary>
/// Interface para gerenciamento de runs
/// </summary>
public interface IRunManager
{
    void StartNewRun();
    void AdvanceDay();
    bool IsRunActive { get; }
    void StartNextWeek(RunData run);
    RunPhase CurrentPhase { get; }
}

/// <summary>
/// Interface para gerenciamento de vida do jogador.
/// </summary>
public interface IHealthService
{
    int CurrentLives { get; }
    int MaxLives { get; }
    bool IsAtFullHealth { get; }
    
    void Heal(int amount);
    void TakeDamage(int amount);
    
    event Action<int, int> OnHealthChanged; // (current, max)
}
