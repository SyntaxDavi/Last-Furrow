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
    /// Contexto de IDENTIDADE da Run atual.
    /// Contém o RandomProvider determinístico.
    /// </summary>
    public static RunIdentityContext IdentityContext
    {
        get
        {
            if (!_initialized)
            {
               Debug.LogWarning("[CardInteractionBootstrapper] Acesso prematuro ao IdentityContext! A run ainda não foi configurada?");
               return default; 
            }
            return _identityContext;
        }
    }

    public static RunRuntimeContext RuntimeContext
    {
        get
        {
            if (!_initialized) return null;
            return _runtimeContext;
        }
    }

    /// <summary>
    /// Setup Inicial: Apenas armazena as dependências do sistema.
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
        // Se já estava inicializado, limpamos para garantir estado limpo
        if (_initialized)
        {
            Cleanup(); 
        }

        _runManager = runManager;
        _saveManager = saveManager;
        _economyService = economyService;
        _library = library;
        _playerEvents = playerEvents;
        _gameEvents = gameEvents;
        
        Debug.Log("[CardInteractionBootstrapper] Dependências registradas. Aguardando ConfigureForRun().");
    }

    /// <summary>
    /// Configura os contextos para uma Run específica usando sua Seed.
    /// Deve ser chamado sempre que uma Run começa ou é carregada.
    /// </summary>
    public static void ConfigureForRun(RunData runData)
    {
        if (runData == null)
        {
            Debug.LogError("[CardInteractionBootstrapper] RunData null! Não é possível configurar contextos.");
            return;
        }

        if (_runManager == null)
        {
            Debug.LogError("[CardInteractionBootstrapper] Dependências não inicializadas! Chame Initialize() primeiro.");
            return;
        }

        try
        {
            Debug.Log($"[CardInteractionBootstrapper] Configurando para Run. MasterSeed: {runData.MasterSeed}");

            // 1. Cria Random Provider Determinístico
            var randomProvider = new SeededRandomProvider(runData.MasterSeed);

            // 2. Cria Contexto de Identidade
            _identityContext = new RunIdentityContext(
                _runManager,
                _saveManager,
                _economyService,
                _library,
                randomProvider, // <--- INJEÇÃO DO RANDOM
                _playerEvents,
                _gameEvents
            );

            // 3. Cria Contexto de Runtime
            // Inicialmente null, pois o GridService geralmente é carregado após a cena
            _runtimeContext = new RunRuntimeContext(null);

            // 4. Inicializa Factory com os novos contextos
            InteractionFactory.Initialize(_identityContext, _runtimeContext);

            _initialized = true;
            Debug.Log("[CardInteractionBootstrapper] ✓ Run Configurada com Sucesso.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardInteractionBootstrapper] Erro ao configurar run: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Atualiza GridService quando cena carrega (sem re-inicializar tudo).
    /// Chamado por AppCore.RegisterGridService().
    /// 
    /// 🛡️ EARLY FAIL: Valida explicitamente que tudo está pronto.
    /// Se algo estiver errado, FALHA IMEDIATAMENTE com erro claro.
    /// </summary>
    public static void SetGridService(IGridService gridService)
    {
        // 1. Validação de Estado
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "[CardInteractionBootstrapper] ERRO CRÍTICO: Contextos não inicializados!\n" +
                "Certifique-se de chamar Initialize() antes de SetGridService().\n" +
                "Ordem correta: AppCore.Initialize() -> Bootstrapper.Initialize() -> Scene Load -> SetGridService()"
            );
        }

        // 2. Validação de Parâmetro
        if (gridService == null)
        {
            throw new ArgumentNullException(
                nameof(gridService),
                "[CardInteractionBootstrapper] ERRO CRÍTICO: GridService não pode ser null!\n" +
                "O sistema de interações depende do GridService para validar cartas no grid."
            );
        }

        // 3. Atualiza Runtime Context
        _runtimeContext.SetGridService(gridService);

        // 4. ✅ VALIDAÇÃO EXPLÍCITA: Testa se as estratégias conseguem ser criadas
        try
        {
            ValidateStrategiesReady();
            Debug.Log("[CardInteractionBootstrapper] ✅ GridService injetado e estratégias validadas com sucesso!");
        }
        catch (Exception ex)
        {
            // Se a validação falhar, LIMPA tudo para evitar estado corrompido
            _runtimeContext.Cleanup();
            throw new InvalidOperationException(
                $"[CardInteractionBootstrapper] FALHA na validação de estratégias após injetar GridService:\n{ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    /// Valida que todas as estratégias críticas podem ser resolvidas.
    /// 
    /// 🛡️ EARLY FAIL: Se alguma estratégia não puder ser criada,
    /// é melhor falhar AQUI (bootstrap) do que durante gameplay.
    /// </summary>
    private static void ValidateStrategiesReady()
    {
        if (!InteractionFactory.IsInitialized)
        {
            throw new InvalidOperationException(
                "[CardInteractionBootstrapper] InteractionFactory não está inicializado!"
            );
        }

        // Testa estratégias críticas
        var testCases = new[]
        {
            CardType.Plant,
            CardType.Modify,
            CardType.Harvest,
            CardType.Expansion
        };

        foreach (var cardType in testCases)
        {
            var strategy = InteractionFactory.GetStrategy(cardType);
            
            if (strategy == null)
            {
                throw new InvalidOperationException(
                    $"[CardInteractionBootstrapper] Estratégia para {cardType} retornou NULL!"
                );
            }

            // Se retornou NullInteractionStrategy, algo está errado
            if (strategy is NullInteractionStrategy)
            {
                throw new InvalidOperationException(
                    $"[CardInteractionBootstrapper] Estratégia para {cardType} não foi registrada no Factory!\n" +
                    $"Verifique se InteractionFactory.Initialize() está criando todas as estratégias necessárias."
                );
            }
        }
    }

    /// <summary>
    /// Cleanup. Chamado quando a run termina.
    /// </summary>
    public static void Cleanup()
    {
        _runtimeContext?.Cleanup();
        _identityContext = default;
        _runtimeContext = null;
        _initialized = false;
        
        // Não limpamos as dependências (_runManager, etc) pois elas persistem entre runs na mesma sessão
        
        InteractionFactory.Cleanup();
        Debug.Log("[CardInteractionBootstrapper] Limpeza concluída.");
    }

    public static bool IsInitialized => _initialized;
}
