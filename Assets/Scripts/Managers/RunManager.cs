using UnityEngine;

public class RunManager : MonoBehaviour, IRunManager
{
    private ISaveManager _saveManager;

    // Constantes para evitar "Magic Numbers" (Ponto 2 do feedback)
    private const int DAYS_IN_PRODUCTION = 5;
    // O fim de semana começa logo após a produção
    private const int DAY_WEEKEND_START = DAYS_IN_PRODUCTION + 1;

    public void Initialize(ISaveManager saveManager)
    {
        _saveManager = saveManager;
    }

    public bool IsRunActive => _saveManager.Data.CurrentRun != null;

    public void StartNewRun()
    {
        // Ponto 1: Usando a Factory criada acima
        RunData newRun = RunData.CreateNewRun();

        _saveManager.Data.CurrentRun = newRun;
        _saveManager.SaveGame();

        AppCore.Instance.Events.TriggerRunStarted();

        // Setup inicial da cena
        AppCore.Instance.GameStateManager.SetState(GameState.Playing);
        AppCore.Instance.Events.TriggerDayChanged(newRun.CurrentDay);
        AppCore.Instance.Events.TriggerToggleHand(true);
    }

    // Ponto 3: O método AdvanceDay agora é um "Coordenador", não um "Operário"
    public void AdvanceDay()
    {
        if (!CanAdvanceDay()) return;

        var run = _saveManager.Data.CurrentRun;
        // 2. Muda o dia
        run.CurrentDay++;

        ProcessDayPhase(run);

        _saveManager.SaveGame();
    }

    // Helper para validar estado
    private bool CanAdvanceDay()
    {
        if (!IsRunActive) return false;

        var currentState = AppCore.Instance.GameStateManager.CurrentState;

        // Só avança se estiver jogando ou no shop
        if (currentState != GameState.Playing && currentState != GameState.Shopping)
        {
            Debug.LogWarning($"[RunManager] Tentativa de avançar dia em estado inválido: {currentState}");
            return false;
        }

        return true;
    }

    // Lógica principal quebrada em pedaços menores
    private void ProcessDayPhase(RunData run)
    {
        // Caso 1: Dias de Trabalho (1 a 5)
        if (run.CurrentDay <= DAYS_IN_PRODUCTION)
        {
            HandleProductionDay(run);
        }
        // Caso 2: Chegou o Fim de Semana (Dia 6)
        else if (run.CurrentDay == DAY_WEEKEND_START)
        {
            StartWeekendPhase();
        }
        // Caso 3: Passou do Fim de Semana (Dia 7+ ou Next Week trigger)
        else
        {
            StartNextWeek(run);
        }
    }

    private void HandleProductionDay(RunData run)
    {
        AppCore.Instance.Events.TriggerDayChanged(run.CurrentDay);

        // Verifica evento crítico de gameplay
        if (run.CurrentDay == DAYS_IN_PRODUCTION)
        {
            Debug.Log(">>> ALERTA: Último dia de colheita! Prepare-se. <<<");
            // Poderia disparar um evento de UI aqui: Events.TriggerHarvestWarning();
        }
    }

    private void StartWeekendPhase()
    {
        Debug.Log("Iniciando Fase de Fim de Semana (Shop)");

        AppCore.Instance.GameStateManager.SetState(GameState.Shopping);
        AppCore.Instance.Events.TriggerWeekendStarted();
        AppCore.Instance.Events.TriggerToggleHand(false); // Esconde cartas de plantio
    }

    private void StartNextWeek(RunData run)
    {
        run.CurrentWeek++;
        run.CurrentDay = 1;

        Debug.Log($"Iniciando Semana {run.CurrentWeek}");

        AppCore.Instance.GameStateManager.SetState(GameState.Playing);

        AppCore.Instance.Events.TriggerWeekChanged(run.CurrentWeek);
        AppCore.Instance.Events.TriggerDayChanged(1);
        AppCore.Instance.Events.TriggerToggleHand(true);
    }

    public void EndRun(RunEndReason reason)
    {
        Debug.Log($"Run Finalizada: {reason}");
        _saveManager.Data.CurrentRun = null;
        _saveManager.SaveGame();

        AppCore.Instance.GameStateManager.SetState(GameState.GameOver);
        AppCore.Instance.Events.TriggerRunEnded(reason);
    }
}