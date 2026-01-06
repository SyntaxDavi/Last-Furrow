using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Views Principais")]
    [SerializeField] private MainHudView _hudView;
    [SerializeField] private PauseMenuView _pauseView; // Crie este script simples herdando de UIView depois
    [SerializeField] private GameOverView _gameOverView;
    [SerializeField] private ShopView _shopView;

    private void Start()
    {
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
        }

        if (_shopView != null)
        {
            _shopView.OnExitRequested += HandleShopExit;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnRunEnded -= HandleRunEnded;
            AppCore.Instance.Events.GameState.OnStateChanged -= HandleStateChanged;
            AppCore.Instance.InputManager.OnBackInput -= HandleBackInput;
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
        // Se estiver em Game Over, ignora mudanças de estado padrão para evitar reabrir HUD
        if (_gameOverView.IsVisible) return;

        switch (newState)
        {
            case GameState.Playing:
                _hudView.Show();
                if (_pauseView) _pauseView.Hide();
                if (_shopView) _shopView.Hide();
                break;
            case GameState.Shopping:
                _hudView.Hide();
                if (_shopView) _shopView.Show();
                break;
            case GameState.Paused:
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