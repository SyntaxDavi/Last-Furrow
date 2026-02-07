using UnityEngine;
using LastFurrow.UI.Pause;
using LastFurrow.UI.RunEnd;
using LastFurrow.UI.MainMenu;

public class UIManager : MonoBehaviour
{
    [Header("Views Principais")]
    [SerializeField] private MainHudView _handContainer;
    [SerializeField] private PauseMenuView _pauseView;
    [SerializeField] private RunEndView _runEndView;
    [SerializeField] private ShopView _shopView;

    // Guarda se estávamos na loja ao pausar
    private bool _wasInShopBeforePause = false;

    private void Start()
    {
        if (AppCore.Instance != null && _shopView != null)
        {
            _shopView.Initialize(AppCore.Instance.ShopService);
            _shopView.OnExitRequested += HandleShopExitButton;
        }

        if (_pauseView) _pauseView.HideImmediate();
        if (_runEndView) _runEndView.HideImmediate();

        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.UI.RequestHUDMode(HUDMode.Production);
        }   
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.GameState.OnStateChanged += HandleGameStateChanged;
            AppCore.Instance.Events.Time.OnRunEnded += HandleRunEnded;
            AppCore.Instance.InputManager.OnBackInput += HandleBackInput;
            AppCore.Instance.InputManager.OnShopToggleInput += HandleShopToggleInput;
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

    private void HandleHUDModeChanged(HUDMode mode)
    {
        if (_runEndView.IsVisible) return;
        UpdateHUDMode(mode);
    }

    private void UpdateHUDMode(HUDMode mode)
    {
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
                if (_handContainer) _handContainer.Hide();
                if (_shopView) _shopView.Show();
                break;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (_runEndView.IsVisible) return;

        if (newState == GameState.Paused)
        {
            if (_pauseView) _pauseView.Show();
        }
        else
        {
            if (_pauseView) _pauseView.Hide();
            
            // FIX: Se saiu do pause e estava no Weekend com shop aberta, reabre
            if (_wasInShopBeforePause && newState == GameState.Shopping)
            {
                ReopenShopIfInWeekend();
            }
            _wasInShopBeforePause = false;
        }
    }

    private void HandleShopToggleInput()
    {
        AppCore.Instance.Events.UI.RequestToggleShop();
    }

    private void HandleShopExitButton()
    {
        if (AppCore.Instance == null) return;

        Debug.Log("[UIManager] Botão 'Trabalhar' clicado. Solicitando exit do Weekend via evento...");
        AppCore.Instance.Events.UI.RequestExitWeekend();
    }

    private void HandleBackInput()
    {
        if (_runEndView.IsVisible) return;

        if (_pauseView != null && _pauseView.IsVisible)
        {
            // Restaura estado anterior
            var previousState = AppCore.Instance.GameStateManager.PreviousState;
            
            // FIX: Se estava na loja (Weekend), lembra disso
            if (previousState == GameState.Shopping)
            {
                _wasInShopBeforePause = true;
            }
            
            if (previousState == GameState.MainMenu || previousState == GameState.Paused)
                previousState = GameState.Playing;
                
            AppCore.Instance.GameStateManager.SetState(previousState);
        }
        else if (_shopView != null && _shopView.IsVisible)
        {
            // FIX: ESC na loja agora abre pause, guardando que estava na loja
            _wasInShopBeforePause = true;
            AppCore.Instance.GameStateManager.SetState(GameState.Paused);
        }
        else
        {
            AppCore.Instance.GameStateManager.SetState(GameState.Paused);
        }
    }
    
    /// <summary>
    /// Reabre a loja se ainda estiver no Weekend e a sessão ainda existir.
    /// </summary>
    private void ReopenShopIfInWeekend()
    {
        var runManager = AppCore.Instance?.RunManager;
        var shopService = AppCore.Instance?.ShopService;
        
        if (runManager == null || shopService == null) return;
        
        // Verifica se ainda está no Weekend
        if (runManager.CurrentPhase == RunPhase.Weekend)
        {
            // Se a sessão da loja ainda existe, mostra a loja
            if (shopService.CurrentSession != null)
            {
                Debug.Log("[UIManager] Reabrindo loja após pause no Weekend");
                if (_shopView) _shopView.Show();
                AppCore.Instance.Events.UI.RequestHUDMode(HUDMode.Shopping);
            }
            else
            {
                // Sessão foi limpa, precisa reabrir a loja completamente
                Debug.Log("[UIManager] Sessão da loja expirou, reabrindo shop no Weekend");
                AppCore.Instance.Events.UI.RequestToggleShop();
            }
        }
    }

    private void HandleRunEnded(RunEndReason reason)
        {
            UpdateHUDMode(HUDMode.Hidden);
            if (_pauseView) _pauseView.Hide();
            if (_runEndView)
            {
                _runEndView.Setup(reason);
                _runEndView.Show();
            }
        }
    }