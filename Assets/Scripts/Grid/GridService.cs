using System;
using UnityEngine;

public class GridService : IGridService
{
    private readonly RunData _runData;
    private readonly IGameLibrary _library;

    public event Action<int> OnSlotStateChanged;
    public event Action OnDataDirty;

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

        // Passamos a Library aqui
        var result = strategy.Execute(slot, card, _library);

        if (result.Success)
        {
            if (_runData.DeckIDs.Contains(card.ID.Value))
            {
                _runData.DeckIDs.Remove(card.ID.Value);
            }

            OnSlotStateChanged?.Invoke(index);
            OnDataDirty?.Invoke();
        }

        return result;
    }

    public void ProcessNightCycleForSlot(int index)
    {
        if (IsIndexInvalid(index)) return;

        var slot = _runData.GridSlots[index];

        // 1. Captura estado anterior (para saber se precisamos atualizar a tela)
        bool wasWatered = slot.IsWatered;

        // 2. Aplica a Regra do Grid: A noite seca o slot.
        // Fazemos isso aqui explicitamente para garantir que slots vazios também sequem.
        // (O CropLogic também faz isso, mas só roda se tiver planta/data)
        slot.IsWatered = false;

        // Inicialmente, precisamos atualizar a tela se a terra estava molhada e secou
        bool visualUpdateNeeded = wasWatered;

        // 3. Se tiver planta, processa a biologia (Crescimento/Morte)
        if (!slot.IsEmpty && _library.TryGetCrop(slot.CropID, out var data))
        {
            // A CropLogic vai calcular crescimento e morte
            var result = CropLogic.ProcessNightlyGrowth(slot, data);

            // Se houve evento biológico (cresceu, morreu, maturou), também precisamos atualizar a tela
            if (result.EventType != GrowthEventType.None)
            {
                visualUpdateNeeded = true;

                // Logs de debug úteis
                if (result.EventType == GrowthEventType.WitheredByAge)
                    Debug.Log($"[GridService] Slot {index} ({data.Name}) morreu de velhice.");
            }
        }

        // 4. Se houve qualquer mudança visual (água secou OU planta mudou), notifica
        if (visualUpdateNeeded)
        {
            OnSlotStateChanged?.Invoke(index);
            OnDataDirty?.Invoke(); // Marca para salvar
        }
    }

    private bool IsIndexInvalid(int index) => index < 0 || index >= _runData.GridSlots.Length;
}