using UnityEngine;

public class GameplayBootstrapper : MonoBehaviour
{
    [Header("Controllers da Cena")]
    [SerializeField] private GameCameraController _gameCamera;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PlayerInteraction _playerInteraction;
    [SerializeField] private HandManager _handManager;

    private IGridService _gridService;

    private void Awake()
    {
        if (AppCore.Instance == null) return;

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
           AppCore.Instance.GameStateManager,
           AppCore.Instance.GridConfiguration, // Injeção da Config
           AppCore.Instance.PatternWeightConfig 
       );

        // 2. Registra no Global (para DailyResolution, CheatManager, etc)
        AppCore.Instance.RegisterGridService(_gridService);
        
        // 2.5 ONDA 4: Inicializa Pattern Tracking com RunData
        AppCore.Instance.InitializePatternTracking(runData);

        // 3. Configura eventos locais
        _gridService.OnDataDirty += () => AppCore.Instance.SaveManager.SaveGame();

        // 4. Injeta nos consumidores da cena
        // GridManager agora é inicializado por GridVisualBootstrapper

        if (_handManager != null)
        {
            _handManager.Configure(runData, library);
        }

        // 5. ? Configura Câmera Dinâmica (SOLID - Usa Strategy Pattern)
        // NOVO: Não depende mais de GridManager.GetGridWorldSize()
        // Câmera recebe dados puros e calcula bounds usando Strategy
        if (_gameCamera != null && _gridManager != null)
        {
            _gameCamera.ConfigureFromGrid(
                AppCore.Instance.GridConfiguration,
                _gridManager.Spacing
            );
            AppCore.Instance.InputManager.SetCamera(_gameCamera.GetComponent<Camera>());
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