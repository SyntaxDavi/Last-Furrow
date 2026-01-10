using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Views Principais")]
    [SerializeField] private MainHudView _handContainer;
    [SerializeField] private PauseMenuView _pauseView;
    [SerializeField] private GameOverView _gameOverView;
    [SerializeField] private ShopView _shopView;

    private void Start()
    {
        if (AppCore.Instance != null && _shopView != null)
        {
            _shopView.Initialize(AppCore.Instance.ShopService);
            _shopView.OnExitRequested += HandleShopExitButton;
        }

        if (_pauseView) _pauseView.HideImmediate();
        if (_gameOverView) _gameOverView.HideImmediate();

        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.UI.RequestHUDMode(HUDMode.Production);
        }   
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            // Eventos de Sistema
            AppCore.Instance.Events.GameState.OnStateChanged += HandleGameStateChanged;
            AppCore.Instance.Events.Time.OnRunEnded += HandleRunEnded;

            // Eventos de Input
            AppCore.Instance.InputManager.OnBackInput += HandleBackInput;
            AppCore.Instance.InputManager.OnShopToggleInput += HandleShopToggleInput;

            // Flow Visual (A Fonte da Verdade)
            AppCore.Instance.Events.UI.OnHUDModeChanged += HandleHUDModeChanged;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.GameState.OnStateChanged -= HandleGameStateChanged;
            AppCore.Instance.Events.Time.OnRunEnded -= HandleRunEnded;
            AppCore.Instance.InputManager.OnBackInput -= HandleBackInput;
            AppCore.Instance.InputManager.OnShopToggleInput -= HandleShopToggleInput;
            AppCore.Instance.Events.UI.OnHUDModeChanged -= HandleHUDModeChanged;
        }

        if (_shopView != null) _shopView.OnExitRequested -= HandleShopExitButton;
    }

    // --- 1. VISUAL (Reage ao Modo) ---

    private void HandleHUDModeChanged(HUDMode mode)
    {
        if (_gameOverView.IsVisible) return;
        UpdateHUDMode(mode);
    }

    private void UpdateHUDMode(HUDMode mode)
    {
        // PONTO ÚNICO DE MUDANÇA VISUAL
        switch (mode)
        {
            case HUDMode.Hidden:
                if (_handContainer) _handContainer.Hide();
                if (_shopView) _shopView.Hide();
                break;

            case HUDMode.Production:
                if (_handContainer) _handContainer.Show();
                if (_shopView) _shopView.Hide();
                break;

            case HUDMode.Shopping:
                if (_handContainer) _handContainer.Hide(); // Esconde mão na loja
                if (_shopView) _shopView.Show();
                break;
        }
    }

    // --- 2. SISTEMA (Pause) ---

    private void HandleGameStateChanged(GameState newState)
    {
        if (_gameOverView.IsVisible) return;

        if (newState == GameState.Paused)
        {
            if (_pauseView) _pauseView.Show();
        }
        else
        {
            if (_pauseView) _pauseView.Hide();
        }
    }

    // --- 3. INPUT (Dispara Intenções) ---

    private void HandleShopToggleInput()
    {
        // MUDANÇA SÊNIOR:
        // Não perguntamos "Que dia é hoje?".
        // Apenas dizemos: "O jogador quer alternar a loja".
        // Quem decide se pode (e o que acontece) é o WeekendFlowController.

        AppCore.Instance.Events.UI.RequestToggleShop();
    }

    private void HandleShopExitButton()
    {
        // Intenção: Jogador quer ir para próxima semana
        var run = AppCore.Instance.SaveManager.Data.CurrentRun;
        AppCore.Instance.RunManager.StartNextWeek(run);
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
            // Opcional: ESC também tenta fechar loja via intenção
            AppCore.Instance.Events.UI.RequestToggleShop();
        }
        else
        {
            AppCore.Instance.GameStateManager.SetState(GameState.Paused);
        }
    }

    private void HandleRunEnded(RunEndReason reason)
    {
        UpdateHUDMode(HUDMode.Hidden);
        if (_pauseView) _pauseView.Hide();
        if (_gameOverView)
        {
            _gameOverView.Setup(reason);
            _gameOverView.Show();
        }
    }
}