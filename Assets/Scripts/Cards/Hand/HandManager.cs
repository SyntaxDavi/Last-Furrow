using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [Header("Configuração Visual")]
    [SerializeField] private CardView _cardPrefab;
    [SerializeField] private Transform _handCenter;

    [Header("Matemática do Leque")]
    [Tooltip("Espaçamento ideal entre cartas")]
    [SerializeField] private float _cardSpacing = 2.0f;

    [Tooltip("Largura MÁXIMA que a mão pode ocupar na tela antes de começar a espremer")]
    [SerializeField] private float _maxHandWidth = 12.0f;

    [Tooltip("Altura do arco (quanto a carta central sobe)")]
    [SerializeField] private float _arcHeight = 0.5f;

    [Tooltip("Rotação das cartas nas pontas (em graus)")]
    [SerializeField] private float _rotationIntensity = 15.0f; // Aumente para girar mais

    // DEPENDÊNCIAS
    private IGameLibrary _library;
    private RunData _runData;

    private List<CardView> _cardsInHand = new List<CardView>();

    // --- INICIALIZAÇÃO ---

    public void Configure(RunData runData, IGameLibrary library)
    {
        _runData = runData;
        _library = library;

        if (_runData != null)
        {
            InitializeHandFromRun();
        }
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnRunStarted += HandleRunStarted;
            AppCore.Instance.Events.Player.OnCardAdded += HandleCardAdded;
            AppCore.Instance.Events.Player.OnCardRemoved += HandleCardRemoved;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnRunStarted -= HandleRunStarted;
            AppCore.Instance.Events.Player.OnCardAdded -= HandleCardAdded;
            AppCore.Instance.Events.Player.OnCardRemoved -= HandleCardRemoved;
        }
    }

    // --- LÓGICA DE DADOS ---

    private void HandleRunStarted() => InitializeHandFromRun();

    private void InitializeHandFromRun()
    {
        ClearHand();

        if (_runData == null)
            _runData = AppCore.Instance.SaveManager.Data.CurrentRun;

        if (_runData == null) return;

        foreach (var instance in _runData.Hand)
        {
            CreateCardVisual(instance);
        }
    }

    private void HandleCardAdded(CardInstance instance)
    {
        CreateCardVisual(instance);
    }

    private void HandleCardRemoved(CardInstance instance)
    {
        // Remoção cirúrgica pelo GUID
        var cardView = _cardsInHand.Find(view => view.Instance.UniqueID == instance.UniqueID);

        if (cardView != null)
        {
            RemoveCard(cardView);
        }
        else
        {
            Debug.LogWarning("[HandManager] Dessincronia visual detectada. Recriando mão.");
            InitializeHandFromRun();
        }
    }

    // --- MANIPULAÇÃO VISUAL ---

    private void CreateCardVisual(CardInstance instance)
    {
        if (_library.TryGetCard(instance.TemplateID, out CardData data))
        {
            var newCard = Instantiate(_cardPrefab, transform);
            newCard.Initialize(data, instance);

            // Posição inicial fora da tela (efeito de entrada)
            newCard.transform.position = _handCenter.position - Vector3.up * 8f;

            RegisterCardEvents(newCard);
            _cardsInHand.Add(newCard);

            CalculateHandPositions();
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

    // --- EVENTOS INTERNOS (Drag) ---

    private void RegisterCardEvents(CardView card)
    {
        // card.OnDragStartedEvent += HandleCardDragStart; // Opcional
        card.OnDragEndedEvent += HandleCardDragEnd;
    }

    private void UnregisterCardEvents(CardView card)
    {
        // card.OnDragStartedEvent -= HandleCardDragStart;
        card.OnDragEndedEvent -= HandleCardDragEnd;
    }

    private void HandleCardDragEnd(CardView card)
    {
        // Se soltou a carta e ela não foi consumida, volta pro lugar
        CalculateHandPositions();
    }

    // --- POSICIONAMENTO  ---

    private void CalculateHandPositions()
    {
        int count = _cardsInHand.Count;
        if (count == 0) return;

        // 1. Calcula a largura que elas QUEREM ocupar
        float desiredWidth = (count - 1) * _cardSpacing;

        // 2. Limita a largura ao máximo da tela (Efeito Sanfona)
        float actualWidth = Mathf.Min(desiredWidth, _maxHandWidth);

        // 3. Recalcula o espaçamento real baseado na largura limitada
        // Se count > 1, divide o espaço. Se count = 1, spacing é 0.
        float currentSpacing = (count > 1) ? actualWidth / (count - 1) : 0;

        // Começa da esquerda
        float startX = -actualWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            CardView card = _cardsInHand[i];
            if (card == null) continue;

            if (card.IsDragging) continue;

            // --- CÁLCULO DE POSIÇÃO (Arco) ---
            float xOffset = startX + (i * currentSpacing);

            // Normaliza posição entre 0 e 1 (0=Esq, 0.5=Meio, 1=Dir)
            float normalizedPos = (count > 1) ? (float)i / (count - 1) : 0.5f;

            // Transforma 0..1 em -1..1 para calcular parábola
            float arcX = (normalizedPos - 0.5f) * 2f;

            // Fórmula da parábola invertida (y = -x^2)
            float yOffset = -Mathf.Abs(arcX * arcX) * _arcHeight;

            // --- CÁLCULO DE ROTAÇÃO (Leque) ---
            // Gira negativo na direita, positivo na esquerda
            float rotationZ = -arcX * _rotationIntensity;

            // Aplica
            Vector3 slotPos = _handCenter.position + new Vector3(xOffset, yOffset, 0);

            card.TargetPosition = slotPos;
            card.TargetRotation = Quaternion.Euler(0, 0, rotationZ);

            // Render Order: Cartas da direita ficam por cima das da esquerda? 
            // Ou o centro por cima? Geralmente: direita por cima.
            card.SetSortingOrder(i * 5);
            card.HandIndex = i;
        }
    }
}