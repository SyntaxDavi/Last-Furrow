using UnityEngine;
using System.Collections;

/// <summary>
/// Bootstrapper de Grid Visual - Injeta dependências em GridManager.
/// Análogo ao UIBootstrapper, responsável por setup inicial do sistema visual do grid.
/// 
/// RESPONSABILIDADE:
/// - Criar GridVisualContext com todas as dependências
/// - Encontrar GridManager na cena
/// - Injetar contexto via Initialize()
/// - Validar inicialização bem-sucedida
/// 
/// ARQUITETURA:
/// - Executa após GameplayBootstrapper ter criado GridService
/// - Único ponto de configuração visual do Grid
/// - Não gerencia estado, apenas setup
/// 
/// SOLID:
/// - Single Responsibility: Apenas injeção de dependências
/// - Dependency Inversion: GridManager recebe abstrações
/// - Open/Closed: Extensível sem modificar código existente
/// 
/// USO:
/// GameObject vazio na cena Game com este script.
/// Atribua GridVisualConfig no Inspector.
/// </summary>
public class GridVisualBootstrapper : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GridVisualConfig _visualConfig;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        if (_showDebugLogs)
            Debug.Log("[GridVisualBootstrapper] Aguardando AppCore e GridService...");

        // Espera AppCore estar pronto
        while (AppCore.Instance == null)
        {
            yield return null;
        }

        // Espera GridService estar disponivel
        while (AppCore.Instance.GetGridService() == null)
        {
            yield return null;
        }

        if (_showDebugLogs)
            Debug.Log("[GridVisualBootstrapper] AppCore e GridService prontos");

        Initialize();
    }

    private void Initialize()
    {
        if (_showDebugLogs)
            Debug.Log("[GridVisualBootstrapper] Iniciando injecao de dependencias...");

        // Validação de configuração
        if (_visualConfig == null)
        {
            Debug.LogError("[GridVisualBootstrapper] CRITICAL: GridVisualConfig nao atribuido no Inspector!");
            return;
        }

        // Cria GridVisualContext
        GridVisualContext context = CreateGridVisualContext();

        if (context == null)
        {
            Debug.LogError("[GridVisualBootstrapper] CRITICAL: Falha ao criar GridVisualContext!");
            return;
        }

        if (_showDebugLogs)
            Debug.Log("[GridVisualBootstrapper] GridVisualContext criado com sucesso");

        // Encontra GridManager na cena
        GridManager gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("[GridVisualBootstrapper] CRITICAL: GridManager nao encontrado na cena!");
            return;
        }

        if (_showDebugLogs)
            Debug.Log($"[GridVisualBootstrapper] GridManager encontrado: configurando...");

        // Injeta contexto
        gridManager.Initialize(context);

        if (_showDebugLogs)
            Debug.Log("[GridVisualBootstrapper] SUCESSO: Grid visual inicializado");
    }

    private GridVisualContext CreateGridVisualContext()
    {
        // Serviços de AppCore
        var gridService = AppCore.Instance.GridService;
        var library = AppCore.Instance.GameLibrary;
        var events = AppCore.Instance.Events;
        var gameStateManager = AppCore.Instance.GameStateManager;

        // Validações
        if (gridService == null)
        {
            Debug.LogError("[GridVisualBootstrapper] GridService null!");
            return null;
        }

        if (library == null)
        {
            Debug.LogError("[GridVisualBootstrapper] GameLibrary null!");
            return null;
        }

        if (events == null)
        {
            Debug.LogError("[GridVisualBootstrapper] Events null!");
            return null;
        }

        if (gameStateManager == null)
        {
            Debug.LogError("[GridVisualBootstrapper] GameStateManager null!");
            return null;
        }

        // Cria DropValidator
        var dropValidator = new DefaultDropValidator(gridService, gameStateManager);

        // Cria contexto
        try
        {
            return new GridVisualContext(
                gridService: gridService,
                library: library,
                dropValidator: dropValidator,
                gridEvents: events.Grid,
                gameStateEvents: events.GameState,
                gameStateManager: gameStateManager,
                visualConfig: _visualConfig
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridVisualBootstrapper] Erro ao criar contexto: {ex.Message}");
            return null;
        }
    }
}
