using System;
using System.Collections.Generic;
using UnityEngine;

// ==============================================================================================
// EVENTS - Eventos de Reorganização de Mão (Interface de Leitura)
// ==============================================================================================

/// <summary>
/// Interface de leitura para eventos (ninguém externo pode disparar)
/// </summary>
public interface IHandReorganizationEventsReader
{
    event Action<CardView, Vector3> OnCardPickedUp;
    event Action<CardView> OnCardDropped;
    event Action<CardView, int> OnCardRepositioned;
    event Action OnHandReorganized;
}

/// <summary>
/// Eventos relacionados à reorganização manual de cartas da mão
/// Apenas internamente pode disparar eventos
/// </summary>
public class HandReorganizationEvents : IHandReorganizationEventsReader
{
    public event Action<CardView, Vector3> OnCardPickedUp;
    public event Action<CardView> OnCardDropped;
    public event Action<CardView, int> OnCardRepositioned;
    public event Action OnHandReorganized;

    // Apenas interno pode disparar
    internal void TriggerCardPickedUp(CardView card, Vector3 position) => OnCardPickedUp?.Invoke(card, position);
    internal void TriggerCardDropped(CardView card) => OnCardDropped?.Invoke(card);
    internal void TriggerCardRepositioned(CardView card, int newIndex) => OnCardRepositioned?.Invoke(card, newIndex);
    internal void TriggerHandReorganized() => OnHandReorganized?.Invoke();
}

// ==============================================================================================
// INTERFACES - Padrão Strategy para Reorganização
// ==============================================================================================

/// <summary>
/// Define o contrato para resolver uma posição alvo baseado em coordenadas
/// NOTA: Recebe APENAS dados abstratos (count, positions teóricas)
/// Não precisa conhecer CardView - apenas semântica de slots
/// </summary>
public interface IHandSlotResolver
{
    /// <summary>
    /// Determina qual slot uma carta deve ocupar baseado em sua posição atual
    /// </summary>
    int ResolveSlot(Vector3 cardPosition, int totalSlots, HandLayoutConfig config);
}

/// <summary>
/// Define o contrato para estratégias de animação durante reorganização
/// </summary>
public interface IReorganizationAnimationStrategy
{
    /// <summary>
    /// Anima uma carta para um novo slot
    /// Tudo que precisa é injetado aqui
    /// </summary>
    void AnimateCardToSlot(CardView card, int slotIndex, int totalCards, HandLayoutConfig config, Vector3 handCenter);
}

// ==============================================================================================
// FACTORY - Cria componentes de Reorganização
// ==============================================================================================

/// <summary>
/// Factory para criar estratégias e resolvadores de slots
/// Segue padrão de Factory + Dependency Injection
/// </summary>
public static class HandReorganizationFactory
{
    public static IHandSlotResolver CreateSlotResolver(SlotResolutionStrategy strategy)
    {
        return strategy switch
        {
            SlotResolutionStrategy.Proximity => new ProximitySlotResolver(),
            SlotResolutionStrategy.GridBased => new GridBasedSlotResolver(),
            _ => new ProximitySlotResolver() // Fallback (Null Object Pattern)
        };
    }

    public static IReorganizationAnimationStrategy CreateAnimationStrategy(AnimationStrategy strategy)
    {
        return strategy switch
        {
            AnimationStrategy.Smooth => new SmoothReorganizationAnimation(),
            AnimationStrategy.Instant => new InstantReorganizationAnimation(),
            _ => new SmoothReorganizationAnimation()
        };
    }
}

public enum SlotResolutionStrategy
{
    Proximity,      // Baseado na distância do mouse
    GridBased,      // Baseado em grid visual
}

public enum AnimationStrategy
{
    Smooth,         // SmoothDamp
    Instant,        // Snap imediato
}

// ==============================================================================================
// IMPLEMENTAÇÕES - Resolvadores de Slots
// ==============================================================================================

/// <summary>
/// Resolve slot baseado na proximidade da carta com os slots disponíveis
/// IMPORTANTE: Usa APENAS dados abstratos (posições teóricas)
/// </summary>
public class ProximitySlotResolver : IHandSlotResolver
{
    public int ResolveSlot(Vector3 cardPosition, int totalSlots, HandLayoutConfig config)
    {
        if (totalSlots == 0) return 0;

        float closestDistance = float.MaxValue;
        int closestSlotIndex = 0;

        // Testa cada slot teórico
        for (int i = 0; i < totalSlots; i++)
        {
            var theoreticalSlot = HandLayoutCalculator.CalculateSlot(
                i,
                totalSlots,
                config,
                Vector3.zero  // Normalizado para comparação
            );

            float distance = Vector3.Distance(cardPosition, theoreticalSlot.Position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSlotIndex = i;
            }
        }

        return Mathf.Clamp(closestSlotIndex, 0, totalSlots - 1);
    }
}

