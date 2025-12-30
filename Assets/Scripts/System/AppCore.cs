using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SINGLETON GLOBAL.
/// Responsabilidade: Manter sistemas que sobrevivem à troca de cenas (Save, Audio, Input).
/// Não deve conter lógica de gameplay específica de uma cena.
/// </summary>
public class AppCore : MonoBehaviour
{
    public static AppCore Instance { get; private set; }
    public IGameLibrary GameLibrary { get; private set; }
    public GameEvents Events { get; private set; }

    [Header("Data")]
    [SerializeField] private GameDatabaseSO _gameDatabase;

    [Header("Sistemas Globais")]
    public SaveManager SaveManager;
    public RunManager RunManager;
    public GameStateManager GameStateManager;
    public TimeManager TimeManager;
    public InputManager InputManager;
    public AudioManager AudioManager;

    // DailyResolutionSystem geralmente manipula dados da Run, então pode ficar aqui,
    // mas não deve ter referências diretas a UI da cena.
    public DailyResolutionSystem DailyResolutionSystem;

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
        // 1. Inicializa componentes internos (ordem importa se houver dependências)
        if (GameStateManager == null) GameStateManager = GetComponent<GameStateManager>() ?? gameObject.AddComponent<GameStateManager>();

        InputManager.Initialize();
        AudioManager.Initialize();
        SaveManager.Initialize();

        if (_gameDatabase != null)
        {
            GameLibrary = new GameLibraryService(_gameDatabase);
            Debug.Log("[AppCore] GameLibrary Inicializada.");
        }
        else
        {
            Debug.LogError("[AppCore] FATAL: GameDatabaseSO não atribuído!");
        }

        // 2. Injeta dependências entre sistemas globais
        RunManager.Initialize(SaveManager);
        DailyResolutionSystem.Initialize(); 
        GameStateManager.Initialize();

        // 3. Bindings globais
        InputManager.OnAnyInputDetected += () => Events.Player.TriggerAnyInput();

        // 4. Carrega a primeira cena
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

    // A única responsabilidade de cena do AppCore é injetar a câmera no InputManager
    // pois o InputManager é global mas a câmera muda.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Boot") return;

        // Atualiza a referência da câmera para o Input System
        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            InputManager.SetCamera(activeCamera);
        }

        // Se houver sistema de Câmera Global (como um CameraController Singleton), inicialize-o aqui
        if (CameraSystem.Instance != null)
        {
            CameraSystem.Instance.Initialize();
            CameraSystem.Instance.AdjustCamera();
        }
    }
}