using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HandManager : MonoBehaviour
{
    [Header("Data Configs")]
    [SerializeField] private HandLayoutConfig _layoutConfig;
    [SerializeField] private CardVisualConfig _visualConfig; // Novo config visual

    [Header("Scene Refs")]
    [SerializeField] private CardView _cardPrefab;
    [SerializeField] private Transform _handCenter;

    // Runtime State
    private List<CardView> _activeCards = new List<CardView>();
    private RunData _runData;
    private IGameLibrary _library;

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
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Player.OnCardAdded -= HandleCardAdded;
            AppCore.Instance.Events.Player.OnCardRemoved -= HandleCardRemoved;
        }
    }

    private void Update()
    {
        // Roda sempre. Garante que se uma carta for removida ou arrastada,
        // as outras reajam suavemente (Juicy) sem precisar de flags complexas.
        if (_activeCards.Count > 0)
        {
            UpdateHandLayout();
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
    }

    private void CreateCardView(CardInstance instance)
    {
        if (!_library.TryGetCard(instance.TemplateID, out CardData data)) return;

        var newCard = Instantiate(_cardPrefab, transform);

        // Injeta Dependências
        newCard.Initialize(data, instance, _visualConfig);

        // Posição inicial (Spawn point fora da tela)
        newCard.transform.position = _handCenter.position - Vector3.up * 6f;

        // Assina Eventos Locais (Callback pattern)
        newCard.OnDragStartEvent += OnCardDragStart;
        newCard.OnDragEndEvent += OnCardDragEnd;
        newCard.OnClickEvent += OnCardClicked;

        _activeCards.Add(newCard);
    }

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
            // Remove Eventos antes de destruir (Safety)
            card.OnDragStartEvent -= OnCardDragStart;
            card.OnDragEndEvent -= OnCardDragEnd;
            card.OnClickEvent -= OnCardClicked;

            _activeCards.Remove(card);
            Destroy(card.gameObject);
        }
    }

    private void ClearHand()
    {
        // Itera de trás pra frente ou cria cópia da lista para destruição segura
        var cleanupList = new List<CardView>(_activeCards);
        foreach (var card in cleanupList)
        {
            RemoveCardView(card);
        }
        _activeCards.Clear();
    }

    // --- LAYOUT ---
    private void UpdateHandLayout()
    {
        int count = _activeCards.Count;

        for (int i = 0; i < count; i++)
        {
            // Pede o cálculo matemático puro (Stateless)
            var targetSlot = HandLayoutCalculator.CalculateSlot(
                i,
                count,
                _layoutConfig,
                _handCenter.position
            );

            // Passa o alvo para a carta (Stateful)
            _activeCards[i].UpdateLayoutTarget(targetSlot);
        }
    }

    // --- EVENT HANDLERS (Callback -> AppCore) ---

    private void OnCardClicked(CardView card)
    {
        Debug.Log($"[HandManager] Clique em {card.Data.Name} detectado. Propagando...");

        // Dispara o evento global para que o ShopView possa ouvir
        AppCore.Instance.Events.Player.TriggerCardClicked(card);
    }

    private void OnCardDragStart(CardView card)
    {
        // Opcional: Efeitos globais na mão (ex: escurecer outras cartas)
    }

    private void OnCardDragEnd(CardView card)
    {
        // Opcional: Resetar efeitos globais
    }
}