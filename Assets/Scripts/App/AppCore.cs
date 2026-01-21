using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; 

public class AppCore : MonoBehaviour
{
    public static AppCore Instance { get; private set; }

    // Serviços Globais
    public IGameLibrary GameLibrary { get; private set; }
    public GameEvents Events { get; private set; }
    public IEconomyService EconomyService { get; private set; }
    public DailyHandSystem DailyHandSystem { get; private set; }
    public WeeklyGoalSystem WeeklyGoalSystem { get; private set; }
    public ShopService ShopService { get; private set; }
    public PatternDetector PatternDetector { get; private set; }
    public PatternScoreCalculator PatternCalculator { get; private set; }
    public PatternTrackingService PatternTracking { get; private set; }

    [Header("Data")]
    [SerializeField] private GameDatabaseSO _gameDatabase;
    [SerializeField] private GridConfiguration _gridConfiguration;
    [SerializeField] private PatternWeightConfig _patternWeightConfig;

    [Header("Game Design Configs")]
    [SerializeField] private ProgressionSettingsSO _progressionSettings;
    
    [Header("Pattern System")]
    [SerializeField] private PatternLibrary _patternLibrary;

    public GridConfiguration GridConfiguration => _gridConfiguration;
    public PatternWeightConfig PatternWeightConfig => _patternWeightConfig;

    [Header("Configuração de Loja (Para o Flow)")]
    [SerializeField] private ShopProfileSO _defaultShop;
    [SerializeField] private List<ShopProfileSO> _specialShops;

    [Header("Sistemas Globais (MonoBehaviours)")]
    public SaveManager SaveManager;
    public RunManager RunManager;
    public GameStateManager GameStateManager;
    public TimeManager TimeManager;
    public InputManager InputManager;
    public AudioManager AudioManager;
    public DailyResolutionSystem DailyResolutionSystem;

    // O Controlador do Flow (Arraste na cena)
    public WeekendFlowController WeekendFlowController;

    private IGridService _gridService;  
    
    // Propriedade publica para GridService (read-only)
    public IGridService GridService => _gridService;