/// <summary>
/// Resolve slot baseado em zona de grid (eixo X)
/// IMPORTANTE: Usa APENAS dados abstratos (positions teóricas)
/// </summary>
public class GridBasedSlotResolver : IHandSlotResolver
{
    public int ResolveSlot(Vector3 cardPosition, int totalSlots, HandLayoutConfig config)
    {
        if (totalSlots == 0) return 0;

        // Calcula slots teóricos ordenados por X
        var theoreticalSlots = new List<(int index, float x)>();
        for (int i = 0; i < totalSlots; i++)
        {
            var slot = HandLayoutCalculator.CalculateSlot(i, totalSlots, config, Vector3.zero);
            theoreticalSlots.Add((i, slot.Position.x));
        }

        // Encontra o slot teórico mais próximo (em X) da posição atual
        float closestDistance = float.MaxValue;
        int closestSlotIndex = 0;

        foreach (var (index, slotX) in theoreticalSlots)
        {
            float distance = Mathf.Abs(cardPosition.x - slotX);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSlotIndex = index;
            }
        }

        return closestSlotIndex;
    }
}

// ==============================================================================================
// IMPLEMENTAÇÕES - Estratégias de Animação
// ==============================================================================================

/// <summary>
/// Anima a carta suavemente para o novo slot
/// Totalmente agnóstica a hierarquia Unity ou HandManager
/// </summary>
public class SmoothReorganizationAnimation : IReorganizationAnimationStrategy
{
    public void AnimateCardToSlot(CardView card, int slotIndex, int totalCards, HandLayoutConfig config, Vector3 handCenter)
    {
        if (card == null || config == null) return;

        var target = HandLayoutCalculator.CalculateSlot(
            slotIndex,
            totalCards,
            config,
            handCenter
        );

        card.UpdateLayoutTarget(target);
    }
}

/// <summary>
/// Teletransporta a carta imediatamente para o novo slot
/// Totalmente agnóstica a hierarquia Unity ou HandManager
/// </summary>
public class InstantReorganizationAnimation : IReorganizationAnimationStrategy
{
    public void AnimateCardToSlot(CardView card, int slotIndex, int totalCards, HandLayoutConfig config, Vector3 handCenter)
    {
        if (card == null || config == null) return;

        var target = HandLayoutCalculator.CalculateSlot(
            slotIndex,
            totalCards,
            config,
            handCenter
        );

        var visualTarget = CardVisualTarget.Create(target.Position, target.Rotation, 1f);
        var movement = card.GetComponent<CardMovementController>();
        if (movement != null)
            movement.SnapTo(visualTarget);
    }
}

// ==============================================================================================
// CORE - Gerenciador de Reorganização Manual
// ==============================================================================================

