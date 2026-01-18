using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Configuração Visual")]
    [SerializeField] private GridSlotView _slotPrefab;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 1.1f);

    private IGridService _gridService;
    private IGameLibrary _library;
    private List<GridSlotView> _spawnedSlots = new List<GridSlotView>();
    public IGridService Service => _gridService;

    public void Configure(IGridService service, IGameLibrary library)
    {
        if (_gridService != null)
            _gridService.OnSlotStateChanged -= RefreshSingleSlot;

        _gridService = service;
        _library = library;

        _gridService.OnSlotStateChanged += RefreshSingleSlot;

        GenerateGrid();
        RefreshAllSlots();
    }
    private void Start()
    {
        if (_gridService == null)
            Debug.LogWarning("[GridManager] Aguardando configuração via Bootstrapper...");
    }

    public Vector2 GetGridWorldSize()
    {
        if (_gridService == null || _gridService.Config == null) 
            return new Vector2(5f, 5f); // Fallback

        int cols = _gridService.Config.Columns;
        int rows = _gridService.Config.Rows;

        // Adiciona uma margem de segurança (ex: 1 slot extra de borda)
        float width = cols * _spacing.x + 1f;
        float height = rows * _spacing.y + 1f;

        return new Vector2(width, height);
    }

    private void OnDestroy()
    {
        if (_gridService != null)
            _gridService.OnSlotStateChanged -= RefreshSingleSlot;
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index >= 0 && index < _spawnedSlots.Count)
            return _spawnedSlots[index].transform.position;
        return Vector3.zero;
    }

    private void GenerateGrid()
    {
        List<SpriteRenderer> renderersForFeedback = new List<SpriteRenderer>();

        foreach (var slot in _spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        _spawnedSlots.Clear();

        foreach (Transform child in _gridContainer)
            Destroy(child.gameObject);

        if (_gridService == null)
        {
            Debug.LogError("[GridManager] GridService nulo! Não é possível gerar grid.");
            return;
        }

        int totalSlots = _gridService.SlotCount;
        var config = _gridService.Config;
        
        int cols = config != null ? config.Columns : (int)Mathf.Sqrt(totalSlots);
        int rows = config != null ? config.Rows : cols; // Fallback quadrado

        // Cálculo de centralização
        // Se cols=5, centerIndex=2 (0,1,2,3,4). xOffset = -2 * spacing.
        // Fórmula geral: (index - (count-1)/2.0f) * spacing
        float xOffset = -(cols - 1) / 2.0f * _spacing.x;
        float yOffset = (rows - 1) / 2.0f * _spacing.y; // Y cresce pra cima no Unity World, mas grid index costuma ser top-down ou bottom-up?
        // No loop original: yPos = (center - row) * _spacing.y; 
        // Vamos manter a lógica visual consistente: row 0 é o topo?
        // Original: row = i / width. yPos = (1 - row) * spacing.y (assumindo 3x3, center=1). 
        // Se row=0 -> y=spacing. Row=1 -> y=0. Row=2 -> y=-spacing.
        // Isso confirma que Row 0 é o TOPO.
        
        // Novo cálculo para Row 0 ser Topo e Center ser (0,0) local:
        // Topo deve ser positivo. Base negativo.
        
        float startX = -(cols - 1) * _spacing.x / 2f;
        float startY = (rows - 1) * _spacing.y / 2f;

        for (int i = 0; i < totalSlots; i++)
        {
            int row = i / cols;
            int col = i % cols;

            var newSlot = Instantiate(_slotPrefab, _gridContainer);

            float xPos = startX + (col * _spacing.x);
            float yPos = startY - (row * _spacing.y);

            newSlot.transform.localPosition = new Vector2(xPos, yPos);
            newSlot.name = $"Slot_{i}_[{col},{row}]";

            newSlot.Initialize(i);
            newSlot.Configure(this, i);

            newSlot.OnDropInteraction += HandleDropInteraction;

            _spawnedSlots.Add(newSlot);

            var renderer = newSlot.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderersForFeedback.Add(renderer);
            }
        }

        var feedback = GetComponent<GridStateFeedback>();
        if (feedback != null)
        {
            feedback.UpdateRenderers(renderersForFeedback);
        }
    }

    private void HandleDropInteraction(int index, CardView cardView)
    {
        CardData data = cardView.Data;
        CardInstance instance = cardView.Instance;

        // A lógica do Grid usa os DADOS (Regras do jogo)
        InteractionResult result = _gridService.ApplyCard(index, data);

        if (result.IsSuccess)
        {
            if (result.ShouldConsumeCard)
            {
                // SOLUÇÃO:
                // 1. CardRemoved: Remove visualmente da mão e da lista lógica (HandManager ouve isso)
                AppCore.Instance.Events.Player.TriggerCardRemoved(instance);

                // 2. CardConsumed: Apenas para estatísticas/analytics (opcional, mas bom manter)
                AppCore.Instance.Events.Player.TriggerCardConsumed(data.ID);
            }
            else
            {
                Debug.Log("[GridManager] Carta usada (ex: Regador infinito), mas mantida na mão.");
            }
        }
        else
        {
            Debug.Log($"[GridManager] Ação falhou: {result.Message}");
            // Feedback de erro visual na carta (opcional)
        }
    }

    // --- ATUALIZAÇÃO VISUAL (Tradução Model -> View) ---

    public void RefreshAllSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++) RefreshSingleSlot(i);
    }

    private void RefreshSingleSlot(int index)
    {
        if (index < 0 || index >= _spawnedSlots.Count) return;

        IReadOnlyCropState state = _gridService.GetSlotReadOnly(index);
        var view = _spawnedSlots[index];

        // Atualiza Bloqueio
        bool isUnlocked = _gridService.IsSlotUnlocked(index);
        view.SetLockedState(!isUnlocked);
        if (!isUnlocked) return; 

        Sprite spriteToRender = null;
        bool isWatered = false;

        if (state != null)
        {
            isWatered = state.IsWatered; 

            if (state.CropID.IsValid) 
            {
                if (_library != null)
                {
                    if (_library.TryGetCrop(state.CropID, out var data))
                    {
                        spriteToRender = data.GetSpriteForStage(
                            state.CurrentGrowth,
                            state.DaysMature, 
                            state.IsWithered
                        );
                    }
                }
            }
        }
        view.SetVisualState(spriteToRender, isWatered);
    }
}