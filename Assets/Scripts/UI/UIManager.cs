using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Views Principais")]
    [SerializeField] private MainHudView _hudView;
    [SerializeField] private PauseMenuView _pauseView;
    [SerializeField] private GameOverView _gameOverView;
    [SerializeField] private ShopView _shopView;

    private void Start()
    {
        if (AppCore.Instance != null && _shopView != null)
        {
            _shopView.Initialize(AppCore.Instance.ShopService);
            _shopView.OnExitRequested += HandleShopExit;
        }

        // Estado Inicial garantido
        _hudView.Show();
        if (_pauseView) _pauseView.HideImmediate();
        if (_gameOverView) _gameOverView.HideImmediate();
        if (_shopView) _shopView.HideImmediate();
    }

    private void OnEnable()
    {   
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnRunEnded += HandleRunEnded;
            AppCore.Instance.Events.GameState.OnStateChanged += HandleStateChanged;
            AppCore.Instance.InputManager.OnBackInput += HandleBackInput;
            AppCore.Instance.InputManager.OnShopToggleInput += HandleShopToggle;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnRunEnded -= HandleRunEnded;
            AppCore.Instance.Events.GameState.OnStateChanged -= HandleStateChanged;
            AppCore.Instance.InputManager.OnBackInput -= HandleBackInput;
            AppCore.Instance.InputManager.OnShopToggleInput -= HandleShopToggle;
        }

        if (_shopView != null)
        {
            _shopView.OnExitRequested -= HandleShopExit;
        }
    }
    private void HandleShopExit()
    {
        // Lógica de Fluxo: Sair da loja significa avançar a semana
        if (AppCore.Instance != null)
        {
            var run = AppCore.Instance.SaveManager.Data.CurrentRun;
            AppCore.Instance.RunManager.StartNextWeek(run);
        }
    }
    private void HandleStateChanged(GameState newState)
    {
        if (_gameOverView.IsVisible) return;

        switch (newState)
        {
            case GameState.Playing:
                if (_hudView) _hudView.Show();
                if (_pauseView) _pauseView.Hide();
                if (_shopView) _shopView.Hide();
                break;
            case GameState.Shopping:
                if (_hudView) _hudView.Show();
                if (_shopView) _shopView.Show();
                break;
            case GameState.Paused:
                _hudView.Hide();
                if (_pauseView) _pauseView.Show();
                break;
        }
    }

    private void HandleRunEnded(RunEndReason reason)
    {
        // Força fechamento de tudo
        _hudView.Hide();
        if (_pauseView) _pauseView.Hide();
        if (_shopView) _shopView.Hide();

        // Abre Game Over
        _gameOverView.Setup(reason);
        _gameOverView.Show();
    }
    private void HandleShopToggle()
    {
        // Só permite alternar se estivermos na FASE de fim de semana
        if (AppCore.Instance.RunManager.CurrentPhase != RunPhase.Weekend) return;

        var stateManager = AppCore.Instance.GameStateManager;
        var currentState = stateManager.CurrentState;

        if (currentState == GameState.Shopping)
        {
            // FECHAR LOJA: Volta para Playing (para ver o grid)
            // A ShopView vai sumir automaticamente por causa do StateObserver
            stateManager.SetState(GameState.Playing);
        }
        else if (currentState == GameState.Playing)
        {
            // ABRIR LOJA: Volta para Shopping
            stateManager.SetState(GameState.Shopping);
        }
    }
    private void HandleBackInput()
    {
        if (_gameOverView.IsVisible) return;

        if (_pauseView != null && _pauseView.IsVisible)
        {
            AppCore.Instance.GameStateManager.SetState(GameState.Playing);
        }
        else if (_shopView != null && _shopView.IsVisible)
        {
            // Opcional: Fechar shop com ESC
        }
        else
        {
            AppCore.Instance.GameStateManager.SetState(GameState.Paused);
        }
    }
}