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

    [Header("Data")]
    [SerializeField] private GameDatabaseSO _gameDatabase;

    [Header("Game Design Configs")]
    [SerializeField] private ProgressionSettingsSO _progressionSettings;

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

    private IGridService _gridService; // Privado

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
        SaveManager.Initialize();

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

        // ----------------------------------------------------

        // 3. Inicializa Sistemas de Regra (QUE DEPENDEM DOS SERVIÇOS ACIMA)
        // Agora, quando Initialize() rodar, o WeeklyGoalSystem já existe!
        DailyResolutionSystem.Initialize();

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
    }

    public void UnregisterGridService()
    {
        _gridService = null;
    }
}