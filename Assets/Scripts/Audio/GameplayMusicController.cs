using UnityEngine;

/// <summary>
/// Provedor de Política de Música de Gameplay.
/// Stateless: Apenas decide SE a música de gameplay deve existir com base no estado do jogo.
/// Usa métodos nomeados para garantir Unsubscribe correto ao destruir a cena.
/// </summary>
public class GameplayMusicController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AudioClip _gameplayMusic;
    [SerializeField] private bool _debugLogs = false;

    private bool _isSubscribed = false;

    // --- CICLO DE VIDA ---

    private void Awake() => TrySubscribe();

    private void Start()
    {
        if (!_isSubscribed) TrySubscribe();
        EvaluateMusicState("Start");
    }

    private void OnEnable()
    {
        if (_isSubscribed) EvaluateMusicState("OnEnable");
    }

    private void OnDestroy()
    {
        // Garante que a música para ao sair da cena de gameplay
        StopMusicOnExit();
        UnsubscribeEvents();
    }

    private void TrySubscribe()
    {
        if (AppCore.Instance == null) return;
        SubscribeEvents();
    }

    // --- EVENTOS (Métodos Nomeados para Unsubscribe correto) ---

    private void SubscribeEvents()
    {
        if (_isSubscribed || AppCore.Instance == null) return;
        var events = AppCore.Instance.Events;
        if (events == null) return;

        events.GameState.OnStateChanged += HandleStateChanged;
        events.Time.OnDayChanged += HandleDayChanged;
        events.Time.OnWeekendStarted += HandleWeekendStarted;
        events.Time.OnRunStarted += HandleRunStarted;
        events.Time.OnRunEnded += HandleRunEnded;

        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed || AppCore.Instance == null) return;
        var events = AppCore.Instance.Events;
        if (events == null) return;

        events.GameState.OnStateChanged -= HandleStateChanged;
        events.Time.OnDayChanged -= HandleDayChanged;
        events.Time.OnWeekendStarted -= HandleWeekendStarted;
        events.Time.OnRunStarted -= HandleRunStarted;
        events.Time.OnRunEnded -= HandleRunEnded;

        _isSubscribed = false;
    }

    private void HandleStateChanged(GameState _) => EvaluateMusicState("StateChanged");
    private void HandleDayChanged(int _) => EvaluateMusicState("DayChanged");
    private void HandleWeekendStarted() => EvaluateMusicState("WeekendStarted");
    private void HandleRunStarted() => EvaluateMusicState("RunStarted", forceRestart: true);
    private void HandleRunEnded(RunEndReason _) => EvaluateMusicState("RunEnded");

    // --- CORE LOGIC (POLÍTICA PURA) ---

    /// <summary>
    /// Avalia a condição de gameplay e delega para o AudioManager (SSoT).
    /// </summary>
    private void EvaluateMusicState(string context, bool forceRestart = false)
    {
        if (AppCore.Instance == null || AppCore.Instance.AudioManager == null) return;

        bool shouldPlay = CalculateShouldPlayPolicy();

        if (_debugLogs) Debug.Log($"[MusicPolicy] ({context}) -> ShouldPlay: {shouldPlay}, Restart: {forceRestart}");

        AppCore.Instance.AudioManager.SetMusicContext(_gameplayMusic, shouldPlay, forceRestart);
    }

    private bool CalculateShouldPlayPolicy()
    {
        var state = AppCore.Instance.GameStateManager.CurrentState;

        // Política 1: Só toca em estados de gameplay ativo
        if (state != GameState.Playing && state != GameState.Paused && state != GameState.Analyzing)
            return false;

        var runData = AppCore.Instance.SaveManager?.Data?.CurrentRun;
        if (runData == null) return false;

        // Política 2: Ciclo Semanal - Dias 1 a 5 (Produção)
        int dayInWeek = (runData.CurrentDay - 1) % 7;
        return dayInWeek < 5;
    }

    /// <summary>
    /// Chamado no OnDestroy para garantir fade-out ao sair da cena.
    /// </summary>
    private void StopMusicOnExit()
    {
        if (AppCore.Instance == null || AppCore.Instance.AudioManager == null) return;
        AppCore.Instance.AudioManager.SetMusicContext(_gameplayMusic, false);
    }
}
