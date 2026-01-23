using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// AppCore Modularizado - Ponto de entrada global do jogo.
/// SOLID: Atua agora como o Orquestrador/Bootstrapper Global, delegando 
/// a gestão de instâncias para o ServiceRegistry e a inicialização para Módulos.
/// </summary>
public class AppCore : MonoBehaviour
{
    public static AppCore Instance { get; private set; }

    // Registro Central de Serviços (SOLID: Service Locator / Registry)
    public ServiceRegistry Services { get; private set; }

    [Header("Data & Configuration")]
    [SerializeField] private GameDatabaseSO _gameDatabase;
    [SerializeField] private GridConfiguration _gridConfiguration;
    [SerializeField] private PatternWeightConfig _patternWeightConfig;
    [SerializeField] private ProgressionSettingsSO _progressionSettings;
    [SerializeField] private PatternLibrary _patternLibrary;

    [Header("Shop Configuration")]
    [SerializeField] private ShopProfileSO _defaultShop;
    [SerializeField] private List<ShopProfileSO> _specialShops;

    [Header("Global MonoBehaviours (Scene References)")]
    public SaveManager SaveManager;
    public RunManager RunManager;
    public GameStateManager GameStateManager;
    public TimeManager TimeManager;
    public InputManager InputManager;
    public AudioManager AudioManager;
    public DailyResolutionSystem DailyResolutionSystem;
    public WeekendFlowController WeekendFlowController;

    [Header("Scene Config")]
    [SerializeField] private string _firstSceneName = "Game";

    // --- PROPRIEDADES DE COMPATIBILIDADE (Não quebram o código atual) ---
    public IGameLibrary GameLibrary => Services?.GameLibrary;
    public GameEvents Events => Services?.Events;
    public IEconomyService EconomyService => Services?.Economy;
    public DailyHandSystem DailyHandSystem => Services?.DailyHand;
    public WeeklyGoalSystem WeeklyGoalSystem => Services?.WeeklyGoal;
    public ShopService ShopService => Services?.Shop;
    public PatternDetector PatternDetector => Services?.PatternDetector;
    public PatternScoreCalculator PatternCalculator => Services?.PatternCalculator;
    public PatternTrackingService PatternTracking => Services?.PatternTracking;
    public IGridService GridService => Services?.GridConfig != null ? _gridService : null; // Mantido via registro manual para cenas
    
    public GridConfiguration GridConfiguration => _gridConfiguration;
    public PatternWeightConfig PatternWeightConfig => _patternWeightConfig;

    private IGridService _gridService;

    // --- CICLO DE VIDA ---

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeModularArchitecture();
    }

    private void InitializeModularArchitecture()
    {
        Debug.Log("[AppCore] 🏗️ Iniciando Arquitetura Modular...");

        // 1. Setup do Registro
        Services = new ServiceRegistry(this);
        var events = new GameEvents(); // Criado cedo pois módulos dependem dele

        // 2. Módulo Core (Sistemas Base e Infra)
        var coreModule = new CoreModule(Services, this);
        coreModule.Initialize();

        // 3. Módulo Domínio (Regras de Negócio e Serviços Puros)
        var domainModule = new DomainModule(Services, this, _progressionSettings);
        domainModule.Initialize();

        // 4. Módulo Gameplay (Sistemas Específicos)
        var patternModule = new PatternModule(Services, this, _patternLibrary);
        patternModule.Initialize();

        // 5. Injeções de Dependência Complexas (Cross-Module)
        InitializeLegacyCrossInjections();

        // 6. Finalização
        InputManager.OnAnyInputDetected += () => Events.Player.TriggerAnyInput();
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Debug.Log("[AppCore] ✅ Arquitetura Modular pronta. Carregando cena inicial...");
        SceneManager.LoadScene(_firstSceneName);
    }

    private void InitializeLegacyCrossInjections()
    {
        // 1. CardInteractionBootstrapper (Ainda usa estático, refatorar depois)
        CardInteractionBootstrapper.Initialize(
            RunManager,
            SaveManager,
            EconomyService,
            GameLibrary,
            Events.Player,
            Events,
            null
        );

        // 2. Weekend Flow
        if (WeekendFlowController != null)
        {
            var weekendBuilder = new DefaultWeekendFlowBuilder(
                new WeekendStateFlow(GameStateManager),
                new WeekendUIFlow(Events.UI),
                new WeekendContentResolver(ShopService, _defaultShop, _specialShops),
                DailyHandSystem,
                new CardDrawPolicy()
            );
            WeekendFlowController.Initialize(RunManager, weekendBuilder);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (InputManager != null) InputManager.OnAnyInputDetected -= Events.Player.TriggerAnyInput;
        
        try
        {
            CardInteractionBootstrapper.Cleanup();
            Debug.Log("[AppCore] ✓ Limpeza concluída.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AppCore] Erro no cleanup: {ex.Message}");
        }
    }

    // --- REGISTRO DE SERVIÇOS DE CENA (Injeção Dinâmica) ---

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Boot") return;
        
        // Setup de Câmera Global para Input
        Camera activeCamera = Camera.main;
        if (activeCamera != null) InputManager.SetCamera(activeCamera);
    }

    public void RegisterGridService(IGridService service)
    {
        _gridService = service;
        
        if (CardInteractionBootstrapper.IsInitialized)
        {
            CardInteractionBootstrapper.SetGridService(service);
            Debug.Log("[AppCore] ✓ GridService injetado no contexto de runtime.");
        }
    }

    public void UnregisterGridService() => _gridService = null;

    public void RegisterDailyResolutionSystem(DailyResolutionSystem system)
    {
        DailyResolutionSystem = system;
        Debug.Log("[AppCore] ✓ DailyResolutionSystem registrado dinamicamente.");
    }

    public void UnregisterDailyResolutionSystem() => DailyResolutionSystem = null;

    // ---pattern Tracking Orchestration ---

    public void InitializePatternTracking(RunData runData)
    {
        if (runData == null) return;
        Services.SetPatternTracking(new PatternTrackingService(runData));
        Debug.Log("[AppCore] ✓ PatternTrackingService inicializado para nova run.");
    }

    public void OnWeeklyReset() => PatternTracking?.OnWeeklyReset();

    public void ReturnToMainMenu()
    {
        GameStateManager.SetState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }
}