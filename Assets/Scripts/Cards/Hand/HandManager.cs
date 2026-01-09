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

    // Runtime State
    private List<CardView> _activeCards = new List<CardView>();
    private RunData _runData;
    private IGameLibrary _library;

    // OTIMIZAÇÃO: Flag para evitar cálculos desnecessários no Update
    private bool _isLayoutDirty = false;
    private Queue<CardInstance> _pendingCards = new Queue<CardInstance>();
    private bool _isSpawning = false;   

    // --- SETUP ---
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
        // CORREÇÃO DE PERFORMANCE:
        // Só recalculamos a matemática do arco se a mão mudou (carta entrou/saiu).
        // As cartas (CardView) continuam interpolando suavemente para seus alvos no Update delas.
        if (_isLayoutDirty)
        {
            RecalculateLayoutTargets();
            _isLayoutDirty = false;
        }
    }

    // --- LÓGICA DE LISTA ---

    private void InitializeHandFromRun()
    {
        ClearHand();
        if (_runData?.Hand == null) return;
        foreach (var instance in _runData.Hand)
        {
            _pendingCards.Enqueue(instance);
        }

        // Inicia o processamento da fila
        if (!_isSpawning)
        {
            StartCoroutine(ProcessCardQueue());
        }

        _isLayoutDirty = true;

    }
    private System.Collections.IEnumerator ProcessCardQueue()
    {
        _isSpawning = true;

        while (_pendingCards.Count > 0)
        {
            var instance = _pendingCards.Dequeue();
            CreateCardView(instance);

            // Espera antes da próxima carta
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

        // --- MUDANÇA AQUI ---
        // Em vez de hardcoded (-6f), usamos o config para definir de onde a carta vem (Deck)
        Vector3 spawnPos = _handCenter.position + (Vector3)_layoutConfig.SpawnOffset;
        spawnPos.z = 0; // Garante que não nasça bugada no Z
        newCard.transform.position = spawnPos;
        // --------------------

        newCard.OnDragStartEvent += OnCardDragStart;
        newCard.OnDragEndEvent += OnCardDragEnd;
        newCard.OnClickEvent += HandleCardClicked;

        _activeCards.Add(newCard);

        // Marca como sujo para que o Update() recalcule o espaço das cartas
        // Isso faz com que as cartas que já estão na mão "abram espaço" para a nova chegando
        _isLayoutDirty = true;
    }

    // --- LÓGICA DE SELEÇÃO (Rádio Button) ---
    private void HandleCardClicked(CardView clickedCard)
    {
        // 1. Lógica Visual Local
        if (clickedCard.CurrentState == CardVisualState.Selected)
        {
            clickedCard.Deselect();
            // Avisa o sistema que NADA está selecionado (passando null ou criando evento de deseleção)
            // Por enquanto, apenas o clique propaga
        }
        else
        {
            clickedCard.Select();
            foreach (var card in _activeCards)
            {
                if (card != clickedCard) card.Deselect();
            }
        }

        // 2. Propagação Global
        AppCore.Instance.Events.Player.TriggerCardClicked(clickedCard);
    }
    private void HandleGlobalClick()
    {
        // Se a mão estiver vazia, ignora
        if (_activeCards.Count == 0) return;

        // VERIFICAÇÃO INTELIGENTE:
        // Se o mouse estiver em cima de QUALQUER carta da minha mão,
        // eu ignoro esse clique global, porque o evento 'OnClick' da própria carta
        // já vai lidar com a seleção/troca.
        bool clickedOnACard = _activeCards.Any(card => card.IsHovered);

        if (!clickedOnACard)
        {
            // Se clicou no "vazio" (chão, céu, UI sem bloqueio), deseleciona tudo.
            DeselectAllCards();
        }
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

        // Se algo foi deselecionado, avisa o sistema global (UI/Loja) que limpou a seleção
        if (anyChanged)
        {
            AppCore.Instance.Events.Player.TriggerCardClicked(null);
        }
    }

    // --- MANIPULAÇÃO DE DADOS ---

    private void HandleCardAdded(CardInstance instance)
    {
        // Adiciona na fila
        _pendingCards.Enqueue(instance);

        // Se não estiver processando, inicia a coroutine
        if (!_isSpawning)
        {
            StartCoroutine(ProcessCardQueue());
        }
    }

    private void HandleCardRemoved(CardInstance instance)
    {
        Debug.Log($"[HandManager] Tentando remover carta {instance.UniqueID}");
        Debug.Log($"[HandManager] Cartas na mão: {_activeCards.Count}");

        var cardView = _activeCards.FirstOrDefault(c => c.Instance.UniqueID == instance.UniqueID);

        if (cardView != null)
        {
            Debug.Log($"[HandManager] Carta encontrada! Removendo {cardView.Data.Name}");
            RemoveCardView(cardView);
            _runData.Hand.Remove(instance);
        }

        Debug.Log($"[HandManager] Após remoção: {_activeCards.Count} cartas");
        Debug.Log($"[HandManager] RunData.Hand agora tem: {_runData.Hand.Count} cartas");
    }

    private void RemoveCardView(CardView card)
    {
        if (_activeCards.Contains(card))
        {
            // 1. Remove listeners PRIMEIRO (evita que a carta morta receba eventos)
            card.OnClickEvent -= HandleCardClicked;
            card.OnDragStartEvent -= OnCardDragStart;
            card.OnDragEndEvent -= OnCardDragEnd;

            // 2. Limpa seleção global SE estava selecionada
            if (card.CurrentState == CardVisualState.Selected)
            {
                // Dispara evento com null para limpar UI de detalhes/loja
                AppCore.Instance.Events.Player.TriggerCardClicked(null);
            }

            // 3. Remove da lista e destrói
            _activeCards.Remove(card);

            _runData.Hand.Remove(card.Instance);

            Destroy(card.gameObject);

            // 4. Marca layout como sujo
            _isLayoutDirty = true;
        }
    }

    private void ClearHand()
    {
        var cleanupList = new List<CardView>(_activeCards);
        foreach (var card in cleanupList)
        {
            RemoveCardView(card);
        }
        _activeCards.Clear();
        _isLayoutDirty = false; // Mão vazia não precisa de cálculo
        _pendingCards.Clear();
        _isSpawning = false;
    }

    // --- LAYOUT (Matemática) ---

    // Renomeado para deixar claro que isso apenas define os ALVOS, não move os objetos
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

            // Apenas atualiza o "destino" da carta.
            // O CardView.Update() vai interpolar suavemente até lá.
            _activeCards[i].UpdateLayoutTarget(targetSlot);
        }
    }

    // --- EFEITOS DE DRAG ---

    private void OnCardDragStart(CardView card)
    {
        // UX: Ao arrastar, limpa seleções para focar na ação
        foreach (var c in _activeCards)
        {
            if (c != card && c.CurrentState == CardVisualState.Selected)
            {
                c.Deselect();
            }
        }
        // Ao arrastar, não mudamos o layout (buraco fica onde estava ou fecha depois)
        // Se quiser fechar o buraco enquanto arrasta, teria que remover temporariamente da lista de layout.
    }

    private void OnCardDragEnd(CardView card)
    {
        // Se soltou e não foi consumida, volta pro lugar.
        // Como o layout não mudou, o target continua o mesmo.
        // O CardView.OnDragEnd vai setar estado para Idle e ele voltará sozinho.
    }
}