using UnityEngine;

/// <summary>
/// COMPOSITION ROOT DA CENA DE GAMEPLAY.
/// Responsabilidade: Pegar dados do AppCore, criar serviços de lógica e injetar nos Controllers visuais
/// </summary>
public class GameplayBootstrapper : MonoBehaviour
{
    [Header("Controllers da Cena")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PlayerInteraction _playerInteraction;
    // Futuro: [SerializeField] private HandManager _handManager;

    // Mantemos referência aos serviços criados para limpeza
    private IGridService _gridService;

    private void Awake()
    {
        // 1. Segurança: Garante que o AppCore existe
        if (AppCore.Instance == null)
        {
            Debug.LogError("AppCore não encontrado! Inicie o jogo pela cena de Boot/Splash.");
            return;
        }

        // 2. Verifica se existe uma Run ativa (Dados)
        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        if (runData == null)
        {
            // Se cairmos na cena de jogo sem dados (debug), cria um teste
            Debug.LogWarning("[Bootstrapper] Sem RunData ativo. Criando Run de Teste/Debug.");
            AppCore.Instance.RunManager.StartNewRun();
            runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        }

        InitializeGameplaySystems(runData);
    }

    private void InitializeGameplaySystems(RunData runData)
    {
        Debug.Log("[Bootstrapper] Inicializando sistemas de gameplay...");

        // --- A. CONFIGURAÇÃO DE INPUT ---
        // Injeta o InputManager global no PlayerInteraction local
        _playerInteraction.Initialize(AppCore.Instance.InputManager);

        // --- B. CONFIGURAÇÃO DO GRID (A nova arquitetura) ---

        // 1. Cria a Lógica (Model/Service)
        _gridService = new GridService(runData);

        // 2. Conecta callbacks de sistema (Persistência)
        // Quando o GridService disser "Mudei algo", avisamos o SaveManager
        _gridService.OnDataDirty += () => AppCore.Instance.SaveManager.SaveGame();

        // 3. Injeta o Serviço no Controller Visual (Dependency Injection)
        if (_gridManager != null)
        {
            _gridManager.Configure(_gridService);
        }
        else
        {
            Debug.LogError("[Bootstrapper] GridManager não atribuído no Inspector!");
        }

        // --- C. BINDING DE EVENTOS GLOBAIS ---

        // Conecta eventos do AppCore (RunManager) aos Controllers da cena
        AppCore.Instance.Events.OnDayChanged += HandleDayChanged;

        // Configura estado inicial do jogo
        AppCore.Instance.GameStateManager.SetState(GameState.Playing);
    }

    private void OnDestroy()
    {
        // Limpeza é crucial para evitar memory leaks de eventos
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.OnDayChanged -= HandleDayChanged;
        }
    }

    // --- EVENT HANDLERS (A Cola entre Global e Local) ---

    private void HandleDayChanged(int day)
    {
        // Orquestra atualizações da cena quando o dia muda
        if (_gridManager != null) _gridManager.RefreshAllSlots();

        // Ex: _handManager.RefreshHand();
        // Ex: _uiManager.UpdateDayText(day);
    }
}