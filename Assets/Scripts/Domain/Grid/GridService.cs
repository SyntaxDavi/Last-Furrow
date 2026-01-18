using UnityEngine;
using System;

public class GridService : IGridService
{
    private readonly RunData _runData;
    private readonly IGameLibrary _library;
    private readonly GameStateManager _gameStateManager;
    private readonly GridConfiguration _config;

    public event Action<int> OnSlotStateChanged;
    public event Action OnDataDirty;
    public event Action<int, GridEventType> OnSlotUpdated;

    // ⭐ NOVO: Evento composto (substitui os 3 acima gradualmente)
    public event System.Action<GridChangeEvent> OnGridChanged;

    public int SlotCount => _runData?.GridSlots?.Length ?? 0;
    public GridConfiguration Config => _config;

    public GridService(RunData runData, IGameLibrary library, GameStateManager gameStateManager, GridConfiguration config)
    {
        _runData = runData;
        _library = library;
        _gameStateManager = gameStateManager;
        _config = config;

        // Validação vital
        if (_config == null)
        {
            Debug.LogError("[GridService] Configuração de Grid é NULA! Usando fallback perigoso.");
        }

        // ⭐ NOVA ARQUITETURA: Delega inicialização para GridInitializer
        GridInitializer.Initialize(_runData, _config);
    }


    public IReadOnlyCropState GetSlotReadOnly(int index)
    {
        if (IsValidIndex(index)) return _runData.GridSlots[index];
        return new CropState();
    }

    public CropState GetSlot(int index)
    {
         if (IsValidIndex(index)) return _runData.GridSlots[index];
         return null;
    }

    public GridSlotState GetSlotStateReadOnly(int index)
    {
        if (IsValidIndex(index))
        {
             if (_runData.SlotStates[index] == null) _runData.SlotStates[index] = new GridSlotState(false);
             return _runData.SlotStates[index];
        }
        return new GridSlotState(false);
    }


    // --- CORREÇÃO 1: Usando o nome correto e a lógica correta ---
    public bool CanReceiveCard(int index, CardData card)
    {
        if (!IsValidIndex(index)) return false;
        if (card == null) return false;

        // Regra de Bloqueio
        bool isUnlocked = IsSlotUnlocked(index);
        
        // Se bloqueado, só carta de Expansion pode interagir (para desbloquear)
        if (!isUnlocked && card.Type != CardType.Expansion) return false;

        var strategy = InteractionFactory.GetStrategy(card.Type);
        if (strategy == null) return false;

        return strategy.CanInteract(index, this, card);
    }

    public InteractionResult ApplyCard(int index, CardData card)
    {
        // 1. PROTEÇÃO DE ESTADO
        if (_gameStateManager.CurrentState != GameState.Playing)
        {
            return InteractionResult.Fail("Ação bloqueada: O jogo não está em fase de produção.");
        }

        // 2. Validações Padrão
        if (!IsValidIndex(index)) return InteractionResult.Fail("Slot inválido.");

        // 3. Estratégia de Interação
        ICardInteractionStrategy strategy = GetStrategyForCard(card);
        if (strategy == null) return InteractionResult.Fail("Carta sem efeito definido.");

        if (!strategy.CanInteract(index, this, card)) return InteractionResult.Fail("Interação inválida neste slot.");

        // 4. Execução
        var result = strategy.Execute(index, this, card);

        if (result.IsSuccess)
        {
            // ⭐ NOVO: Usa sistema de eventos composto
            EmitGridChangeEvent(
                index,
                result.EventType,
                new GridChangeImpact
                {
                    RequiresVisualUpdate = true,
                    RequiresSave = true,
                    AffectsScore = false // TODO: Determinar quando afeta score
                }
            );
        }

        return result;
    }
    public float GetGridContaminationPercentage()
    {
        if (_runData.GridSlots == null || _runData.GridSlots.Length == 0) return 0f;

        int totalSlots = _runData.GridSlots.Length;
        int contaminatedSlots = 0;

        foreach (var slot in _runData.GridSlots)
        {
            // Contaminação = Planta Morta (Futuro: + Pragas)
            if (slot.IsWithered)
            {
                contaminatedSlots++;
            }
        }

        return (float)contaminatedSlots / totalSlots;
    }

