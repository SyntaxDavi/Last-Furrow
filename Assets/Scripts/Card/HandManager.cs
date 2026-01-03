using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [Header("Configuração Visual")]
    [SerializeField] private CardView _cardPrefab;
    [SerializeField] private Transform _handCenter;
    [SerializeField] private float _cardSpacing = 2.0f;
    [SerializeField] private float _arcHeight = 0.5f;

    // DEPENDÊNCIAS (Injetadas pelo Bootstrapper)
    private IGameLibrary _library;
    private RunData _runData;

    private List<CardView> _cardsInHand = new List<CardView>();

    // --- INICIALIZAÇÃO ---

    /// <summary>
    /// Configura o HandManager com os dados necessários.
    /// Substitui o Start() e remove a dependência de Singletons para dados.
    /// </summary>
    public void Configure(RunData runData, IGameLibrary library)
    {
        _runData = runData;
        _library = library;

        // Se já tivermos dados, inicializa a mão imediatamente
        if (_runData != null)
        {
            InitializeHandFromRun();
        }
    }

    private void OnEnable()
    {
        // Eventos Globais (Observer Pattern)
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnRunStarted += HandleRunStarted;  
            AppCore.Instance.Events.Player.OnCardConsumed += HandleCardConsumed;
            AppCore.Instance.Events.Player.OnCardAdded += HandleCardAdded;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnRunStarted -= HandleRunStarted;
            AppCore.Instance.Events.Player.OnCardConsumed -= HandleCardConsumed;
            AppCore.Instance.Events.Player.OnCardAdded -= HandleCardAdded;
        }
    }

    // --- LÓGICA DE DADOS ---

    private void HandleRunStarted() => InitializeHandFromRun();

    private void InitializeHandFromRun()
    {
        ClearHand();

        // Garante que usa o dado injetado ou busca do save
        if (_runData == null)
            _runData = AppCore.Instance.SaveManager.Data.CurrentRun;

        if (_runData == null) return;

        // Agora o foreach funciona porque CreateCardVisual aceita 'instance'
        foreach (var instance in _runData.Hand)
        {
            CreateCardVisual(instance);
        }
    }
    private void HandleCardAdded(CardInstance instance)
    {
        CreateCardVisual(instance);
    }

    private void HandleCardConsumed(CardID cardID)
    {
        // 1. Busca Visual
        var cardView = _cardsInHand.Find(c => c.Data.ID == cardID);

        if (cardView != null)
        {
            RemoveCard(cardView);

            if (_runData != null && _runData.Hand != null)
            {
                var instanceToRemove = _runData.Hand.Find(x => x.TemplateID == cardID);

                if (!string.IsNullOrEmpty(instanceToRemove.UniqueID))
                {
                    _runData.Hand.Remove(instanceToRemove);
                    AppCore.Instance.SaveManager.SaveGame();
                }
            }
        }
        else
        {
            Debug.LogWarning("[HandManager] Tentou consumir carta que não estava na mão visualmente.");
        }
    }

    // --- MANIPULAÇÃO VISUAL ---

    public void AddCardToHand(CardID cardID)
    {
        if (_library == null) return;

        // Cria uma instância "falsa" ou nova para satisfazer o método visual
        CardInstance newInstance = new CardInstance(cardID);
        CreateCardVisual(newInstance);
    }

    private void CreateCardVisual(CardInstance instance)
    {
        // Usa o TemplateID da instância para buscar os dados visuais (ícone, nome)
        if (_library.TryGetCard(instance.TemplateID, out CardData data))
        {
            var newCard = Instantiate(_cardPrefab, transform);

            // A CardView (prefab) precisa ser atualizada para aceitar (CardData, CardInstance)
            // Se sua CardView ainda não aceita instance, use: newCard.Initialize(data);
            // Mas o ideal é:
            newCard.Initialize(data,instance); 

            // Hack temporário: Armazena o ID na View para sabermos qual remover depois
            // (Supondo que CardView tenha um campo para isso, senão apenas Data)

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

            // Destroi o objeto Unity
            Destroy(card.gameObject);

            // Reorganiza a mão
            CalculateHandPositions();
        }
    }

    private void ClearHand()
    {
        // Limpeza segura
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

    // --- EVENTOS INTERNOS DA CARTA (DRAG & DROP) ---

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
        // Futuro: Animação de "abrir espaço" na mão
    }

    private void HandleCardDragEnd(CardView card)
    {
        // Recalcula para garantir que a carta volte para a posição correta se não foi consumida
        CalculateHandPositions();
    }

    // --- POSICIONAMENTO (MATH) ---

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

            // Se estiver arrastando, não forçamos a posição, 
            // mas mantemos o índice atualizado
            if (card.IsDragging) continue;

            // Matemática do Arco
            float xOffset = startX + (i * _cardSpacing);

            // Normaliza X entre -1 e 1 para calcular a altura do arco
            float normalizedX = (count > 1) ? (float)i / (count - 1) : 0.5f;
            normalizedX = (normalizedX - 0.5f) * 2; // Range -1 a 1

            float yOffset = -Mathf.Abs(normalizedX) * _arcHeight;

            Vector3 slotPos = _handCenter.position + new Vector3(xOffset, yOffset, 0);

            // Atualiza View
            card.TargetPosition = slotPos;
            card.SetSortingOrder(i * 10);
            card.HandIndex = i;
        }
    }
}