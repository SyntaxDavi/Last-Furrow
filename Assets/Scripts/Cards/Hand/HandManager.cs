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
            CreateCardView(instance);
        }

        // Marca para calcular no próximo frame
        _isLayoutDirty = true;
    }

    private void CreateCardView(CardInstance instance)
    {
        if (!_library.TryGetCard(instance.TemplateID, out CardData data)) return;

        var newCard = Instantiate(_cardPrefab, transform);

        // Injeção de Dependências
        newCard.Initialize(
            data,
            instance,
            _visualConfig,
            AppCore.Instance.InputManager
        );

        // Posição inicial (Spawn point)
        newCard.transform.position = _handCenter.position - Vector3.up * 6f;

        // Assinatura de Eventos
        newCard.OnDragStartEvent += OnCardDragStart;
        newCard.OnDragEndEvent += OnCardDragEnd;
        newCard.OnClickEvent += HandleCardClicked;

        _activeCards.Add(newCard);

        // A mão mudou, precisa recalcular
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

    private void HandleCardAdded(CardInstance instance) => CreateCardView(instance);

    private void HandleCardRemoved(CardInstance instance)
    {
        var cardView = _activeCards.FirstOrDefault(c => c.Instance.UniqueID == instance.UniqueID);
        if (cardView != null)
        {
            RemoveCardView(cardView);
        }
    }

    private void RemoveCardView(CardView card)
    {
        if (_activeCards.Contains(card))
        {
            // EDGE CASE FIX: Se a carta removida estava selecionada, avisa o sistema!
            // Isso evita que a loja tente vender uma carta que não existe mais visualmente.
            if (card.CurrentState == CardVisualState.Selected)
            {
                // Dispara evento com null para limpar UI de detalhes/loja
                AppCore.Instance.Events.Player.TriggerCardClicked(null);
            }

            // Remove listeners para evitar memory leaks
            card.OnClickEvent -= HandleCardClicked;
            card.OnDragStartEvent -= OnCardDragStart;
            card.OnDragEndEvent -= OnCardDragEnd;

            _activeCards.Remove(card);
            Destroy(card.gameObject);

            // A mão mudou, precisa recalcular o layout das que sobraram
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