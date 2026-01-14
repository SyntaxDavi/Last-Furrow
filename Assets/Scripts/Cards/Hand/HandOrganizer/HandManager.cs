using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HandManager : MonoBehaviour
{
    [Header("Data Configs")]
    [SerializeField] private HandLayoutConfig _layoutConfig;
    [SerializeField] private CardVisualConfig _visualConfig; 

    [Header("Scene Refs")]
    [SerializeField] private CardView _cardPrefab;
    [SerializeField] private Transform _handCenter;

    [Header("Organização")]
    [SerializeField] private HandOrganizer _handOrganizer;

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
        // Se a mão mudou, recalculamos APENAS os alvos (Targets).
        // A física das cartas (SmoothDamp) roda independente no Update delas.
        if (_isLayoutDirty)
        {
            RecalculateLayoutTargets();
            _isLayoutDirty = false;
        }
    }

    // --- LÓGICA DE SPAWN (O "Fan Out" acontece aqui) ---

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

        while (_pendingCards.Count > 0)
        {
            var instance = _pendingCards.Dequeue();
            CreateCardView(instance);

            // O "Fan Out" visual é criado por este delay.
            // A carta nasce no Deck e corre para a mão suavemente.
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
            AppCore.Instance.InputManager
        );

        Vector3 spawnPos = _handCenter.position + (Vector3)_layoutConfig.SpawnOffset;
        spawnPos.z = _visualConfig.IdleZ;

        newCard.transform.position = spawnPos;

        newCard.OnDragStartEvent += OnCardDragStart;
        newCard.OnDragEndEvent += OnCardDragEnd;
        newCard.OnClickEvent += HandleCardClicked;

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
        }
    }

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
        if (cardView != null) RemoveCardView(cardView);

        if (_runData?.Hand != null)
            _runData.Hand.RemoveAll(c => c.UniqueID == instance.UniqueID);
    }

    private void RemoveCardView(CardView card)
    {
        if (_activeCards.Contains(card))
        {
            card.OnClickEvent -= HandleCardClicked;
            card.OnDragStartEvent -= OnCardDragStart;
            card.OnDragEndEvent -= OnCardDragEnd;

            if (card.CurrentState == CardVisualState.Selected)
                AppCore.Instance.Events.Player.TriggerCardClicked(null);

            _activeCards.Remove(card);
            Destroy(card.gameObject);
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
    }

    private void OnCardDragStart(CardView card)
    {
        foreach (var c in _activeCards)
        {
            if (c != card && c.CurrentState == CardVisualState.Selected) c.Deselect();
        }
    }

    private void OnCardDragEnd(CardView card) { }

    // ==============================================================================================
    // API PÚBLICA (Para HandOrganizer e outros)
    // ==============================================================================================

    /// <summary>
    /// Retorna uma cópia da lista de cartas ativas
    /// </summary>
    public List<CardView> GetActiveCards()
    {
        return new List<CardView>(_activeCards);
    }

    /// <summary>
    /// Retorna a posição do centro da mão
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
}