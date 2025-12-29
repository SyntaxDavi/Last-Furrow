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
    public CameraSystem CameraSystem;
    public DailyResolutionSystem DailyResolutionSystem;

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
        // Se certifique que GameStateManager está no GameObject AppCore no Editor
        if (GameStateManager == null) GameStateManager = GetComponent<GameStateManager>();
        if (GameStateManager == null) GameStateManager = gameObject.AddComponent<GameStateManager>();

        DailyResolutionSystem.Initialize();

        SaveManager.Initialize();
        GameStateManager.Initialize();
        RunManager.Initialize(SaveManager);

        AudioManager.Initialize();
        InputManager.Initialize();
        CameraSystem.Initialize();

        SceneManager.LoadScene(_firstSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Boot") return;

        InputManager.UpdateCameraReference();
        CameraSystem.AdjustCamera();
    }
}