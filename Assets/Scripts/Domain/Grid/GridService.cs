using UnityEngine;
using System;

public class GridService : IGridService
{
    private readonly RunData _runData;
    private readonly IGameLibrary _library;
    private readonly GameStateManager _gameStateManager;

    public event Action<int> OnSlotStateChanged;
    public event Action OnDataDirty;
    public event Action<int, GridEventType> OnSlotUpdated;

    public GridService(RunData runData, IGameLibrary library, GameStateManager gameStateManager)
    {
        _runData = runData;
        _library = library;
        _gameStateManager = gameStateManager;
    }

    public IReadOnlyCropState GetSlotReadOnly(int index)
    {
        if (IsValidIndex(index)) return _runData.GridSlots[index];
        return new CropState();
    }

    // --- CORREÇÃO 1: Usando o nome correto e a lógica correta ---
    public bool CanReceiveCard(int index, CardData card)
    {
        // Se o índice NÃO for válido (!IsValidIndex), retorna falso.
        if (!IsValidIndex(index))
        {
            Debug.Log($"Fail: Index {index} inválido ou fora do array.");
            return false;
        }

        if (card == null) return false;

        var slot = _runData.GridSlots[index];

        var strategy = InteractionFactory.GetStrategy(card.Type);
        if (strategy == null) return false;

        // O Slot está vazio? A estratégia permite?
        return strategy.CanInteract(slot, card);
    }

    public InteractionResult ApplyCard(int index, CardData card)
    {
        // 1. PROTEÇÃO DE ESTADO
        if (_gameStateManager.CurrentState != GameState.Playing)
        {
            return InteractionResult.Fail("Ação bloqueada: O jogo não está em fase de produção.");
        }

        // 2. Validações Padrão
        // --- CORREÇÃO 2: Adicionado '!' (NOT) ---
        // Se NÃO for válido, aí sim retorna erro.
        if (!IsValidIndex(index)) return InteractionResult.Fail("Slot inválido.");

        var slot = _runData.GridSlots[index];

        // 3. Estratégia de Interação
        ICardInteractionStrategy strategy = GetStrategyForCard(card);
        if (strategy == null) return InteractionResult.Fail("Carta sem efeito definido.");

        if (!strategy.CanInteract(slot, card)) return InteractionResult.Fail("Interação inválida neste slot.");

        // 4. Execução
        var result = strategy.Execute(slot, card);

        if (result.IsSuccess)
        {
            OnSlotStateChanged?.Invoke(index);
            OnSlotUpdated?.Invoke(index, result.EventType);
            OnDataDirty?.Invoke();
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
        // --- CORREÇÃO 3: Adicionado '!' (NOT) ---
        if (!IsValidIndex(index)) return;

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

    private ICardInteractionStrategy GetStrategyForCard(CardData card)
    {
        if (card == null) return null;
        return InteractionFactory.GetStrategy(card.Type);
    }

    // --- HELPER UNIFICADO ---
    // Retorna TRUE se o índice for BOM.
    private bool IsValidIndex(int index) => index >= 0 && index < _runData.GridSlots.Length;
}