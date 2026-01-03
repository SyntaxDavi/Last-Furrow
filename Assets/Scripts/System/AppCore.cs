using UnityEngine;
using UnityEngine.SceneManagement;

public class AppCore : MonoBehaviour
{
    public static AppCore Instance { get; private set; }
    public IGameLibrary GameLibrary { get; private set; }
    public GameEvents Events { get; private set; }
    public IEconomyService EconomyService { get; private set; }

    [Header("Data")]
    [SerializeField] private GameDatabaseSO _gameDatabase;

    [Header("Sistemas Globais")]
    public SaveManager SaveManager;
    public RunManager RunManager;
    public GameStateManager GameStateManager;
    public TimeManager TimeManager;
    public InputManager InputManager;
    public AudioManager AudioManager;

    // DailyResolutionSystem
    public DailyResolutionSystem DailyResolutionSystem;

    // O serviço é privado e só alterado por métodos explícitos
    private IGridService _gridService;

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
        if (GameStateManager == null) GameStateManager = GetComponent<GameStateManager>() ?? gameObject.AddComponent<GameStateManager>();

        InputManager.Initialize();
        AudioManager.Initialize();
        SaveManager.Initialize();

        if (_gameDatabase != null)
        {
            GameLibrary = new GameLibraryService(_gameDatabase);
            Debug.Log("[AppCore] GameLibrary Inicializada.");
        }

        RunManager.Initialize(SaveManager);
        DailyResolutionSystem.Initialize();
        GameStateManager.Initialize();
        EconomyService = new EconomyService(RunManager, SaveManager);

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

    // --- ARQUITETURA ESTRITA DE SERVIÇO ---

    /// <summary>
    /// Retorna a lógica do Grid atual.
    /// ATENÇÃO: Lança erro se o Bootstrapper da cena não tiver registrado o serviço.
    /// Isso garante que não existam serviços "fantasmas".
    /// </summary>
    public IGridService GetGridLogic()
    {
        if (_gridService == null)
        {
            Debug.LogError("FATAL: Tentativa de acessar GridService antes do Bootstrapper registrar! " +
                           "Verifique a ordem de execução ou se você está na cena de Gameplay.");
            return null;
        }
        return _gridService;
    }

    /// <summary>
    /// Chamado EXCLUSIVAMENTE pelo Bootstrapper da cena de Gameplay.
    /// </summary>
    public void RegisterGridService(IGridService service)
    {
        if (_gridService != null && _gridService != service)
        {
            Debug.LogWarning("[AppCore] Substituindo um GridService existente. Isso é normal na troca de cenas, mas perigoso se for acidental.");
        }
        _gridService = service;
        Debug.Log("[AppCore] GridService registrado com sucesso.");
    }

    /// <summary>
    /// Limpa a referência quando a cena de gameplay é destruída.
    /// </summary>
    public void UnregisterGridService()
    {
        _gridService = null;
    }
}