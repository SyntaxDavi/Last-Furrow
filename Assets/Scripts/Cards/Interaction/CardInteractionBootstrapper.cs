using UnityEngine;
using System;

/// <summary>
/// Bootstrapper para Card Interactions (ESTÁTICO).
/// 
/// Responsabilidade: Criar RunIdentityContext e injetar nas estratégias.
/// 
/// Chamado por AppCore durante o setup da run.
/// NÃO é um MonoBehaviour - é apenas um orchestrador estático.
/// 
/// IMPORTANTE:
/// - Inicializa UMA VEZ no início da run (sem re-inicialização)
/// - Garanteque Factory está inicializado antes de qualquer estratégia
/// - Protegido contra inicialização dupla
/// - Valida todos os parâmetros
/// </summary>
public static class CardInteractionBootstrapper
{
    private static RunIdentityContext _identityContext;
    private static RunRuntimeContext _runtimeContext;
    private static bool _initialized = false;

    public static RunIdentityContext IdentityContext
    {
        get
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "[CardInteractionBootstrapper] Contextos não foram inicializados! " +
                    "Certifique-se de chamar Initialize() antes de usar estratégias de interação."
                );
            }
            return _identityContext;
        }
    }

    public static RunRuntimeContext RuntimeContext
    {
        get
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "[CardInteractionBootstrapper] Contextos não foram inicializados!"
                );
            }
            return _runtimeContext;
        }
    }

    /// <summary>
    /// Inicializa os contextos UMA ÚNICA VEZ.
    /// Deve ser chamado por AppCore durante o setup da run.
    /// </summary>
    public static void Initialize(
        IRunManager runManager,
        ISaveManager saveManager,
        IEconomyService economyService,
        IGameLibrary library,
        PlayerEvents playerEvents,
        GameEvents gameEvents,
        IGridService gridService = null)
    {
        // Proteção contra inicialização dupla
        if (_initialized)
        {
            Debug.LogWarning("[CardInteractionBootstrapper] Já foi inicializado! Ignorando chamada duplicada.");
            return;
        }

        try
        {
            // 1. Cria contexto de identidade (imutável, sem Grid)
            _identityContext = new RunIdentityContext(
                runManager,
                saveManager,
                economyService,
                library,
                playerEvents,
                gameEvents
            );

            // 2. Cria contexto de runtime (mutável, com Grid)
            _runtimeContext = new RunRuntimeContext(gridService);

            // 3. Injeta nas estratégias (Factory recebe ambos contextos)
            InteractionFactory.Initialize(_identityContext, _runtimeContext);

            // 4. Marca como inicializado
            _initialized = true;

            Debug.Log("[CardInteractionBootstrapper] ? Contextos inicializados com sucesso!");
            Debug.Log($"[CardInteractionBootstrapper] ?? RunIdentityContext: OK (sem Grid)");
            Debug.Log($"[CardInteractionBootstrapper] ?? RunRuntimeContext: {(gridService != null ? "OK com Grid" : "Aguardando GridService")}");
        }
        catch (ArgumentNullException ex)
        {
            Debug.LogError($"[CardInteractionBootstrapper] ERRO: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardInteractionBootstrapper] Erro inesperado: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Atualiza GridService quando cena carrega (sem re-inicializar tudo).
    /// Chamado por AppCore.RegisterGridService().
    /// </summary>
    public static void SetGridService(IGridService gridService)
    {
        if (!_initialized)
        {
            Debug.LogError("[CardInteractionBootstrapper] Contextos não inicializados! Não pode set GridService.");
            return;
        }

        _runtimeContext.SetGridService(gridService);
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
        InteractionFactory.Cleanup();
        Debug.Log("[CardInteractionBootstrapper] Limpeza concluída.");
    }

    /// <summary>
    /// Query: Verifica se está inicializado (útil para debug).
    /// </summary>
    public static bool IsInitialized => _initialized;
}



