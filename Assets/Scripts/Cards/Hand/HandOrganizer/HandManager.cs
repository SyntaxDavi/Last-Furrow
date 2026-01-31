    using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gerencia as cartas na mao do jogador.
/// Coordena spawn, layout, selecao e eventos de cartas.
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private HandLayoutConfig _layoutConfig;
    [SerializeField] private CardVisualConfig _visualConfig;

    [Header("Scene Refs")]
    [SerializeField] private CardView _cardPrefab;
    [SerializeField] private Transform _handCenter;

    [Header("Organizacao")]
    [SerializeField] private HandOrganizer _handOrganizer;
    
    [Header("Fan Controller")]
    [SerializeField] private HandFanController _fanController;
    
    [Header("Hover Controller")]
    [SerializeField] private HandHoverController _hoverController;

    // Eventos
    public event System.Action OnHandLayoutChanged;
    public event System.Action<int> OnCardVisuallySpawned; // int = indice na sequencia de spawn (0, 1, 2...)
    public event System.Action OnCardVisuallySelected;
    public event System.Action OnCardVisuallyHovered;
    public event System.Action OnHandFullyElevated;
    public event System.Action OnHandFullyLowered;
    public event System.Action OnCardVisuallyReordered;
    public event System.Action OnCardVisuallyDragged;
    public event System.Action OnCardVisuallyPlayed;

    // Runtime State
    private List<CardView> _activeCards = new List<CardView>();
    private RunData _runData;
    private IGameLibrary _library;

    // Flags de Controle
    private bool _isLayoutDirty = false;
    private Queue<CardInstance> _pendingCards = new Queue<CardInstance>();
    private bool _isSpawning = false;

    // --- SETUP ---
    private void Awake()
    {
        // Inicializa o HandOrganizer se estiver presente
        if (_handOrganizer != null)
        {
            _handOrganizer.Initialize(this);
        }
        
        // Inicializa o FanController se estiver presente
        if (_fanController != null)
        {
            _fanController.Initialize(this);
        }
        
        // Inicializa o HoverController se estiver presente
        if (_hoverController != null)
        {
            _hoverController.Initialize(this);
        }
    }

    public void Configure(RunData runData, IGameLibrary library)
    {
        _runData = runData;
        _library = library;
        InitializeHandFromRun();
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Player.OnCardAdded += HandleCardAdded;
            AppCore.Instance.Events.Player.OnCardRemoved += HandleCardRemoved;
            AppCore.Instance.InputManager.OnPrimaryClick += HandleGlobalClick;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Player.OnCardAdded -= HandleCardAdded;
            AppCore.Instance.Events.Player.OnCardRemoved -= HandleCardRemoved;
            AppCore.Instance.InputManager.OnPrimaryClick -= HandleGlobalClick;
        }
    }

    private void Update()
    {
        // Se a mao mudou, recalculamos APENAS os alvos (Targets).
        // A fisica das cartas (SmoothDamp) roda independente no Update delas.
        if (_isLayoutDirty)
        {
            RecalculateLayoutTargets();
            _isLayoutDirty = false;
            OnHandLayoutChanged?.Invoke();
            
            // Only play reorder sound if cards are actually present and it wasn't a spawn sequence
            if (!_isSpawning && _activeCards.Count > 0)
            {
                OnCardVisuallyReordered?.Invoke();
            }
        }
    }

    // --- LOGICA DE SPAWN (O "Fan Out" acontece aqui) ---

    private void InitializeHandFromRun()
    {
        ClearHand();
        if (_runData?.Hand == null) return;

        foreach (var instance in _runData.Hand)
        {
            _pendingCards.Enqueue(instance);
        }

        if (!_isSpawning) StartCoroutine(ProcessCardQueue());
    }

    private System.Collections.IEnumerator ProcessCardQueue()
    {
        _isSpawning = true;
        int sequenceIndex = 0; // Para efeitos visuais/sonoros em sequencia

        while (_pendingCards.Count > 0)
        {
            var instance = _pendingCards.Dequeue();
            CreateCardView(instance);

            OnCardVisuallySpawned?.Invoke(sequenceIndex);
            sequenceIndex++;

            // O "Fan Out" visual e criado por este delay.
            // A carta nasce no Deck e corre para a mao suavemente.
            yield return new WaitForSeconds(_layoutConfig.SpawnDelay);
        }

        _isSpawning = false;
    }

    private void CreateCardView(CardInstance instance)
    {
        if (!_library.TryGetCard(instance.TemplateID, out CardData data)) return;

        var newCard = Instantiate(_cardPrefab, transform);
        newCard.Initialize(
            data,
            instance,
            _visualConfig,
            AppCore.Instance.InputManager,
            FindFirstObjectByType<PlayerInteraction>()?.DragSystem   
        );

        Vector3 spawnPos = _handCenter.position + (Vector3)_layoutConfig.SpawnOffset;
        spawnPos.z = _visualConfig.IdleZ;

        newCard.transform.position = spawnPos;

        newCard.OnDragStartEvent += OnCardDragStart;
        newCard.OnDragEndEvent += OnCardDragEnd;
        newCard.OnClickEvent += HandleCardClicked;
        newCard.OnHoverEnterEvent += HandleCardHovered;

        _activeCards.Add(newCard);
        int newIndex = _activeCards.Count - 1;
        var targetSlot = HandLayoutCalculator.CalculateSlot(newIndex, _activeCards.Count, _layoutConfig, _handCenter.position);
        newCard.UpdateLayoutTarget(targetSlot);

        _isLayoutDirty = true;
    }

    private void RecalculateLayoutTargets()
    {
        int count = _activeCards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var targetSlot = HandLayoutCalculator.CalculateSlot(
                i,
                count,
                _layoutConfig,
                _handCenter.position
            );
            _activeCards[i].UpdateLayoutTarget(targetSlot);
        }
    }


    private void HandleCardClicked(CardView clickedCard)
    {
        if (clickedCard.CurrentState == CardVisualState.Selected)
        {
            clickedCard.Deselect();
            AppCore.Instance.Events.Player.TriggerCardClicked(null);
        }
        else
        {
            clickedCard.Select();
            foreach (var card in _activeCards) if (card != clickedCard) card.Deselect();
            AppCore.Instance.Events.Player.TriggerCardClicked(clickedCard);
            OnCardVisuallySelected?.Invoke();
        }
    }
    
    private void HandleCardHovered() => OnCardVisuallyHovered?.Invoke();
    
    public void TriggerHandFullyElevated() => OnHandFullyElevated?.Invoke();
    public void TriggerHandFullyLowered() => OnHandFullyLowered?.Invoke();
    public void TriggerCardVisuallySpawned(int sequenceIndex) => OnCardVisuallySpawned?.Invoke(sequenceIndex);

    private void HandleGlobalClick()
    {
        if (_activeCards.Count == 0) return;
        bool clickedOnACard = _activeCards.Any(card => card.IsHovered);
        if (!clickedOnACard) DeselectAllCards();
    }

    private void DeselectAllCards()
    {
        bool anyChanged = false;
        foreach (var card in _activeCards)
        {
            if (card.CurrentState == CardVisualState.Selected)
            {
                card.Deselect();
                anyChanged = true;
            }
        }
        if (anyChanged) AppCore.Instance.Events.Player.TriggerCardClicked(null);
    }

    private void HandleCardAdded(CardInstance instance)
    {
        _pendingCards.Enqueue(instance);
        if (!_isSpawning) StartCoroutine(ProcessCardQueue());
    }

    private void HandleCardRemoved(CardInstance instance)
    {
        var cardView = _activeCards.FirstOrDefault(c => c.Instance.UniqueID == instance.UniqueID);
        if (cardView != null)
        {
            OnCardVisuallyPlayed?.Invoke();
            RemoveCardView(cardView);
        }

        if (_runData?.Hand != null)
            _runData.Hand.RemoveAll(c => c.UniqueID == instance.UniqueID);
    }

    private void RemoveCardView(CardView card)
    {
        if (_activeCards.Contains(card))
        {
            card.OnClickEvent -= HandleCardClicked;
            card.OnHoverEnterEvent -= HandleCardHovered;
            card.OnDragStartEvent -= OnCardDragStart;
            card.OnDragEndEvent -= OnCardDragEnd;

            if (card.CurrentState == CardVisualState.Selected)
                AppCore.Instance.Events.Player.TriggerCardClicked(null);

            _activeCards.Remove(card);
                card.PlayUseAnimation();
                _isLayoutDirty = true;
        }
    }

    private void ClearHand()
    {
        var cleanupList = new List<CardView>(_activeCards);
        foreach (var card in cleanupList) RemoveCardView(card);
        _activeCards.Clear();
        _isLayoutDirty = false;
        _pendingCards.Clear();
        _isSpawning = false;
        OnHandLayoutChanged?.Invoke();
    }

    private void OnCardDragStart(CardView card)
    {
        OnCardVisuallyDragged?.Invoke();
        
        // Abaixa todas as outras cartas imediatamente
        _hoverController?.ForceElevation(false);
        
        foreach (var c in _activeCards)
        {
            if (c != card && c.CurrentState == CardVisualState.Selected) c.Deselect();
        }
    }

    private void OnCardDragEnd(CardView card) 
    { 
        // Invalida o cache do hover controller para forçar reavaliação
        // Isso garante que as cartas reajam à posição atual do mouse após soltar
        _hoverController?.InvalidateBoundsCache();
    }

    // ==========================================================================
    // API PUBLICA (Para HandOrganizer, HandHoverController e outros)
    // ==========================================================================

    /// <summary>
    /// Verifica se alguma carta está sendo arrastada no momento.
    /// </summary>

    /// <summary>
    /// Força todas as cartas a saírem do estado de Drag.
    /// Usado quando uma fase de resolução começa abruptamente.
    /// </summary>
    public void ForceReleaseAllDrags()
    {
        foreach (var card in _activeCards)
        {
            if (card.CurrentState == CardVisualState.Dragging)
            {
                card.OnDragEnd();
            }
        }
        DeselectAllCards();
    }

    public bool IsDraggingAnyCard => _activeCards.Any(c => c.CurrentState == CardVisualState.Dragging);
    
    /// <summary>
    /// Retorna uma copia da lista de cartas ativas (para modificacao segura)
    /// </summary>
    public List<CardView> GetActiveCards()
    {
        return new List<CardView>(_activeCards);
    }
    
    /// <summary>
    /// Retorna lista somente-leitura das cartas ativas (mais eficiente para iteracao)
    /// </summary>
    public IReadOnlyList<CardView> GetActiveCardsReadOnly()
    {
        return _activeCards.AsReadOnly();
    }

    /// <summary>
    /// Retorna a posicao do centro da mao
    /// </summary>
    public Vector3 GetHandCenterPosition()
    {
        return _handCenter?.position ?? Vector3.zero;
    }

    /// <summary>
    /// Retorna o HandOrganizer (caso queira usar de fora)
    /// </summary>
    public HandOrganizer GetOrganizer()
    {
        return _handOrganizer;
    }
    
    /// <summary>
    /// Retorna o HandHoverController
    /// </summary>
    public HandHoverController GetHoverController()
    {
        return _hoverController;
    }
    
    /// <summary>
    /// Retorna o HandFanController
    /// </summary>
    public HandFanController GetFanController()
    {
        return _fanController;
    }
    
    /// <summary>
    /// Retorna o CardVisualConfig (usado por controllers externos)
    /// </summary>
    public CardVisualConfig GetVisualConfig()
    {
        return _visualConfig;
    }
    
    /// <summary>
    /// Retorna o HandLayoutConfig (usado por controllers externos)
    /// </summary>
    public HandLayoutConfig GetLayoutConfig()
    {
        return _layoutConfig;
    }

    /// <summary>
    /// Reordena internamente a lista de cartas ativas.
    /// Usado pelo HandOrganizer para manter a ordem apos shuffle/sort.
    /// </summary>
    public void ReorderActiveCards(List<CardView> newOrder)
    {
        if (newOrder == null || newOrder.Count != _activeCards.Count) return;

        _activeCards.Clear();
        _activeCards.AddRange(newOrder);
        
        // Marca para recalcular no Update, que disparará o OnHandLayoutChanged após os alvos mudarem
        _isLayoutDirty = true;
    }

}
