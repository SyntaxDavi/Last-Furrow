using System.Collections.Generic;
using UnityEngine;

// Estratégia de Segurança (Null Object)
public class NullInteractionStrategy : ICardInteractionStrategy
{
    public bool CanInteract(int index, IGridService grid, CardData card) => false;

    public InteractionResult Execute(int index, IGridService grid, CardData card)
    {
        return InteractionResult.Fail($"Nenhuma estratégia definida para o tipo: {card.Type}");
    }
}

/// <summary>
/// Factory para estratégias de interação de cartas.
/// 
/// Recebe DOIS contextos:
/// - RunIdentityContext (imutável, sem Grid)
/// - RunRuntimeContext (mutável, com Grid)
/// 
/// Estratégias injetam ambos conforme necessário.
/// </summary>
public static class InteractionFactory
{
    private static readonly Dictionary<CardType, ICardInteractionStrategy> _strategies
        = new Dictionary<CardType, ICardInteractionStrategy>();

    private static readonly NullInteractionStrategy _nullStrategy = new NullInteractionStrategy();
    
    private static bool _initialized = false;

    /// <summary>
    /// Inicializa com ambos contextos.
    /// Protegido contra múltiplas inicializações.
    /// </summary>
    public static void Initialize(RunIdentityContext identityContext, RunRuntimeContext runtimeContext)
    {
        if (_initialized)
        {
            Debug.LogWarning("[InteractionFactory] Já foi inicializado! Ignorando chamada duplicada.");
            return;
        }

        try
        {
            _strategies.Clear();

            // Cria estratégias COM INJEÇÃO DE AMBOS CONTEXTOS
            _strategies[CardType.Plant] = new PlantInteraction(identityContext);
            _strategies[CardType.Modify] = new WaterInteractionStrategy(identityContext, runtimeContext);
            _strategies[CardType.Care] = new WaterInteractionStrategy(identityContext, runtimeContext);
            _strategies[CardType.Harvest] = new HarvestInteractionStrategy(identityContext);
            _strategies[CardType.Clear] = new ClearInteractionStrategy(identityContext);
            _strategies[CardType.Expansion] = new UnlockInteractionStrategy(identityContext);

            _initialized = true;
            Debug.Log("[InteractionFactory] ? Inicializado com sucesso! 5 estratégias injetadas.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InteractionFactory] Erro ao inicializar: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Fornece uma estratégia para um tipo de carta.
    /// SEGURO: Nunca retorna null.
    /// </summary>
    public static ICardInteractionStrategy GetStrategy(CardType type)
    {
        if (!_initialized)
        {
            Debug.LogError(
                "[InteractionFactory] ERRO: Factory não foi inicializado! " +
                "Certifique-se de chamar CardInteractionBootstrapper.Initialize() antes de usar cartas."
            );
            return _nullStrategy;
        }

        if (_strategies.TryGetValue(type, out var strategy))
        {
            if (strategy == null)
            {
                Debug.LogWarning($"[InteractionFactory] Strategy para {type} é null! Usando NullStrategy.");
                return _nullStrategy;
            }
            return strategy;
        }

        Debug.LogWarning($"[InteractionFactory] Tipo de carta desconhecido: {type}. Usando NullStrategy.");
        return _nullStrategy;
    }

    /// <summary>
    /// Cleanup.
    /// </summary>
    public static void Cleanup()
    {
        _strategies.Clear();
        _initialized = false;
        Debug.Log("[InteractionFactory] Limpeza concluída.");
    }

    /// <summary>
    /// Query: Verifica se factory está pronto.
    /// </summary>
    public static bool IsInitialized => _initialized;
}
