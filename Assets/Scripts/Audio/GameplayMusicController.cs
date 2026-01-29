using UnityEngine;

/// <summary>
/// Provedor de Política de Música de Gameplay.
/// Stateless: Apenas decide SE a música de gameplay deve existir com base no estado do jogo.
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

    private void OnDestroy() => UnsubscribeEvents();
    private void OnEnable()
    {
        if (_isSubscribed) EvaluateMusicState("OnEnable");
    }

    private void TrySubscribe()
    {
        if (AppCore.Instance == null) return;
        SubscribeEvents();
    }

    // --- EVENTOS ---

    private void SubscribeEvents()
    {
        if (_isSubscribed || AppCore.Instance == null) return;
        var events = AppCore.Instance.Events;
        if (events == null) return;

        events.GameState.OnStateChanged += (s) => EvaluateMusicState("StateChanged");
        events.Time.OnDayChanged += (d) => EvaluateMusicState("DayChanged");
        events.Time.OnWeekendStarted += () => EvaluateMusicState("WeekendStarted");
        events.Time.OnRunStarted += () => EvaluateMusicState("RunStarted", forceRestart: true);
        events.Time.OnRunEnded += (r) => EvaluateMusicState("RunEnded");
        
        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed || AppCore.Instance == null) return;
        var events = AppCore.Instance.Events;
        if (events == null) return;

        // Nota: O uso de lambdas anônimas no Subscribe dificulta o Unsubscribe direto por referência,
        // mas como este objeto persiste (ou morre com a cena), o Unsubscribe de AppCore limparia tudo.
        // Se precisar de precisão, usaríamos métodos nomeados.
        _isSubscribed = false;
    }

    // --- CORE LOGIC (POLÍTICA PURA) ---

    /// <summary>
    /// Avalia a condição de gameplay e delega para o AudioManager (SSoT).
    /// </summary>
    private void EvaluateMusicState(string context, bool forceRestart = false)
    {
        if (AppCore.Instance == null || AppCore.Instance.AudioManager == null) return;

        bool shouldPlay = CalculateShouldPlayPolicy();

        if (_debugLogs) Debug.Log($"[MusicPolicy] ({context}) -> ShouldPlay: {shouldPlay}, Restart: {forceRestart}");

        // Delegação total: O Controller não sabe se está pausado, se já está tocando, etc.
        // Ele apenas diz "Eu quero essa música tocando (ou não)".
        AppCore.Instance.AudioManager.SetMusicContext(_gameplayMusic, shouldPlay, forceRestart);
    }

    private bool CalculateShouldPlayPolicy()
    {
        var state = AppCore.Instance.GameStateManager.CurrentState;
        
        // Política 1: Só toca em Gameplay (Playing ou Paused)
        if (state != GameState.Playing && state != GameState.Paused) return false;

        var runData = AppCore.Instance.SaveManager?.Data?.CurrentRun;
        if (runData == null) return false;

        // Política 2: Ciclo Semanal - Dias 1 a 5 (Produção)
        int dayInWeek = (runData.CurrentDay - 1) % 7;
        return dayInWeek < 5;
    }
}
