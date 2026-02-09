using UnityEngine;

/// <summary>
/// Provedor de Pol�tica de M�sica do Menu Principal.
/// Reage ao estado global do jogo e define quando a m�sica de menu deve tocar.
/// Deve ser colocado como GameObject na cena MainMenu.
/// </summary>
public class MainMenuMusicController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AudioClip _mainMenuMusic;
    [SerializeField] private bool _debugLogs = false;

    private bool _isSubscribed = false;

    // --- CICLO DE VIDA ---

    private void Awake() => TrySubscribe();

    private void Start()
    {
        if (!_isSubscribed) TrySubscribe();
        
        // Sinaliza que estamos no MainMenu
        if (AppCore.Instance?.GameStateManager != null)
        {
            AppCore.Instance.GameStateManager.SetState(GameState.MainMenu);
        }
        
        EvaluateMusicState("Start");
    }

    private void OnEnable()
    {
        if (_isSubscribed) EvaluateMusicState("OnEnable");
    }

    private void OnDestroy()
    {
        // Para a m�sica ao sair da cena
        StopMusicOnExit();
        UnsubscribeEvents();
    }

    private void TrySubscribe()
    {
        if (AppCore.Instance == null) return;
        SubscribeEvents();
    }

    // --- EVENTOS (M�todos Nomeados para Unsubscribe correto) ---

    private void SubscribeEvents()
    {
        if (_isSubscribed || AppCore.Instance == null) return;
        var events = AppCore.Instance.Events;
        if (events?.GameState == null) return;

        events.GameState.OnStateChanged += HandleGameStateChanged;
        _isSubscribed = true;

        if (_debugLogs) Debug.Log("[MainMenuMusic] Events subscribed");
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed || AppCore.Instance == null) return;
        var events = AppCore.Instance.Events;
        if (events?.GameState != null)
        {
            events.GameState.OnStateChanged -= HandleGameStateChanged;
        }
        _isSubscribed = false;

        if (_debugLogs) Debug.Log("[MainMenuMusic] Events unsubscribed");
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (_debugLogs) Debug.Log($"[MainMenuMusic] State changed to {newState}");
        EvaluateMusicState($"StateChanged:{newState}");
    }

    // --- L�GICA DE POL�TICA ---

    /// <summary>
    /// Decide se a m�sica de menu deve estar ativa baseado no estado atual.
    /// </summary>
    private void EvaluateMusicState(string source)
    {
        if (AppCore.Instance == null || AppCore.Instance.AudioManager == null) return;

        var currentState = AppCore.Instance.GameStateManager?.CurrentState ?? GameState.Initialization;
        
        // M�sica do menu s� toca quando est� no estado MainMenu
        bool shouldPlay = currentState == GameState.MainMenu;

        if (_debugLogs) Debug.Log($"[MainMenuMusic] Evaluate ({source}): State={currentState}, ShouldPlay={shouldPlay}");

        AppCore.Instance.AudioManager.SetMusicContext(_mainMenuMusic, shouldPlay);
    }

    private void StopMusicOnExit()
    {
        if (AppCore.Instance == null || AppCore.Instance.AudioManager == null) return;

        if (_debugLogs) Debug.Log("[MainMenuMusic] Stopping music on exit");

        AppCore.Instance.AudioManager.SetMusicContext(_mainMenuMusic, shouldBePlaying: false);
    }
}