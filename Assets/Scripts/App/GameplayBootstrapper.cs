using UnityEngine;

public class GameplayBootstrapper : MonoBehaviour
{
    [Header("Controllers da Cena")]
    [SerializeField] private GameCameraController _gameCamera;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PlayerInteraction _playerInteraction;
    [SerializeField] private GridFeedbackController _feedbackController;
    [SerializeField] private HandManager _handManager;

    [Header("Level Data")]
    [SerializeField] private float _levelGridWidth = 5f;
    [SerializeField] private float _levelGridHeight = 7f;

    private IGridService _gridService;

    private void Awake()
    {
        if (AppCore.Instance == null) return;

        if (_gameCamera != null)
        {
            _gameCamera.Configure(_levelGridWidth, _levelGridHeight);
            AppCore.Instance.InputManager.SetCamera(_gameCamera.GetComponent<Camera>());
        }

        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        if (runData == null)
        {
            Debug.LogWarning("[Bootstrapper] Sem RunData ativo. Criando Run de Teste.");
            AppCore.Instance.RunManager.StartNewRun();
            runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        }

        InitializeGameplaySystems(runData);
    }

    private void InitializeGameplaySystems(RunData runData)
    {
        Debug.Log("[Bootstrapper] Inicializando sistemas de gameplay...");

        var library = AppCore.Instance.GameLibrary;
        _playerInteraction.Initialize(AppCore.Instance.InputManager);

        // --- CRIAÇÃO CENTRALIZADA ---
        // 1. Cria a instância
        _gridService = new GridService(
           runData,
           library,
           AppCore.Instance.GameStateManager
       );

        // 2. Registra no Global (para DailyResolution, CheatManager, etc)
        AppCore.Instance.RegisterGridService(_gridService);

        // 3. Configura eventos locais
        _gridService.OnDataDirty += () => AppCore.Instance.SaveManager.SaveGame();

        // 4. Injeta nos consumidores da cena
        if (_gridManager != null)
        {
            _gridManager.Configure(_gridService, library);
        }

        if (_feedbackController != null && _gridManager != null)
        {
            _feedbackController.Configure(_gridManager);
        }

        if (_handManager != null)
        {
            _handManager.Configure(runData, library);
        }

        AppCore.Instance.Events.Time.OnDayChanged += HandleDayChanged;
        AppCore.Instance.GameStateManager.SetState(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnDayChanged -= HandleDayChanged;
            AppCore.Instance.UnregisterGridService();
        }
    }

    private void HandleDayChanged(int day)
    {
        if (_gridManager != null) _gridManager.RefreshAllSlots();
    }
}