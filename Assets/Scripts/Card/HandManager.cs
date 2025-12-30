using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private CardView _cardPrefab;
    [SerializeField] private Transform _handCenter;
    [SerializeField] private float _cardSpacing = 2.0f;
    [SerializeField] private float _arcHeight = 0.5f;

    private List<CardView> _cardsInHand = new List<CardView>();

    private void Start()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.OnRunStarted += OnRunStarted;
            AppCore.Instance.Events.OnCardConsumed += RemoveVisualCard;
            if (AppCore.Instance.RunManager.IsRunActive)
            {
                InitializeHandFromRun();
            }
        }

#if UNITY_EDITOR
        if (!AppCore.Instance.RunManager.IsRunActive)
        {
            for (int i = 0; i < 5; i++) CreateDebugCard(i);
        }
#endif
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.OnRunStarted -= OnRunStarted;
            AppCore.Instance.Events.OnCardConsumed -= RemoveVisualCard;
        }
    }

    // --- EVENT HANDLERS ---

    private void OnRunStarted() => InitializeHandFromRun();

    private void InitializeHandFromRun()
    {
        ClearHand();
        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        if (runData == null) return;

        foreach (string cardID in runData.DeckIDs)
        {
            AddCardToHand(cardID);
        }
    }

    // --- MANIPULAÇÃO DA MÃO ---

    public void AddCardToHand(string cardID)
    {
        if (GameLibrary.Instance == null) return;
        CardData data = GameLibrary.Instance.GetCard(cardID);
        if (data == null) return;

        var newCard = Instantiate(_cardPrefab, transform);
        newCard.Initialize(data);

        // Posição inicial (spawn fora da tela)
        newCard.transform.position = _handCenter.position - Vector3.up * 5f;

        RegisterCardEvents(newCard); 

        _cardsInHand.Add(newCard);

        CalculateHandPositions();
    }
    private void RemoveVisualCard(string cardID)
    {
        // Procura na lista de _cardsInHand
        for (int i = 0; i < _cardsInHand.Count; i++)
        {
            if (_cardsInHand[i].Data.ID == cardID)
            {
                var cardToRemove = _cardsInHand[i];
                RemoveCard(cardToRemove); // Seu método existente que destrói o objeto
                return; // Remove só uma!
            }
        }
    }
    public void RemoveCard(CardView card)
    {
        if (_cardsInHand.Contains(card))
        {
            UnregisterCardEvents(card); 
            _cardsInHand.Remove(card);
            Destroy(card.gameObject);

            CalculateHandPositions();
        }
    }

    private void ClearHand()
    {
        foreach (var card in _cardsInHand)
        {
            if (card != null)
            {
                UnregisterCardEvents(card);
                Destroy(card.gameObject);
            }
        }
        _cardsInHand.Clear();
    }

    // --- EVENTOS DAS CARTAS ---

    private void RegisterCardEvents(CardView card)
    {
        card.OnDragStartedEvent += HandleCardDragStart;
        card.OnDragEndedEvent += HandleCardDragEnd;
    }

    private void UnregisterCardEvents(CardView card)
    {
        card.OnDragStartedEvent -= HandleCardDragStart;
        card.OnDragEndedEvent -= HandleCardDragEnd;
    }

    private void HandleCardDragStart(CardView card)
    {
        // Aqui você poderia fazer as outras cartas se afastarem (Hearthstone style).
        // Por enquanto, não fazemos nada, o layout mantém o buraco aberto.
    }

    private void HandleCardDragEnd(CardView card)
    {
        // Quando solta a carta, recalculamos.
        // Isso garante que ela saiba qual é seu TargetPosition original 
        // e restaure o Sorting correto.
        CalculateHandPositions();
    }

    // --- LAYOUT ---

    private void CalculateHandPositions()
    {
        int count = _cardsInHand.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * _cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            CardView card = _cardsInHand[i];
            if (card == null) continue;

            // Se estiver arrastando, ignoramos a atualização de posição,
            // mas mantemos o cálculo de índice/dados se necessário.
            // Nota: O HandleCardDragEnd vai chamar isso aqui de novo quando soltar,
            // garantindo que a carta volte pro lugar.
            if (card.IsDragging) continue;

            float xOffset = startX + (i * _cardSpacing);
            float normalizedX = (count > 1) ? (float)i / (count - 1) : 0.5f;
            normalizedX = (normalizedX - 0.5f) * 2;
            float yOffset = -Mathf.Abs(normalizedX) * _arcHeight;

            Vector3 slotPos = _handCenter.position + new Vector3(xOffset, yOffset, 0);

            card.TargetPosition = slotPos;

            // Define Sorting normal
            card.SetSortingOrder(i * 10);
            card.HandIndex = i;
        }
    }

#if UNITY_EDITOR
    private void CreateDebugCard(int index)
    {
        CardData dummyData = ScriptableObject.CreateInstance<CardData>();
        dummyData.CardName = "Debug " + index;
        dummyData.Cost = index;

        var newCard = Instantiate(_cardPrefab, transform);
        newCard.Initialize(dummyData);
        newCard.transform.position = _handCenter.position;

        RegisterCardEvents(newCard); // Não esquecer de registrar no debug também!

        _cardsInHand.Add(newCard);
        CalculateHandPositions();
    }
#endif
}