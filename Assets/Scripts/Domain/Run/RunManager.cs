using UnityEngine;
using System;

/// <summary>
/// RunManager Refatorado - Responsabilidade Única.
/// 
/// APENAS: Move o tempo da Run e anuncia mudanças de fase.
/// 
/// NÃO CONHECE:
/// - Sistemas que precisam de reset semanal (Pattern Tracking)
/// - Detalhes de UI ou GameState (delega para dependências)
/// 
/// Arquitetura:
/// - Usa IRunCalendar para lógica temporal (configurável)
/// - Centraliza mudanças de fase em ChangePhase() (nunca muda em silêncio)
/// - Dispara eventos de domínio que outros sistemas escutam
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

    // Estado privado, leitura pública
    private RunPhase _currentPhase;
    public RunPhase CurrentPhase => _currentPhase;

    // --- EVENTOS DE FLUXO (O FlowController escuta estes) ---
    public event Action<RunData> OnWeekendStarted;
    public event Action<RunData> OnProductionStarted;

    // --- INICIALIZAÇÃO ---
    /// <summary>
    /// Inicializa o RunManager com todas as dependências necessárias.
    /// SOLID: Injeção de Dependência Explícita - sem callbacks ocultos.
    /// </summary>
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

        // Restaura o estado correto ao carregar o jogo
        if (IsRunActive)
        {
            RestorePhaseFromSave(_saveManager.Data.CurrentRun);
        }
        
        Debug.Log("[RunManager] ✓ Inicializado com calendário injetado");
    }

    public bool IsRunActive => _saveManager?.Data?.CurrentRun != null;

    // --- CONTROLE DE DADOS ---

    public void StartNewRun()
    {
        // 1. Criação Pura de Dados (Domain)
        RunData newRun = RunData.CreateNewRun(_gridConfiguration);

        // Ajusta meta inicial se settings existirem
        if (_progressionSettings != null)
        {
            newRun.WeeklyGoalTarget = _progressionSettings.GetGoalForWeek(1);
        }

        // 2. Persistência
        _saveManager.Data.CurrentRun = newRun;
        _saveManager.SaveGame();

        // 3. Muda para fase Production (centralizado)
        ChangePhase(RunPhase.Production, newRun, isNewRun: true);
    }

    public void AdvanceDay()
    {
        if (!CanAdvanceDay()) return;

        var run = _saveManager.Data.CurrentRun;
        run.CurrentDay++;

        // Decide a fase baseada no novo dia
        ProcessDayTransition(run);

        _saveManager.SaveGame();
    }

    // --- TRANSIÇÕES DE FASE (CENTRALIZADAS) ---

    private void ProcessDayTransition(RunData run)
    {
        // Caso 1: Dias de Trabalho (1 a N)
        if (_calendar.IsProductionDay(run.CurrentDay))
        {
            HandleProductionDay(run);
        }
        // Caso 2: Chegou o Weekend
        else if (_calendar.IsWeekendStart(run.CurrentDay))
        {
            ChangePhase(RunPhase.Weekend, run);
        }
        // Caso 3: Passou do Ciclo (não deveria acontecer via AdvanceDay normal)
        else if (_calendar.IsPastCycle(run.CurrentDay))
        {
            Debug.LogWarning("[RunManager] Dia passou do ciclo via AdvanceDay. Isso não deveria acontecer.");
            StartNextWeek(run);
        }
    }

    private void HandleProductionDay(RunData run)
    {
        // Não muda fase, apenas notifica mudança de dia
        _timeEvents.TriggerDayChanged(run.CurrentDay);

        if (run.CurrentDay == _calendar.ProductionDays)
        {
            Debug.Log(">>> ALERTA: Último dia de colheita da semana! <<<");
        }
    }

    /// <summary>
    /// Chamado pelo StartNextWeekStep no pipeline de exit do Weekend.
    /// </summary>
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

        // 2. Persistência
        _saveManager.SaveGame();

        // 3. Muda para fase Production (centralizado)
        ChangePhase(RunPhase.Production, run, isNewWeek: true);
    }

    public void EndRun(RunEndReason reason)
    {
        Debug.Log($"Run Finalizada: {reason}");
        _saveManager.Data.CurrentRun = null;
        _saveManager.SaveGame();

        _gameStateProvider.SetState(GameState.GameOver);
        _timeEvents.TriggerRunEnded(reason);
    }

    // --- MUDANÇA DE FASE CENTRALIZADA ---

    /// <summary>
    /// Ponto único de mudança de fase.
    /// NUNCA muda _currentPhase em outro lugar.
    /// Garante que estado e eventos estejam sempre sincronizados.
    /// </summary>
    private void ChangePhase(RunPhase newPhase, RunData run, bool isNewRun = false, bool isNewWeek = false)
    {
        RunPhase previousPhase = _currentPhase;
        _currentPhase = newPhase;

        Debug.Log($"[RunManager] Fase: {previousPhase} → {newPhase} (Semana {run.CurrentWeek}, Dia {run.CurrentDay})");

        switch (newPhase)
        {
            case RunPhase.Production:
                EmitProductionEvents(run, isNewRun, isNewWeek);
                break;

            case RunPhase.Weekend:
                EmitWeekendEvents(run);
                break;
        }
    }

    private void EmitProductionEvents(RunData run, bool isNewRun, bool isNewWeek)
    {
        // Evento de domínio principal
        OnProductionStarted?.Invoke(run);

        // Eventos de tempo
        if (isNewRun)
        {
            _timeEvents.TriggerRunStarted();
        }

        if (isNewWeek)
        {
            // ARQUITETURA: PatternTracking e outros sistemas escutam OnWeekStarted
            // Ao invés de callback oculto, declaramos o fato de domínio
            _timeEvents.TriggerWeekStarted(run.CurrentWeek);
            _timeEvents.TriggerWeekChanged(run.CurrentWeek);
        }

        _timeEvents.TriggerDayChanged(run.CurrentDay);
    }

    private void EmitWeekendEvents(RunData run)
    {
        // Evento de domínio principal
        OnWeekendStarted?.Invoke(run);

        // Evento legado de tempo
        _timeEvents.TriggerWeekendStarted();
    }

    // --- RESTAURAÇÃO DE ESTADO (LOAD) ---

    /// <summary>
    /// Restaura fase após carregar save.
    /// Não emite eventos pois o jogo ainda está inicializando.
    /// Sistemas devem usar seu próprio estado salvo.
    /// </summary>
    private void RestorePhaseFromSave(RunData run)
    {
        if (run == null) return;
        _currentPhase = _calendar.GetPhaseForDay(run.CurrentDay);
        Debug.Log($"[RunManager] Fase restaurada do save: {_currentPhase}");
    }

    // --- HELPERS ---

    private bool CanAdvanceDay()
    {
        if (!IsRunActive) return false;
        var s = _gameStateProvider.CurrentState;
        return s == GameState.Playing || s == GameState.Shopping;
    }
}