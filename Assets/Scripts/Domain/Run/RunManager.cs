using UnityEngine;
using System;

public class RunManager : MonoBehaviour, IRunManager
{
    [Header("Configuração de Jogo")]
    [SerializeField] private ProgressionSettingsSO _progressionSettings;

    // Dependências injetadas
    private ISaveManager _saveManager;
    private GridConfiguration _gridConfiguration;
    private TimeEvents _timeEvents;
    private IGameStateProvider _gameStateProvider;
    private Action _onWeeklyResetCallback;

    // Estado privado, leitura pública
    private RunPhase _currentPhase;
    public RunPhase CurrentPhase => _currentPhase;

    private const int DAYS_IN_PRODUCTION = 5;
    private const int DAY_WEEKEND_START = 6;

    // --- EVENTOS DE FLUXO (O FlowController escuta estes) ---
    public event Action<RunData> OnWeekendStarted;
    public event Action<RunData> OnProductionStarted;

    // --- INICIALIZAÇÃO ---
    /// <summary>
    /// Inicializa o RunManager com todas as dependências necessárias.
    /// SOLID: Injeção de Dependência Explícita - sem acesso a AppCore.Instance.
    /// </summary>
    public void Initialize(
        ISaveManager saveManager,
        GridConfiguration gridConfiguration,
        TimeEvents timeEvents,
        IGameStateProvider gameStateProvider,
        Action onWeeklyResetCallback)
    {
        _saveManager = saveManager;
        _gridConfiguration = gridConfiguration;
        _timeEvents = timeEvents;
        _gameStateProvider = gameStateProvider;
        _onWeeklyResetCallback = onWeeklyResetCallback;

        // Restaura o estado correto ao carregar o jogo
        if (IsRunActive)
        {
            RefreshPhaseState(_saveManager.Data.CurrentRun);
        }
        
        Debug.Log("[RunManager] ✓ Inicializado com injeção de dependências");
    }

    public bool IsRunActive => _saveManager?.Data?.CurrentRun != null;

    // --- CONTROLE DE DADOS ---

    public void StartNewRun()
    {
        // 1. Criação Pura de Dados (Domain) - usa dependência injetada
        RunData newRun = RunData.CreateNewRun(_gridConfiguration);

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

        // 4. Notificações - usa dependência injetada
        _timeEvents.TriggerRunStarted();
        _timeEvents.TriggerDayChanged(newRun.CurrentDay);

        // Aviso de Fluxo: O jogo começa em modo Produção
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

        // Avisa sistemas passivos (HUD de relógio) - usa dependência injetada
        _timeEvents.TriggerDayChanged(run.CurrentDay);

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
        OnWeekendStarted?.Invoke(run);

        // Mantemos o evento legado de tempo - usa dependência injetada
        _timeEvents.TriggerWeekendStarted();
    }

    // Chamado quando o dia vira 7 (resetando para 1)
    public void StartNextWeek(RunData run)
    {
        if (run == null)
        {
            Debug.LogError("[RunManager] Tentativa de iniciar próxima semana com RunData nulo!");
            return;
        }

        // 1. Atualiza Dados
        run.CurrentWeek++;
        run.CurrentDay = 1;

        Debug.Log($"[RunManager] >>> INICIANDO SEMANA {run.CurrentWeek} <<<");

        // 2. Atualiza Dado
        _currentPhase = RunPhase.Production;

        // ONDA 4: Reset semanal do Pattern Tracking - usa callback injetado
        _onWeeklyResetCallback?.Invoke();

        // 3. Persistência (CRÍTICO: Faltava o Save aqui ao ser chamado fora do AdvanceDay)
        _saveManager.SaveGame();

        // 4. Dispara Fatos (Fase primeiro para o FlowController reagir)
        OnProductionStarted?.Invoke(run);

        // 5. Eventos legados de tempo - usa dependência injetada
        _timeEvents.TriggerWeekChanged(run.CurrentWeek);
        _timeEvents.TriggerDayChanged(1);
    }

    public void EndRun(RunEndReason reason)
    {
        Debug.Log($"Run Finalizada: {reason}");
        _saveManager.Data.CurrentRun = null;
        _saveManager.SaveGame();

        // Usa dependências injetadas
        _gameStateProvider.SetState(GameState.GameOver);
        _timeEvents.TriggerRunEnded(reason);
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
        // Usa dependência injetada
        var s = _gameStateProvider.CurrentState;
        return s == GameState.Playing || s == GameState.Shopping;
    }
}