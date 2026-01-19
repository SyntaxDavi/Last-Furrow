using UnityEngine;

/// <summary>
/// Bootstrapper de UI - Injeta dependências em todos componentes UI.
/// 
/// RESPONSABILIDADE:
/// - Criar UIContext único
/// - Encontrar todos componentes UI na cena
/// - Injetar contexto via Initialize()
/// - Validar inicialização bem-sucedida
/// 
/// ARQUITETURA:
/// - Executa após GameplayBootstrapper
/// - Único ponto de configuração de UI
/// - Não gerencia estado (apenas setup)
/// 
/// SOLID:
/// - Single Responsibility: Apenas injeção
/// - Dependency Inversion: UI recebe abstrações, não implementações
/// - Open/Closed: Novos componentes são auto-descobertos
/// 
/// USO:
/// Adicione na cena Game como GameObject com este script.
/// Ordem de execução: AppCore ? GameplayBootstrapper ? UIBootstrapper
/// </summary>
public class UIBootstrapper : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private System.Collections.IEnumerator InitializeWhenReady()
    {
        // Espera AppCore estar pronto
        while (AppCore.Instance == null)
        {
            yield return null;
        }

        // Espera RunData existir
        while (AppCore.Instance.SaveManager?.Data?.CurrentRun == null)
        {
            yield return null;
        }

        Initialize();
    }

    private void Initialize()
    {
        if (_showDebugLogs)
            Debug.Log("[UIBootstrapper] Iniciando injeção de dependências...");

        // 1. Cria UIContext
        var context = CreateUIContext();

        // 2. Injeta em HeartDisplayManagerRefactored
        var heartManager = FindObjectOfType<HeartDisplayManagerRefactored>();
        if (heartManager != null)
        {
            heartManager.Initialize(context);
            if (_showDebugLogs)
                Debug.Log("[UIBootstrapper] ? HeartDisplayManagerRefactored inicializado");
        }

        // 3. Injeta em DayWeekDisplayRefactored
        var dayWeekDisplay = FindObjectOfType<DayWeekDisplayRefactored>();
        if (dayWeekDisplay != null)
        {
            dayWeekDisplay.Initialize(context);
            if (_showDebugLogs)
                Debug.Log("[UIBootstrapper] ? DayWeekDisplayRefactored inicializado");
        }

        // 4. Injeta em SleepButtonControllerRefactored
        var sleepButton = FindObjectOfType<SleepButtonControllerRefactored>();
        if (sleepButton != null)
        {
            sleepButton.Initialize(context);
            if (_showDebugLogs)
                Debug.Log("[UIBootstrapper] ? SleepButtonControllerRefactored inicializado");
        }

        // TODO FUTURO: Auto-discover de componentes que implementam IUIComponent
        // var allUIComponents = FindObjectsOfType<MonoBehaviour>().OfType<IUIComponent>();

        if (_showDebugLogs)
            Debug.Log("[UIBootstrapper] ? Injeção de dependências concluída!");
    }

    private UIContext CreateUIContext()
    {
        var events = AppCore.Instance.Events;
        var saveManager = AppCore.Instance.SaveManager;
        var runManager = AppCore.Instance.RunManager;

        // Cria adapter para RunData
        var runDataProvider = new RunDataProviderAdapter(saveManager);

        // Cria policy de tempo
        var timePolicy = new DefaultTimePolicy();

        return new UIContext(
            progressionEvents: events.Progression,
            timeEvents: events.Time,
            gameStateEvents: events.GameState,
            // economyEvents: null, // TODO: Adicionar quando existir
            gridEvents: events.Grid,
            playerEvents: events.Player,
            runData: runDataProvider,
            timePolicy: timePolicy
        );
    }
}
