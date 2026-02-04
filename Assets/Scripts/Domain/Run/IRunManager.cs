using System;

/// <summary>
/// Interface do RunManager após refatoração SOLID.
/// Responsabilidade única: Mover o tempo da Run e anunciar mudanças de fase.
/// </summary>
public interface IRunManager
{
    RunPhase CurrentPhase { get; }
    bool IsRunActive { get; }
    
    event Action<RunData> OnWeekendStarted;
    event Action<RunData> OnProductionStarted;
    
    void StartNewRun();
    void AdvanceDay();
    void StartNextWeek(RunData run);
    void EndRun(RunEndReason reason);
}
