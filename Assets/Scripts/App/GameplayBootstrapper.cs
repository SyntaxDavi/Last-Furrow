using UnityEngine;
using LastFurrow.Visual.Camera;

/// <summary>
/// Bootstrapper da cena de gameplay.
/// Inicializa sistemas específicos da cena e notifica o RunManager.
/// </summary>
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
        if (AppCore.Instance == null)
        {
            Debug.LogError("[Bootstrapper] AppCore.Instance é null!");
            return;
        }

        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        if (runData == null)
        {
            Debug.LogWarning("[Bootstrapper] Sem RunData ativo. Criando nova Run.");
            AppCore.Instance.RunManager.StartNewRun();       
            runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        }

        InitializeGameplaySystems(runData);
    }

    private void InitializeGameplaySystems(RunData runData)  
    {
        Debug.Log($"[Bootstrapper] Inicializando... Dia: {runData.CurrentDay}, Semana: {runData.CurrentWeek}");

        var core = AppCore.Instance;
        
        // 1. Input
        _playerInteraction.Initialize(core.InputManager);

        // 2. Aleatoriedade Determinística
        CardInteractionBootstrapper.ConfigureForRun(runData);

        // 3. Grid Service
        _gridService = new GridService(
           runData,
           core.GameLibrary,
           core.GameStateManager,
           core.GridConfiguration,
           core.PatternWeightConfig
       );

        core.RegisterGridService(_gridService);  
        core.InitializePatternTracking(runData); 

        // 4. Eventos do Grid
        _gridService.OnGridChanged += HandleGridChanged;

        // 5. Hand Manager
        if (_handManager != null)
        {
            _handManager.Configure(runData, core.GameLibrary);        
        }

        // 6. Câmera
        if (_gameCamera != null && _gridManager != null)     
        {
            _gameCamera.ConfigureFromGrid(core.GridConfiguration, _gridManager.Spacing);
            core.InputManager.SetCamera(_gameCamera.GetComponent<Camera>());
        }

        // 7. Estado base (ANTES de notificar RunManager)
        core.GameStateManager.SetState(GameState.Playing);
        
        // 8. CRÍTICO: Notifica RunManager para emitir eventos de fase.
        // Isso abre a loja se estivermos no Weekend.
        core.RunManager.NotifyGameplaySceneLoaded();
        
        Debug.Log($"[Bootstrapper] ✓ Fase: {core.RunManager.CurrentPhase}");
    }

    private void HandleGridChanged(GridChangeEvent evt)
    {
        AppCore.Instance?.Events.Grid.TriggerGridChanged(evt);
        
        if (evt.Impact.RequiresSave)
        {
            AppCore.Instance?.SaveManager.SaveGame();
        }
    }

    private void OnDestroy()
    {
        if (_gridService != null)
        {
            _gridService.OnGridChanged -= HandleGridChanged;
        }
    }
}