    public void ProcessNightCycleForSlot(int index)
    {
        if (!IsValidIndex(index)) return;

        // ⭐ EARLY EXIT: Slots bloqueados não são processados
        // RAZÃO: 
        // - Bloqueados não participam da meta/score
        // - Bloqueados nunca têm plantas (estado garantido vazio)
        // - Bloqueados não podem ser molhados (sem estado residual)
        // - Evita processamento desnecessário e eventos inválidos
        if (!IsSlotUnlocked(index)) return;

        var slot = _runData.GridSlots[index];
        bool wasWatered = slot.IsWatered;

        // 1. Seca a terra
        slot.IsWatered = false;

        GridEventType eventToEmit = GridEventType.GenericUpdate;
        bool visualUpdateNeeded = wasWatered;

        if (wasWatered) eventToEmit = GridEventType.DryOut;

        // 2. Processa Biologia
        if (!slot.IsEmpty && _library.TryGetCrop(slot.CropID, out var data))
        {
            var result = CropLogic.ProcessNightlyGrowth(slot, data);

            if (result.EventType != GrowthEventType.None)
            {
                visualUpdateNeeded = true;
                switch (result.EventType)
                {
                    case GrowthEventType.Matured:
                        eventToEmit = GridEventType.Matured;
                        break;
                    case GrowthEventType.WitheredByAge:
                        eventToEmit = GridEventType.Withered;
                        break;
                    default:
                        if (eventToEmit == GridEventType.GenericUpdate)
                            eventToEmit = GridEventType.GenericUpdate;
                        break;
                }
            }
        }

        if (visualUpdateNeeded)
        {
            OnSlotStateChanged?.Invoke(index);
            OnSlotUpdated?.Invoke(index, eventToEmit);
            OnDataDirty?.Invoke();
        }
    }

    public bool IsSlotUnlocked(int index)
    {
        if (!IsValidIndex(index)) return false;
        if (_runData.SlotStates[index] == null) _runData.SlotStates[index] = new GridSlotState(false);
        return _runData.SlotStates[index].IsUnlocked;
    }

    public bool CanUnlockSlot(int index)
    {
        if (!IsValidIndex(index)) return false;
        if (IsSlotUnlocked(index)) return false;
        return IsAdjacentToUnlocked(index);
    }

    public bool TryUnlockSlot(int index)

    {
        if (!IsValidIndex(index)) return false;
        if (IsSlotUnlocked(index)) return false; 

        if (IsAdjacentToUnlocked(index))
        {
            _runData.SlotStates[index].IsUnlocked = true;
            OnSlotStateChanged?.Invoke(index);
            OnDataDirty?.Invoke();
            return true;
        }
        return false;
    }

    private bool IsAdjacentToUnlocked(int index)
    {
        if (_config == null) return false;

        int width = _config.Columns;
        int height = _config.Rows;
        
        // Verifica consistência
        if (width * height != SlotCount) 
        {
             // Fallback para grid quadrado se a config não bater com dados (ex: migração incompleta)
             width = (int)Mathf.Sqrt(SlotCount);
        }

        int r = index / width;
        int c = index % width;

        int[] dr = {-1, 1, 0, 0};
        int[] dc = {0, 0, -1, 1};

        for(int i=0; i<4; i++)
        {
            int nr = r + dr[i];
            int nc = c + dc[i];
            if (nr >= 0 && nr < height && nc >= 0 && nc < width)
            {
                int nIndex = nr * width + nc;
                if (IsSlotUnlocked(nIndex)) return true;
            }
        }
        return false;
    }

    private ICardInteractionStrategy GetStrategyForCard(CardData card)

    {
        if (card == null) return null;
        return InteractionFactory.GetStrategy(card.Type);
    }

    /// <summary>
    /// ⭐ NOVO: Emite evento composto + eventos legados (transição gradual).
    /// 
    /// MIGRAÇÃO:
    /// - Mantém eventos antigos para compatibilidade
    /// - Adiciona evento novo (OnGridChanged)
    /// - Futuramente, remover eventos antigos e manter só OnGridChanged
    /// </summary>
    private void EmitGridChangeEvent(int index, GridEventType eventType, GridChangeImpact impact)
    {
        // 1. Evento novo (composto)
        var gridEvent = GridChangeEvent.Create(
            index,
            eventType,
            impact,
            GridSlotSnapshot.FromCropState(GetSlot(index))
        );
        OnGridChanged?.Invoke(gridEvent);

        // 2. Eventos legados (compatibilidade)
        if (impact.RequiresVisualUpdate)
        {
            OnSlotStateChanged?.Invoke(index);
            OnSlotUpdated?.Invoke(index, eventType);
        }

        if (impact.RequiresSave)
        {
            OnDataDirty?.Invoke();
        }
    }

    // --- HELPER UNIFICADO ---
    // Retorna TRUE se o índice for BOM.
    private bool IsValidIndex(int index) => index >= 0 && index < _runData.GridSlots.Length;
}