using System;

public class GridService : IGridService
{
    private readonly RunData _runData;

    // Eventos desacoplados do AppCore
    public event Action<int> OnSlotStateChanged;
    public event Action OnDataDirty; 

    public GridService(RunData runData)
    {
        _runData = runData ?? throw new ArgumentNullException(nameof(runData));
        ValidateGridSize();
    }

    private void ValidateGridSize()
    {
        if (_runData.GridSlots == null || _runData.GridSlots.Length != 9)
        {
            _runData.GridSlots = new CropState[9];
            for (int i = 0; i < 9; i++) _runData.GridSlots[i] = new CropState();
        }
    }

    public CropState GetSlotReadOnly(int index)
    {
        if (IsIndexInvalid(index)) return null;
        return _runData.GridSlots[index]; // Retorna referência para leitura
    }

    public bool CanReceiveCard(int index, CardData card)
    {
        if (IsIndexInvalid(index) || card == null) return false;
        var strategy = InteractionFactory.GetStrategy(card.Type);
        return strategy != null && strategy.CanInteract(_runData.GridSlots[index], card);
    }

    public InteractionResult ApplyCard(int index, CardData card)
    {
        if (IsIndexInvalid(index)) return InteractionResult.Fail("Índice inválido");

        var strategy = InteractionFactory.GetStrategy(card.Type);
        if (strategy == null) return InteractionResult.Fail("Sem estratégia para esta carta");

        var result = strategy.Execute(_runData.GridSlots[index], card);

        if (result.Success)
        {
            _runData.DeckIDs.Remove(card.ID); // Regra de negócio: Consumir carta
            OnSlotStateChanged?.Invoke(index); // Notifica Controller
            OnDataDirty?.Invoke();             // Notifica necessidade de Save
        }

        return result;
    }

    private bool IsIndexInvalid(int index) => index < 0 || index >= _runData.GridSlots.Length;
}