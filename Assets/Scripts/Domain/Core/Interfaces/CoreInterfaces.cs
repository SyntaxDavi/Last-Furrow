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
