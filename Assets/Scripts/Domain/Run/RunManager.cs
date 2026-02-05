using System;
using UnityEngine;

/// <summary>
/// RunManager com Scene-Aware Activation.
/// 
/// ARQUITETURA SIMPLIFICADA:
/// - Initialize(): Conecta dependências, prepara estado interno
/// - NotifyGameplaySceneLoaded(): Chamado pelo GameplayBootstrapper, emite eventos de fase
/// 
/// Isso unifica boot e continue em um único fluxo previsível.
/// </summary>
public class RunManager : MonoBehaviour, IRunManager
{
    // Dependências injetadas
    private ISaveManager _saveManager;
    private GridConfiguration _gridConfiguration;
    private TimeEvents _timeEvents;
    private IGameStateProvider _gameStateProvider;
    private IRunCalendar _calendar;
    private ProgressionSettingsSO _progressionSettings;

    // Estado
    private RunPhase _currentPhase;
    private bool _isInitialized;

    public RunPhase CurrentPhase => _currentPhase;
    public bool IsRunActive => _saveManager?.Data?.CurrentRun != null;

    // --- EVENTOS DE FLUXO ---
    public event Action<RunData> OnWeekendStarted;
    public event Action<RunData> OnProductionStarted;

    // --- INICIALIZAÇÃO ---
    
    public void Initialize(
        ISaveManager saveManager,
        GridConfiguration gridConfiguration,
        TimeEvents timeEvents,
        IGameStateProvider gameStateProvider,
        IRunCalendar calendar,
        ProgressionSettingsSO progressionSettings = null)
    {
        _saveManager = saveManager;
        _gridConfiguration = gridConfiguration;
        _timeEvents = timeEvents;
        _gameStateProvider = gameStateProvider;
        _calendar = calendar;
        _progressionSettings = progressionSettings;

        // Prepara fase interna (sem emitir eventos)
        if (IsRunActive)
        {
            _currentPhase = _calendar.GetPhaseForDay(_saveManager.Data.CurrentRun.CurrentDay);
            Debug.Log($"[RunManager] Estado restaurado: {_currentPhase} (Dia {_saveManager.Data.CurrentRun.CurrentDay})");
        }

        _isInitialized = true;
        Debug.Log("[RunManager] ✓ Inicializado");
    }

    // --- SCENE-AWARE ACTIVATION ---
    
    /// <summary>
    /// Chamado pelo GameplayBootstrapper quando a cena de gameplay carrega.
    /// Emite os eventos apropriados para a fase atual.
    /// 
    /// UNIFICADO: Funciona tanto no boot quanto no continue.
    /// </summary>
    public void NotifyGameplaySceneLoaded()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[RunManager] NotifyGameplaySceneLoaded chamado antes de Initialize!");
            return;
        }

        var runData = _saveManager?.Data?.CurrentRun;
        if (runData == null)
        {
            Debug.Log("[RunManager] NotifyGameplaySceneLoaded: Sem RunData ativo");
            return;
        }

        // Recalcula fase (pode ter mudado desde Initialize)
        _currentPhase = _calendar.GetPhaseForDay(runData.CurrentDay);
        
        Debug.Log($"[RunManager] GameplayScene carregada. Fase: {_currentPhase}, Dia: {runData.CurrentDay}");

        // Emite eventos para listeners
        EmitCurrentPhaseEvents(runData);
    }

    // --- EMISSÃO DE EVENTOS (UNIFICADA) ---

    private void EmitCurrentPhaseEvents(RunData run)
    {
        switch (_currentPhase)
        {
            case RunPhase.Weekend:
                Debug.Log("[RunManager] → OnWeekendStarted");
                OnWeekendStarted?.Invoke(run);
                _timeEvents.TriggerWeekendStarted();
                break;
                
            case RunPhase.Production:
                Debug.Log("[RunManager] → OnProductionStarted");
                OnProductionStarted?.Invoke(run);
                _timeEvents.TriggerDayChanged(run.CurrentDay);
                break;
        }
    }

    // --- CONTROLE DE FLUXO ---

    public void StartNewRun()
    {
        var newRun = RunData.CreateNewRun(_gridConfiguration);

        if (_progressionSettings != null)
        {
            newRun.WeeklyGoalTarget = _progressionSettings.GetGoalForWeek(1);
        }

        _saveManager.Data.CurrentRun = newRun;
        _saveManager.SaveGame();

        ChangePhase(RunPhase.Production, newRun, isNewRun: true);
    }

    public void AdvanceDay()
    {
        if (!CanAdvanceDay()) return;

        var run = _saveManager.Data.CurrentRun;
        run.CurrentDay++;

        ProcessDayTransition(run);

        _saveManager.SaveGame();
    }

    public void StartNextWeek(RunData run)
    {
        if (run == null)
        {
            Debug.LogError("[RunManager] StartNextWeek: RunData nulo!");
            return;
        }

        run.CurrentWeek++;
        run.CurrentDay = 1;

        Debug.Log($"[RunManager] >>> SEMANA {run.CurrentWeek} <<<");

        _saveManager.SaveGame();
        ChangePhase(RunPhase.Production, run, isNewWeek: true);
    }

    public void EndRun(RunEndReason reason)
    {
        Debug.Log($"[RunManager] Run finalizada: {reason}");
        _saveManager.Data.CurrentRun = null;
        _saveManager.SaveGame();

        _gameStateProvider.SetState(GameState.GameOver);
        _timeEvents.TriggerRunEnded(reason);
    }

    // --- TRANSIÇÕES INTERNAS ---

    private void ProcessDayTransition(RunData run)
    {
        if (_calendar.IsProductionDay(run.CurrentDay))
        {
            _timeEvents.TriggerDayChanged(run.CurrentDay);
            
            if (run.CurrentDay == _calendar.ProductionDays)
            {
                Debug.Log(">>> Último dia de produção! <<<");
            }
        }
        else if (_calendar.IsWeekendStart(run.CurrentDay))
        {
            ChangePhase(RunPhase.Weekend, run);
        }
        else if (_calendar.IsPastCycle(run.CurrentDay))
        {
            Debug.LogWarning("[RunManager] Dia passou do ciclo.");
            StartNextWeek(run);
        }
    }

    private void ChangePhase(RunPhase newPhase, RunData run, bool isNewRun = false, bool isNewWeek = false)
    {
        var previousPhase = _currentPhase;
        _currentPhase = newPhase;

        Debug.Log($"[RunManager] {previousPhase} → {newPhase} (S{run.CurrentWeek}D{run.CurrentDay})");

        switch (newPhase)
        {
            case RunPhase.Production:
                OnProductionStarted?.Invoke(run);
                if (isNewRun) _timeEvents.TriggerRunStarted();
                if (isNewWeek)
                {
                    _timeEvents.TriggerWeekStarted(run.CurrentWeek);
                    _timeEvents.TriggerWeekChanged(run.CurrentWeek);
                }
                _timeEvents.TriggerDayChanged(run.CurrentDay);
                break;

            case RunPhase.Weekend:
                OnWeekendStarted?.Invoke(run);
                _timeEvents.TriggerWeekendStarted();
                break;
        }
    }

    private bool CanAdvanceDay()
    {
        if (!IsRunActive) return false;
        var s = _gameStateProvider.CurrentState;
        return s == GameState.Playing || s == GameState.Shopping;
    }
}