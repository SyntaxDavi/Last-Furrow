using UnityEngine;

// Contrato para quem gerencia o ESTADO GLOBAL (Playing <-> Shopping)
public interface IWeekendStateFlow
{
    void EnterWeekendState();
    void ExitWeekendState();
}

// Contrato para quem gerencia a UI (Mão, HUD, Popups)
// Agora usando a semântica de Setup/Cleanup em vez de "EsconderMão"
public interface IWeekendUIFlow
{
    void SetupUIForWeekend();
    void CleanupUIAfterWeekend();
}

// Contrato para quem decide o CONTEÚDO (Qual loja abrir? Tem evento?)
public interface IWeekendContentResolver
{
    void ResolveContent(RunData currentRun);
}