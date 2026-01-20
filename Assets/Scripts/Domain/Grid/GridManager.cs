using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gerenciador visual do Grid - Orquestra GridSlotViews.
/// 
/// RESPONSABILIDADE:
/// - Spawnar e posicionar slots visuais (GridSlotView)
/// - Injetar GridVisualContext em cada slot
/// - Atualizar visual quando estado do domínio muda
/// - Traduzir IReadOnlyCropState para comandos visuais
/// 
/// ARQUITETURA:
/// - Recebe GridVisualContext via Initialize() (injeção)
/// - Escuta GridService.OnSlotStateChanged (event-driven)
/// - Manager Push: Empurra atualizações para slots
/// 
/// NÃO FAZ:
/// - Validar regras de jogo (IDropValidator faz)
/// - Renderizar (GridSlotView faz)
/// - Decidir cores (GridVisualConfig faz)
/// - Acessar AppCore.Instance diretamente
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Configuracao Visual")]
    [SerializeField] private GridSlotView _slotPrefab;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 1.1f);

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    private GridVisualContext _context;
    private List<GridSlotView> _spawnedSlots = new List<GridSlotView>();
    private bool _isInitialized = false;
    
    // Propriedades publicas (mantidas para compatibilidade)
    public IGridService Service => _context?.GridService;
    public Vector2 Spacing => _spacing;

    /// <summary>
    /// Inicializa o GridManager com contexto injetado.
    /// Substitui o antigo Configure().
    /// </summary>
    public void Initialize(GridVisualContext context)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[GridManager] Ja foi inicializado!");
            return;
        }

        if (context == null)
        {
            Debug.LogError("[GridManager] CRITICAL: GridVisualContext null!");
            return;
        }

        if (_slotPrefab == null)
        {
            Debug.LogError("[GridManager] CRITICAL: SlotPrefab nao atribuido no Inspector!");
            return;
        }

        if (_gridContainer == null)
        {
            Debug.LogWarning("[GridManager] Container null, usando this.transform");
            _gridContainer = this.transform;
        }

        _context = context;

        if (_showDebugLogs)
            Debug.Log("[GridManager] Iniciando configuracao do grid visual...");

        _context.GridService.OnSlotStateChanged += RefreshSingleSlot;

        GenerateGrid();
        RefreshAllSlots();

        _isInitialized = true;

        if (_showDebugLogs)
            Debug.Log($"[GridManager] SUCESSO: Grid inicializado ({_spawnedSlots.Count} slots)");
    }

    private void Start()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[GridManager] Aguardando configuracao via GridVisualBootstrapper...");
        }
    }

    private void OnDestroy()
    {
        if (_context != null && _context.GridService != null)
        {
            _context.GridService.OnSlotStateChanged -= RefreshSingleSlot;
        }
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index >= 0 && index < _spawnedSlots.Count)
            return _spawnedSlots[index].transform.position;
        return Vector3.zero;
    }

    private void GenerateGrid()
    {
        if (_showDebugLogs)
            Debug.Log("[GridManager] Gerando slots visuais...");

        foreach (var slot in _spawnedSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        _spawnedSlots.Clear();

        foreach (Transform child in _gridContainer)
        {
            Destroy(child.gameObject);
        }

        if (_context.GridService == null)
        {
            Debug.LogError("[GridManager] GridService null!");
            return;
        }

        int totalSlots = _context.GridService.SlotCount;
        var config = _context.GridService.Config;
        
        if (config == null)
        {
            Debug.LogError("[GridManager] GridConfiguration null!");
            return;
        }

        int cols = config.Columns;
        int rows = config.Rows;

        if (_showDebugLogs)
            Debug.Log($"[GridManager] Grid {rows}x{cols} = {totalSlots} slots");

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

            newSlot.Initialize(_context, i);
            newSlot.OnDropInteraction += HandleDropInteraction;

            _spawnedSlots.Add(newSlot);

            if (_showDebugLogs)
                Debug.Log($"[GridManager] Slot {i} criado em ({xPos:F1}, {yPos:F1})");
        }

        if (_showDebugLogs)
            Debug.Log($"[GridManager] Pool completo: {_spawnedSlots.Count}/{totalSlots} slots");
    }

    private void HandleDropInteraction(int index, CardView cardView)
    {
        if (_showDebugLogs)
            Debug.Log($"[GridManager] Drop no slot {index}: {cardView.Data.Name}");

        CardData data = cardView.Data;
        CardInstance instance = cardView.Instance;

        InteractionResult result = _context.GridService.ApplyCard(index, data);

        if (result.IsSuccess)
        {
            if (result.ShouldConsumeCard)
            {
                AppCore.Instance.Events.Player.TriggerCardRemoved(instance);
                AppCore.Instance.Events.Player.TriggerCardConsumed(data.ID);

                if (_showDebugLogs)
                    Debug.Log($"[GridManager] Carta consumida: {data.Name}");
            }
            else
            {
                if (_showDebugLogs)
                    Debug.Log($"[GridManager] Carta mantida na mao: {data.Name}");
            }
        }
        else
        {
            Debug.LogWarning($"[GridManager] Acao falhou: {result.Message}");
        }
    }

    public void RefreshAllSlots()
    {
        if (_showDebugLogs)
            Debug.Log("[GridManager] Atualizando todos os slots...");

        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            RefreshSingleSlot(i);
        }

        if (_showDebugLogs)
            Debug.Log($"[GridManager] {_spawnedSlots.Count} slots atualizados");
    }

    private void RefreshSingleSlot(int index)
    {
        if (index < 0 || index >= _spawnedSlots.Count)
        {
            Debug.LogWarning($"[GridManager] Indice invalido: {index}");
            return;
        }

        IReadOnlyCropState state = _context.GridService.GetSlotReadOnly(index);
        var view = _spawnedSlots[index];

        bool isUnlocked = _context.GridService.IsSlotUnlocked(index);
        view.SetLockedState(!isUnlocked);
        
        if (!isUnlocked)
        {
            if (_showDebugLogs)
                Debug.Log($"[GridManager] Slot {index} bloqueado");
            return; 
        }

        Sprite spriteToRender = null;
        bool isWatered = false;
        bool isMature = false;
        bool isWithered = false;

        if (state != null)
        {
            isWatered = state.IsWatered;
            isWithered = state.IsWithered;

            if (state.CropID.IsValid) 
            {
                // Planta está madura se atingiu os dias necessários
                isMature = (state.CurrentGrowth >= state.DaysMature) && !isWithered;

                if (_context.Library != null)
                {
                    if (_context.Library.TryGetCrop(state.CropID, out var cropData))
                    {
                        spriteToRender = cropData.GetSpriteForStage(
                            state.CurrentGrowth,
                            state.DaysMature, 
                            state.IsWithered
                        );
                    }
                    else
                    {
                        Debug.LogWarning($"[GridManager] Crop nao encontrado: {state.CropID}");
                    }
                }
            }
        }

        view.SetVisualState(spriteToRender, isWatered, isMature, isWithered);

        if (_showDebugLogs)
            Debug.Log($"[GridManager] Slot {index}: sprite={spriteToRender != null}, water={isWatered}, mature={isMature}, withered={isWithered}");
    }
}
