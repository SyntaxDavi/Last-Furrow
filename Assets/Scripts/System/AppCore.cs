using UnityEngine;
using UnityEngine.SceneManagement;

public class AppCore : MonoBehaviour
{
    public static AppCore Instance { get; private set; }

    [Header("Core Systems")]
    public SaveManager SaveManager;
    public RunManager RunManager;
    public GameStateManager GameStateManager;
    public TimeManager TimeManager;
    public InputManager InputManager;
    public AudioManager AudioManager;

    public DailyResolutionSystem DailyResolutionSystem;
    public GridInteractionSystem GridInteractionSystem;
    private System.Action _onAnyInputHandler;

    // Barramento de Eventos
    public GameEvents Events { get; private set; }

    [Header("Debug")]
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

        InitializeServices();
    }

    public void ReturnToMainMenu()
    {
        Events.ResetAllListeners();
        GameStateManager.SetState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void InitializeServices()
    {
        // Garante componentes essenciais
        if (GameStateManager == null) GameStateManager = GetComponent<GameStateManager>();
        if (GameStateManager == null) GameStateManager = gameObject.AddComponent<GameStateManager>();

        // Inicializa Sistemas Filhos (que estão no objeto AppCore)
        DailyResolutionSystem.Initialize();
        SaveManager.Initialize();
        GameStateManager.Initialize();
        RunManager.Initialize(SaveManager);
        AudioManager.Initialize();
        InputManager.Initialize();
        _onAnyInputHandler = () => Events.TriggerAnyInput();
        InputManager.OnAnyInputDetected += _onAnyInputHandler;

        if (CameraSystem.Instance != null)
        {
            CameraSystem.Instance.Initialize();
        }

        // Carrega a primeira cena APÓS tudo estar pronto
        SceneManager.LoadScene(_firstSceneName);
    }
    private void OnDestroy()
    {
        if (InputManager != null && _onAnyInputHandler != null)
            InputManager.OnAnyInputDetected -= _onAnyInputHandler;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Boot") return;

        // Pega a câmera ativa (pode ser a Singleton ou a da cena)
        Camera activeCamera = Camera.main;

        // 1. Injeta a Câmera no InputManager (Maneira correta)
        InputManager.SetCamera(activeCamera);

        // 2. Ajusta o Zoom
        if (CameraSystem.Instance != null)
        {
            // Se o CameraSystem for Singleton, ele já sabe quem é a câmera interna dele,
            // mas é bom garantir que ele atualize a lógica.
            CameraSystem.Instance.AdjustCamera();
        }
    }
}