/// <summary>
/// Orquestra o sistema de reorganização manual de cartas
/// Responsabilidades:
/// - Detectar quando uma carta é pegada
/// - Resolver para qual slot ela deve ir
/// - Animar a reorganização
/// - Disparar eventos
/// 
/// Totalmente desacoplado via Strategy + Factory
/// </summary>
public class HandManualReorganizationManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private SlotResolutionStrategy _slotResolutionStrategy = SlotResolutionStrategy.Proximity;
    [SerializeField] private AnimationStrategy _animationStrategy = AnimationStrategy.Smooth;

    [Header("Referências")]
    [SerializeField] private HandManager _handManager;
    [SerializeField] private HandLayoutConfig _layoutConfig;

    // Componentes internos
    private IHandSlotResolver _slotResolver;
    private IReorganizationAnimationStrategy _animationStrategy_impl;
    private HandReorganizationEvents _events;

    // Estado
    private CardView _currentlyDraggedCard;
    private int _originalSlotIndex;

    // ==============================================================================================
    // CICLO DE VIDA
    // ==============================================================================================

    private void Awake()
    {
        InitializeStrategies();
        _events = new HandReorganizationEvents();
    }

    private void Start()
    {
        if (_handManager == null)
            _handManager = GetComponentInParent<HandManager>();

        // Subscribe aos eventos de adição/remoção de cartas
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Player.OnCardAdded += OnCardAdded;
            AppCore.Instance.Events.Player.OnCardRemoved += OnCardRemoved;
        }

        // Subscribe inicial às cartas já existentes
        SubscribeToCardDragEvents();
    }

    private void OnDestroy()
    {
        // Unsubscribe dos eventos globais
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Player.OnCardAdded -= OnCardAdded;
            AppCore.Instance.Events.Player.OnCardRemoved -= OnCardRemoved;
        }

        // Unsubscribe das cartas
        UnsubscribeFromCardDragEvents();
    }

    // ==============================================================================================
    // DINÂMICO: Reage quando cartas são adicionadas/removidas
    // ==============================================================================================

    private void OnCardAdded(CardInstance instance)
    {
        // Aguarda a carta ser criada e adicionada à lista
        // (o timing é garantido porque a mensagem OnCardAdded é disparada DEPOIS da criação)
        SubscribeToCardDragEvents();
    }

    private void OnCardRemoved(CardInstance instance)
    {
        // Remove inscrições da carta que saiu
        SubscribeToCardDragEvents();
    }

    // ==============================================================================================
    // ESTRATÉGIAS
    // ==============================================================================================

    private void InitializeStrategies()
    {
        _slotResolver = HandReorganizationFactory.CreateSlotResolver(_slotResolutionStrategy);
        _animationStrategy_impl = HandReorganizationFactory.CreateAnimationStrategy(_animationStrategy);
    }

    // ==============================================================================================
    // EVENTOS DE DRAG (Dinâmico)
    // ==============================================================================================

    private void SubscribeToCardDragEvents()
    {
        // Primeiro, remove inscrições antigas (evita duplicação)
        UnsubscribeFromCardDragEvents();

        // Depois, se inscreve nas cartas atuais
        var cards = _handManager.GetActiveCards();
        foreach (var card in cards)
        {
            card.OnDragStartEvent += OnCardDragStart;
            card.OnDragEndEvent += OnCardDragEnd;
        }
    }

    private void UnsubscribeFromCardDragEvents()
    {
        var cards = _handManager.GetActiveCards();
        foreach (var card in cards)
        {
            card.OnDragStartEvent -= OnCardDragStart;
            card.OnDragEndEvent -= OnCardDragEnd;
        }
    }

    private void OnCardDragStart(CardView card)
    {
        _currentlyDraggedCard = card;
        var allCards = _handManager.GetActiveCards();
        _originalSlotIndex = allCards.IndexOf(card);

        _events.TriggerCardPickedUp(card, card.transform.position);
    }

    private void OnCardDragEnd(CardView card)
    {
        if (_currentlyDraggedCard != card) return;

        var allCards = _handManager.GetActiveCards();
        if (allCards.Count == 0) return;

        // Resolve para qual slot ela deve ir (APENAS dados abstratos)
        int newSlotIndex = _slotResolver.ResolveSlot(
            card.transform.position,
            allCards.Count,
            _layoutConfig
        );

        // Se mudou de posição, reordena via HandManager (encapsulation)
        if (newSlotIndex != _originalSlotIndex)
        {
            _handManager.ReorderCard(_originalSlotIndex, newSlotIndex);
            AnimateAllCards(allCards);
        }
        else
        {
            // Se não mudou, volta para a posição original
            _animationStrategy_impl.AnimateCardToSlot(
                card,
                _originalSlotIndex,
                allCards.Count,
                _layoutConfig,
                _handManager.GetHandCenterPosition()
            );
        }

        _events.TriggerCardDropped(card);
        _currentlyDraggedCard = null;
    }

    // ==============================================================================================
    // LÓGICA DE REORGANIZAÇÃO
    // ==============================================================================================

    private void AnimateAllCards(List<CardView> allCards)
    {
        // Anima todas as cartas para suas novas posições
        for (int i = 0; i < allCards.Count; i++)
        {
            _animationStrategy_impl.AnimateCardToSlot(
                allCards[i],
                i,
                allCards.Count,
                _layoutConfig,
                _handManager.GetHandCenterPosition()
            );

            _events.TriggerCardRepositioned(allCards[i], i);
        }

        _events.TriggerHandReorganized();
    }

    // ==============================================================================================
    // API PÚBLICA
    // ==============================================================================================

    /// <summary>
    /// Retorna interface de leitura para eventos (só listeners)
    /// </summary>
    public IHandReorganizationEventsReader Events => _events;
}