    [Header("Configuração")]
    [SerializeField] private string _firstSceneName = "Game";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Events = new GameEvents();
        InitializeGlobalServices();
    }

    private void InitializeGlobalServices()
    {
        // 1. MonoBehaviours Básicos (Sem dependências complexas)
        if (GameStateManager == null) GameStateManager = GetComponent<GameStateManager>() ?? gameObject.AddComponent<GameStateManager>();
        InputManager.Initialize();
        AudioManager.Initialize();
        
        // ⭐ INJEÇÃO: SaveManager recebe GridConfiguration explicitamente
        if (_gridConfiguration != null)
        {
            SaveManager.Initialize(_gridConfiguration);
        }
        else
        {
            Debug.LogError("[AppCore] GridConfiguration não atribuída! SaveManager não poderá validar compatibilidade.");
            SaveManager.Initialize(); // Fallback (legacy)
        }

        if (_gameDatabase != null)
        {
            GameLibrary = new GameLibraryService(_gameDatabase);
            Debug.Log("[AppCore] GameLibrary Inicializada.");
        }

        // 2. Inicializa Domínio    
        RunManager.Initialize(SaveManager);

        // --- MUDANÇA AQUI: CRIAR SERVIÇOS PUROS PRIMEIRO ---
        // Eles precisam existir antes que o DailyResolutionSystem tente acessá-los.

        EconomyService = new EconomyService(RunManager, SaveManager);
        DailyHandSystem = new DailyHandSystem(GameLibrary, EconomyService, new SeasonalCardStrategy(), Events.Player);
        WeeklyGoalSystem = new WeeklyGoalSystem(GameLibrary, Events.Progression, _progressionSettings);
        ShopService = new ShopService(EconomyService, SaveManager, GameLibrary, Events);
        
        // ⭐ NOVO: Pattern System (Onda 5.5 - SOLID Refactor)
        if (_patternLibrary == null)
        {
            Debug.LogError("[AppCore] PatternLibrary não atribuída! Pattern System não funcionará.");
        }
        
        // Criar Factory (type-safe, sem reflexão)
        var patternFactory = new PatternFactory();
        
        // Criar Detector com Factory (Dependency Inversion)
        PatternDetector = new PatternDetector(_patternLibrary, patternFactory);
        
        // Criar Calculator com Library
        PatternCalculator = new PatternScoreCalculator(GameLibrary);
        
        // NOTA: PatternTracking é inicializado depois, quando RunData estiver disponível
        // Ver InitializePatternTracking() chamado pelo RunManager ou GameplayBootstrapper
        Debug.Log("[AppCore] ✓ Pattern System inicializado (Onda 5.5 - SOLID)");

        // Injeta RunIdentityContext (imutável) em TODAS as estratégias
        // Grid será adicionado depois (via SetGridService)
        // Ninguém mais acessa AppCore para pegar EconomyService nas estratégias
        // Tudo vem via injeção explícita
        try
        {
            CardInteractionBootstrapper.Initialize(
                RunManager,
                SaveManager,
                EconomyService,
                GameLibrary,
                Events.Player,
                Events,
                null // GridService será definido quando a scene carregar
            );
            Debug.Log("[AppCore] ✓ CardInteractionBootstrapper inicializado com sucesso!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AppCore] ERRO crítico ao inicializar CardInteractionBootstrapper: {ex.Message}");
            throw;
        }

        // ----------------------------------------------------

        // 3. Inicializa Sistemas de Regra (QUE DEPENDEM DOS SERVIÇOS ACIMA)
        // Agora, quando Initialize() rodar, o WeeklyGoalSystem já existe!

        GameStateManager.Initialize();

        // 4. Injeção de Dependência do Flow (Mantido igual)
        if (WeekendFlowController != null)
        {
            var weekendStateFlow = new WeekendStateFlow(GameStateManager);
            var weekendUIFlow = new WeekendUIFlow(Events.UI);
            var weekendContentResolver = new WeekendContentResolver(ShopService, _defaultShop, _specialShops);

            var weekendBuilder = new DefaultWeekendFlowBuilder(
                weekendStateFlow,
                weekendUIFlow,
                weekendContentResolver
            );

            WeekendFlowController.Initialize(RunManager, weekendBuilder);
        }
        else
        {
            Debug.LogError("[AppCore] WeekendFlowController não atribuído!");
        }
        // -----------------------------------------------

        InputManager.OnAnyInputDetected += () => Events.Player.TriggerAnyInput();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(_firstSceneName);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (InputManager != null) InputManager.OnAnyInputDetected -= Events.Player.TriggerAnyInput;
        
        // Garante que todas as dependências injetadas sejam limpas
        try
        {
            CardInteractionBootstrapper.Cleanup();
            Debug.Log("[AppCore] ✓ CardInteractionBootstrapper limpeza concluída.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AppCore] Erro ao fazer cleanup do CardInteractionBootstrapper: {ex.Message}");
        }
    }

    public void ReturnToMainMenu()
    {
        GameStateManager.SetState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Boot") return;
        Camera activeCamera = Camera.main;
        if (activeCamera != null) InputManager.SetCamera(activeCamera);

        // SE a cena for a de Gameplay, injeta as dependências do DailySystem
        if (scene.name == "Game" || scene.name == _firstSceneName)
        {
            // Pequeno delay para garantir que o GridService da cena já se registrou no AppCore
            StartCoroutine(InjectDailySystemRoutine());
        }
    }
    private System.Collections.IEnumerator InjectDailySystemRoutine()
    {
        yield return null; // Espera 1 frame para GridManager registrar o GridService
        InjectDailySystemDependencies();
    }
    // --- REGISTRO DE SERVIÇOS DE CENA ---

    public IGridService GetGridLogic()
    {
        if (_gridService == null)
        {
            Debug.LogError("FATAL: GridService não encontrado!");
            return null;
        }
        return _gridService;
    }

    public void RegisterGridService(IGridService service)
    {
        _gridService = service;
        
        // Sem re-inicialização de nada - muito mais seguro
        if (CardInteractionBootstrapper.IsInitialized)
        {
            try
            {
                CardInteractionBootstrapper.SetGridService(service);
                Debug.Log("[AppCore] ✓ GridService injetado no RunRuntimeContext!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AppCore] Erro ao injetar GridService: {ex.Message}");
            }
        }
    }
    public void InjectDailySystemDependencies()
    {
        if (DailyResolutionSystem == null) return;

        // 1. Validações de Dados
        if (SaveManager?.Data?.CurrentRun == null) return;

        if (PatternTracking == null)
        {
            InitializePatternTracking(SaveManager.Data.CurrentRun);
        }

        // 2. ONDA 6.3: Buscar Bootstrapper (1x ao invés de 3x FindFirstObjectByType)
        var bootstrapper = FindFirstObjectByType<DailyVisualBootstrapper>();
        
        if (bootstrapper == null)
        {
            Debug.LogError("[AppCore] FATAL: DailyVisualBootstrapper não encontrado na scene! Adicione o componente.");
            return;
        }

        // 3. Criar Contexto de Lógica
        var logicContext = new DailyResolutionContext(
            RunManager,
            SaveManager,
            InputManager,
            Events,
            DailyHandSystem,
            WeeklyGoalSystem,
            GetGridLogic(),
            PatternDetector,
            PatternTracking,
            PatternCalculator
        );
        
        // 4. Criar Contexto Visual via Bootstrapper (valida automaticamente)
        var visualContext = bootstrapper.CreateContext();
        
        if (visualContext == null || !visualContext.IsValid())
        {
            Debug.LogError("[AppCore] FATAL: VisualContext inválido! Verifique referências no Bootstrapper.");
            return;
        }
        
        // 5. Criar Pipeline Builder (Factory Pattern)
        var pipelineBuilder = new DailyPipelineBuilder();

        // 6. Injetar TODOS contextos + Builder no sistema
        DailyResolutionSystem.Construct(logicContext, visualContext, pipelineBuilder);

        Debug.Log($"[AppCore] ✓ DailySystem Construct OK - Builder: {pipelineBuilder.GetType().Name}");
    }

    public void UnregisterGridService()
    {
        _gridService = null;
    }
    
    // ===== ONDA 4: Pattern Tracking =====
    
    /// <summary>
    /// Inicializa o PatternTrackingService quando o RunData estiver disponível.
    /// Chamado pelo GameplayBootstrapper após carregar/criar uma run.
    /// </summary>
    public void InitializePatternTracking(RunData runData)
    {
        if (runData == null)
        {
            Debug.LogError("[AppCore] Não é possível inicializar PatternTracking sem RunData!");
            return;
        }
        
        PatternTracking = new PatternTrackingService(runData);
        Debug.Log("[AppCore] ✓ PatternTrackingService inicializado");
    }
    
    /// <summary>
    /// Chamado no início de uma nova semana para resetar tracking.
    /// </summary>
    public void OnWeeklyReset()
    {
        PatternTracking?.OnWeeklyReset();
    }
}