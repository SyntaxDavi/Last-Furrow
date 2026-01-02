using System;
using UnityEngine;

public class GridService : IGridService
{
    private readonly RunData _runData;
    private readonly IGameLibrary _library;

    public event Action<int> OnSlotStateChanged;
    public event Action OnDataDirty;
    public event Action<int, GridEventType> OnSlotUpdated;
    public GridService(RunData runData, IGameLibrary library)
    {   
        _runData = runData;
        _library = library;
    }

    public IReadOnlyCropState GetSlotReadOnly(int index)
    {
        if (IsIndexInvalid(index)) return null;

        // Retorna o objeto concreto, mas o compilador trata como Interface
        return _runData.GridSlots[index];
    }

    public bool CanReceiveCard(int index, CardData card)
    {
        if (IsIndexInvalid(index) || card == null) return false;
        var slot = _runData.GridSlots[index];
        var strategy = InteractionFactory.GetStrategy(card.Type);
        return strategy != null && strategy.CanInteract(slot, card);
    }

    public InteractionResult ApplyCard(int index, CardData card)
    {
        if (IsIndexInvalid(index)) return InteractionResult.Fail("Índice inválido");

        var slot = _runData.GridSlots[index];
        var strategy = InteractionFactory.GetStrategy(card.Type);

        if (strategy == null) return InteractionResult.Fail("Sem estratégia");

        var result = strategy.Execute(slot, card, _library);

        if (result.IsSuccess)
        {
            // CONSUMO CONDICIONAL (Baseado na decisão da estratégia)
            if (result.ShouldConsumeCard)
            {
                // Aqui ainda assumimos DeckIDs, mas futuramente pode virar HandManager
                if (_runData.DeckIDs.Contains(card.ID.Value))
                    _runData.DeckIDs.Remove(card.ID.Value);
            }

            // DISPARO CENTRALIZADO
            OnSlotStateChanged?.Invoke(index); // UI Básica (Sprite)
            OnSlotUpdated?.Invoke(index, result.EventType); // UI Rica (Som/Partícula)

            OnDataDirty?.Invoke();
        }

        return result;
    }

    public void ProcessNightCycleForSlot(int index)
    {
        if (IsIndexInvalid(index)) return;
        var slot = _runData.GridSlots[index];

        bool wasWatered = slot.IsWatered;

        // 1. Seca a terra
        slot.IsWatered = false;

        GridEventType eventToEmit = GridEventType.GenericUpdate;
        bool visualUpdateNeeded = wasWatered;

        // Se secou e nada mais acontecer, o evento é DryOut
        if (wasWatered) eventToEmit = GridEventType.DryOut;

        // 2. Processa Biologia
        if (!slot.IsEmpty && _library.TryGetCrop(slot.CropID, out var data))
        {
            var result = CropLogic.ProcessNightlyGrowth(slot, data);

            if (result.EventType != GrowthEventType.None)
            {
                visualUpdateNeeded = true;

                // TRADUÇÃO (Logic -> GridEvent)
                // Eventos biológicos têm prioridade sobre o "DryOut"
                switch (result.EventType)
                {
                    case GrowthEventType.Matured:
                        eventToEmit = GridEventType.Matured;
                        break;
                    case GrowthEventType.WitheredByAge:
                        eventToEmit = GridEventType.Withered;
                        break;
                    // Growing ou LastFreshDayWarning podem ser mapeados para GenericUpdate 
                    // ou criar eventos específicos se você tiver som de "crescimento"
                    default:
                        // Se era DryOut mas cresceu, talvez queira manter DryOut ou Generic?
                        // Vamos dar prioridade ao DryOut se não for Matured/Withered
                        if (eventToEmit == GridEventType.GenericUpdate)
                            eventToEmit = GridEventType.GenericUpdate;
                        break;
                }
            }
        }

        if (visualUpdateNeeded)
        {
            OnSlotStateChanged?.Invoke(index);
            // Emite o evento rico (Matured, Withered ou DryOut)
            OnSlotUpdated?.Invoke(index, eventToEmit);
            OnDataDirty?.Invoke();
        }
    }

    private bool IsIndexInvalid(int index) => index < 0 || index >= _runData.GridSlots.Length;
}