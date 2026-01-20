using UnityEngine;
using System;

public class RunManager : MonoBehaviour, IRunManager
{
    [Header("Configuração de Jogo")]
    [SerializeField] private ProgressionSettingsSO _progressionSettings;

    private ISaveManager _saveManager;

    // Estado privado, leitura pública
    private RunPhase _currentPhase;
    public RunPhase CurrentPhase => _currentPhase;

    private const int DAYS_IN_PRODUCTION = 5;
    private const int DAY_WEEKEND_START = 6;

    // --- NOVOS EVENTOS DE FLUXO (O FlowController escuta estes) ---
    public event Action<RunData> OnWeekendStarted;
    public event Action<RunData> OnProductionStarted;

    // --- INICIALIZAÇÃO ---
    public void Initialize(ISaveManager saveManager)
    {
        _saveManager = saveManager;

        // Restaura o estado correto ao carregar o jogo
        if (IsRunActive)
        {
            RefreshPhaseState(_saveManager.Data.CurrentRun);
        }
    }

    public bool IsRunActive => _saveManager.Data?.CurrentRun != null;

    // --- CONTROLE DE DADOS ---

    public void StartNewRun()
    {
        // Obtém config do AppCore
        var gridConfig = AppCore.Instance.GridConfiguration;
        
        // 1. Criação Pura de Dados (Domain)
        RunData newRun = RunData.CreateNewRun(gridConfig);
        
        // Ajusta meta inicial se settings existirem
        if (_progressionSettings != null)
        {
             newRun.WeeklyGoalTarget = _progressionSettings.GetGoalForWeek(1);
        }

        // 2. Persistência
        _saveManager.Data.CurrentRun = newRun;
        _saveManager.SaveGame();

        // 3. Atualiza estado interno
        RefreshPhaseState(newRun);

        // 4. Notificações
        // Aviso genérico de tempo (para HUDs simples)
        AppCore.Instance.Events.Time.TriggerRunStarted();
        AppCore.Instance.Events.Time.TriggerDayChanged(newRun.CurrentDay);

        // Aviso de Fluxo: O jogo começa em modo Produção
        // (Futuro: Você pode criar um ProductionFlowController que ouve isso)
        OnProductionStarted?.Invoke(newRun);
    }

    public void AdvanceDay()
    {
        if (!CanAdvanceDay()) return;

        var run = _saveManager.Data.CurrentRun;
        run.CurrentDay++;

        // Decide a fase baseada no novo dia
        ProcessDayPhase(run);

        _saveManager.SaveGame();
    }

    // --- LÓGICA DE FASE (PURE DOMAIN) ---

    private void ProcessDayPhase(RunData run)
    {
        // Caso 1: Dias de Trabalho (1 a 5)
        if (run.CurrentDay <= DAYS_IN_PRODUCTION)
        {
            HandleProductionDay(run);
        }
        // Caso 2: Chegou o Sábado (Dia 6)
        else if (run.CurrentDay == DAY_WEEKEND_START)
        {
            StartWeekendPhase(run);
        }
        // Caso 3: Passou do Fim de Semana (Dia 7 virando Dia 1)
        else
        {
            StartNextWeek(run);
        }
    }

    private void HandleProductionDay(RunData run)
    {
        _currentPhase = RunPhase.Production;

        // Avisa sistemas passivos (HUD de relógio)
        AppCore.Instance.Events.Time.TriggerDayChanged(run.CurrentDay);

        if (run.CurrentDay == DAYS_IN_PRODUCTION)
        {
            Debug.Log(">>> ALERTA: Último dia de colheita da semana! <<<");
        }
    }

    // Chamado quando o dia vira 6
    public void StartWeekendPhase(RunData run)
    {
        Debug.Log($"[RunManager] Fase mudou para: Weekend (Semana {run.CurrentWeek})");

        // 1. Atualiza Dado
        _currentPhase = RunPhase.Weekend;

        // 2. Dispara Fato
        // O WeekendFlowController vai pegar isso e rodar o pipeline (Fade -> UI -> Shop)
        OnWeekendStarted?.Invoke(run);

        // Mantemos o evento legado de tempo para outros sistemas menores
        AppCore.Instance.Events.Time.TriggerWeekendStarted();
    }

    // Chamado quando o dia vira 7 (resetando para 1)
    public void StartNextWeek(RunData run)
    {
        // 1. Atualiza Dados
        run.CurrentWeek++;
        run.CurrentDay = 1;

        Debug.Log($"[RunManager] Fase mudou para: Production (Semana {run.CurrentWeek})");

        // 2. Atualiza Dado
        _currentPhase = RunPhase.Production;

        // ? ONDA 4: Reset semanal do Pattern Tracking
        // Limpa histórico de padrões quebrados (para bonus de recriação)
        AppCore.Instance.OnWeeklyReset();

        // 3. Dispara Fato
        // O WeekendFlowController (no ExitPipeline) ou um ProductionFlowController vai pegar isso
        OnProductionStarted?.Invoke(run);

        // Eventos legados de tempo
        AppCore.Instance.DailyHandSystem.ProcessDailyDraw(run);
        AppCore.Instance.Events.Time.TriggerWeekChanged(run.CurrentWeek);
        AppCore.Instance.Events.Time.TriggerDayChanged(1);
    }

    public void EndRun(RunEndReason reason)
    {
        Debug.Log($"Run Finalizada: {reason}");
        _saveManager.Data.CurrentRun = null;
        _saveManager.SaveGame();

        // Aqui o GameState muda para GameOver. 
        // Idealmente, isso também seria um evento para um "GameOverFlow", mas pode ficar assim por enquanto.
        AppCore.Instance.GameStateManager.SetState(GameState.GameOver);
        AppCore.Instance.Events.Time.TriggerRunEnded(reason);
    }

    // --- HELPERS ---

    private void RefreshPhaseState(RunData run)
    {
        if (run == null) return;
        _currentPhase = (run.CurrentDay <= DAYS_IN_PRODUCTION) ? RunPhase.Production : RunPhase.Weekend;
    }

    private bool CanAdvanceDay()
    {
        if (!IsRunActive) return false;
        // Validação básica de segurança
        var s = AppCore.Instance.GameStateManager.CurrentState;
        return s == GameState.Playing || s == GameState.Shopping;
    }
}