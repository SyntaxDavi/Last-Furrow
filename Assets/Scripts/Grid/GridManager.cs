using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Configuração Visual")]
    [SerializeField] private GridSlotView _slotPrefab;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 1.1f);

    // O CÉREBRO (Lógica Pura)
    private IGridService _gridService;

    // A CACHE VISUAL (Para atualizações rápidas)
    private List<GridSlotView> _spawnedSlots = new List<GridSlotView>();

    // --- INICIALIZAÇÃO (Injeção de Dependência) ---

    /// <summary>
    /// Chamado pelo AppCore (Composition Root).
    /// Configura o Grid com um serviço de lógica específico.
    /// </summary>
    public void Configure(IGridService service)
    {
        if (_gridService != null)
        {
            // Se já existia, desinscreve para evitar vazamento de memória
            _gridService.OnSlotStateChanged -= RefreshSingleSlot;
        }

        _gridService = service;

        // Escuta mudanças nos dados para atualizar a tela
        _gridService.OnSlotStateChanged += RefreshSingleSlot;

        // Gera o grid visual
        GenerateGrid();

        // Atualiza estado inicial
        RefreshAllSlots();
    }

    private void Start()
    {
        // SAFETY CHECK: Se ninguém configurou (ex: cena de teste sem AppCore), tenta rodar sozinho?
        // Na arquitetura profissional, preferimos falhar ou avisar se a dependência não for injetada.
        if (_gridService == null)
        {
            Debug.LogWarning("[GridManager] Inicializado sem Serviço! Aguardando chamada de Configure().");
        }
    }

    private void OnDestroy()
    {
        if (_gridService != null)
        {
            _gridService.OnSlotStateChanged -= RefreshSingleSlot;
        }
    }

    // --- GERAÇÃO DO GRID E BINDING (A "Cola") ---

    private void GenerateGrid()
    {
        // 1. Limpeza
        foreach (var slot in _spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        _spawnedSlots.Clear();

        foreach (Transform child in _gridContainer)
            Destroy(child.gameObject);

        // 2. Criação
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;

            var newSlot = Instantiate(_slotPrefab, _gridContainer);

            float xPos = (col - 1) * _spacing.x;
            float yPos = (1 - row) * _spacing.y;
            newSlot.transform.localPosition = new Vector2(xPos, yPos);
            newSlot.name = $"Slot_{i}";

            // 3. BINDING (A Mágica acontece aqui)
            // Conectamos os eventos da View diretamente na lógica do Service
            newSlot.Initialize(i);

            // View pergunta: "Posso aceitar isso?" -> Controller pergunta ao Service
            newSlot.OnCheckDropInteraction += (index, card) => _gridService.CanReceiveCard(index, card);

            // View avisa: "Recebi isso!" -> Controller manda Service aplicar
            newSlot.OnDropInteraction += HandleDropInteraction;

            _spawnedSlots.Add(newSlot);
        }
    }

    // --- MANIPULAÇÃO DE INTERAÇÃO ---

    private void HandleDropInteraction(int index, CardData card)
    {
        // Tenta aplicar a lógica
        InteractionResult result = _gridService.ApplyCard(index, card);

        if (result.Success)
        {
            // Sucesso Lógico: O visual do slot será atualizado via evento OnSlotStateChanged.
            // Aqui cuidamos do feedback "extra-grid" (UI, Som, Partículas)

            // Ex: Notificar sistema de Mão para consumir a carta visualmente
            AppCore.Instance.Events.TriggerCardConsumed(card.ID);

            // Ex: Tocar som
            // AudioManager.Play("PlantSound");
        }
        else
        {
            // Falha: Feedback visual de erro
            Debug.Log($"[GridManager] Ação falhou: {result.Message}");
            // Ex: Mostrar popup de erro ou som de "Negado"
        }
    }

    // --- ATUALIZAÇÃO VISUAL (Tradução Model -> View) ---

    public void RefreshAllSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            RefreshSingleSlot(i);
        }
    }

    private void RefreshSingleSlot(int index)
    {
        if (index < 0 || index >= _spawnedSlots.Count) return;

        // 1. Busca dados puros (somente leitura)
        var state = _gridService.GetSlotReadOnly(index);
        var view = _spawnedSlots[index];

        // 2. Tradução (ID -> Sprite)
        Sprite spriteToRender = null;
        bool isWatered = false;

        if (state != null)
        {
            isWatered = state.IsWatered;

            if (!string.IsNullOrEmpty(state.CropID))
            {
                // Acesso ao GameLibrary (Singleton de Assets é aceitável em Controllers)
                if (GameLibrary.Instance != null)
                {
                    CropData data = GameLibrary.Instance.GetCrop(state.CropID);
                    if (data != null)
                    {
                        spriteToRender = data.GetSpriteForStage(state.CurrentGrowth);
                    }
                }
            }
        }

        // 3. Atualiza View Passiva
        view.SetVisualState(spriteToRender, isWatered);
    }
}