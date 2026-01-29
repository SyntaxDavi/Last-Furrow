using UnityEngine;
using System;

/// <summary>
/// Bootstrapper para Card Interactions (ESTÁTICO).
///
/// Responsabilidade: Criar RunIdentityContext e injetar nas estratégias.
///
/// Chamado por AppCore durante o setup da run.
/// NÃO É um MonoBehaviour - É apenas um orchestrador estático.
///
/// IMPORTANTE:
/// - Inicializa UMA VEZ no início da run (sem re-inicialização)
/// - Garante que Factory esteja inicializado antes de qualquer estratégia
/// - Protegido contra inicialização dupla
/// - Valida todos os parâmetros
/// </summary>
public static class CardInteractionBootstrapper
{
    private static bool _initialized = false;
    private static RunIdentityContext _identityContext;
    private static RunRuntimeContext _runtimeContext;
    
    // Dependências cacheadas para recriação do contexto a cada run
    private static IRunManager _runManager;
    private static ISaveManager _saveManager;
    private static IEconomyService _economyService;
    private static IGameLibrary _library;
    private static PlayerEvents _playerEvents;
    private static GameEvents _gameEvents;

    /// <summary>
    /// Garante que campos estáticos sejam limpos no Domain Reload (sem reload de domínio na Unity).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        _initialized = false;
        _identityContext = default;
        _runtimeContext = null;
        _runManager = null;
        _saveManager = null;
        _economyService = null;
        _library = null;
        _playerEvents = null;
        _gameEvents = null;
    }

    /// <summary>
    /// Contexto de IDENTIDADE da Run atual.
    /// Contém o RandomProvider determinístico.
    /// </summary>
    public static RunIdentityContext IdentityContext
    {
        get
        {
            if (!_initialized)
            {
                throw new InvalidOperationException($"[{nameof(CardInteractionBootstrapper)}] Acesso prematuro ao IdentityContext! A run ainda não foi configurada.");
            }
            return _identityContext;
        }
    }

    /// <summary>
    /// Contexto de RUNTIME da Run atual (Grid, etc).
    /// </summary>
    public static RunRuntimeContext RuntimeContext
    {
        get
        {
            if (!_initialized)
            {
                throw new InvalidOperationException($"[{nameof(CardInteractionBootstrapper)}] Acesso prematuro ao RuntimeContext!");
            }
            return _runtimeContext;
        }
    }

    /// <summary>
    /// Setup Inicial: Armazena as dependências persistentes do sistema.
    /// </summary>
    public static void Initialize(
        IRunManager runManager,
        ISaveManager saveManager,
        IEconomyService economyService,
        IGameLibrary library,
        PlayerEvents playerEvents,
        GameEvents gameEvents,
        IGridService initialGridService)
    {
        if (_initialized)
        {
            Debug.Log($"[{nameof(CardInteractionBootstrapper)}] Re-inicializando dependências...");
        }

        _runManager = runManager ?? throw new ArgumentNullException(nameof(runManager));
        _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
        _economyService = economyService ?? throw new ArgumentNullException(nameof(economyService));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _playerEvents = playerEvents ?? throw new ArgumentNullException(nameof(playerEvents));
        _gameEvents = gameEvents ?? throw new ArgumentNullException(nameof(gameEvents));
        
        Debug.Log($"[{nameof(CardInteractionBootstrapper)}] Dependências registradas com sucesso.");
    }

    /// <summary>
    /// Configura os contextos para uma Run específica usando sua Seed.
    /// </summary>
    public static void ConfigureForRun(RunData runData)
    {
        if (runData == null)
        {
            throw new ArgumentNullException(nameof(runData), $"[{nameof(CardInteractionBootstrapper)}] RunData não pode ser null.");
        }

        if (_runManager == null)
        {
            throw new InvalidOperationException($"[{nameof(CardInteractionBootstrapper)}] Dependências não inicializadas! Chame Initialize() primeiro.");
        }

        try
        {
            Debug.Log($"[{nameof(CardInteractionBootstrapper)}] Configurando contextos para Run (Seed: {runData.MasterSeed})");

            // 1. Cria Random Provider Determinístico
            var randomProvider = new SeededRandomProvider(runData.MasterSeed);

            // 2. Cria Contexto de Identidade
            _identityContext = new RunIdentityContext(
                _runManager,
                _saveManager,
                _economyService,
                _library,
                randomProvider,
                _playerEvents,
                _gameEvents
            );

            // 3. Cria Contexto de Runtime (Grid será injetado depois via SetGridService)
            _runtimeContext = new RunRuntimeContext(null);

            // 4. Inicializa Factory
            InteractionFactory.Initialize(_identityContext, _runtimeContext);

            _initialized = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(CardInteractionBootstrapper)}] Falha crítica na configuração da run: {ex.Message}");
            _initialized = false;
            throw;
        }
    }

    /// <summary>
    /// Injeta o GridService no contexto de runtime.
    /// </summary>
    public static void SetGridService(IGridService gridService)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"[{nameof(CardInteractionBootstrapper)}] Tentativa de SetGridService sem inicialização prévia.");
        }

        if (gridService == null)
        {
            throw new ArgumentNullException(nameof(gridService), $"[{nameof(CardInteractionBootstrapper)}] GridService null injetado.");
        }

        _runtimeContext.SetGridService(gridService);

        // Validação imediata para garantir que o sistema está funcional
        ValidateStrategiesReady();
        
        Debug.Log($"[{nameof(CardInteractionBootstrapper)}] GridService injetado e validado.");
    }

    private static void ValidateStrategiesReady()
    {
        if (!InteractionFactory.IsInitialized)
        {
            throw new InvalidOperationException($"[{nameof(CardInteractionBootstrapper)}] InteractionFactory não inicializou corretamente.");
        }

        // Tipos críticos que DEVEM ter estratégias válidas
        var criticalTypes = new[] { CardType.Plant, CardType.Modify, CardType.Harvest, CardType.Expansion };

        foreach (var type in criticalTypes)
        {
            var strategy = InteractionFactory.GetStrategy(type);
            if (strategy == null || strategy is NullInteractionStrategy)
            {
                throw new InvalidOperationException($"[{nameof(CardInteractionBootstrapper)}] Estratégia crítica ausente ou inválida para: {type}");
            }
        }
    }

    public static void Cleanup()
    {
        _runtimeContext?.Cleanup();
        _identityContext = default;
        _runtimeContext = null;
        _initialized = false;
        
        InteractionFactory.Cleanup();
        Debug.Log($"[{nameof(CardInteractionBootstrapper)}] Cleanup concluído.");
    }

    public static bool IsInitialized => _initialized;
}
