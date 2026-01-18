using UnityEngine;

/// <summary>
/// Estratégia para regar/acelerar crescimento (água, fertilizante).
/// Recebe RunIdentityContext e RunRuntimeContext via injeção.
/// 
/// Validações contra:
/// - Null references
/// - Dados inválidos
/// - Overdose letal
/// </summary>
public class WaterInteractionStrategy : ICardInteractionStrategy
{
    private readonly RunIdentityContext _identityContext;
    private readonly RunRuntimeContext _runtimeContext;

    public WaterInteractionStrategy(RunIdentityContext identityContext, RunRuntimeContext runtimeContext)
    {
        if (identityContext.Library == null)
            throw new System.ArgumentNullException(nameof(identityContext), 
                "[WaterInteractionStrategy] RunIdentityContext.Library é nulo!");
        
        _identityContext = identityContext;
        _runtimeContext = runtimeContext;
    }

    public bool CanInteract(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlotReadOnly(index);
        
        if (slot == null || card == null)
            return false;

        // Se no h planta, a maioria dos modificadores no funciona
        if (slot.IsEmpty)
            return false;

        return !slot.IsWithered;
    }

    public InteractionResult Execute(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlot(index);
        
        // Validaes defensivas
        if (slot == null)
            return InteractionResult.Fail("[ERRO] CropState  null!");

        if (card == null)
            return InteractionResult.Fail("[ERRO] CardData  null!");

        if (slot.IsEmpty)
            return InteractionResult.Fail("Não há planta aqui para regar.");

        if (slot.IsWithered)
            return InteractionResult.Fail("A planta está morta, regar não ajuda mais.");

        if (slot.IsWatered)
            return InteractionResult.Fail("A planta já foi regada hoje.");

        // Busca dados com erro tratado
        if (!_identityContext.Library.TryGetCrop(slot.CropID, out CropData cropData))
        {
            Debug.LogWarning($"[WaterInteractionStrategy] Crop desconhecido: {slot.CropID}");
            return InteractionResult.Fail("Dados da planta não encontrados.");
        }

        if (cropData == null)
            return InteractionResult.Fail("Dados da planta corrompidos.");

        // Marca como regada
        slot.IsWatered = true;

        // Aceleração com fallback
        int acceleration = card.GrowthAcceleration > 0 ? card.GrowthAcceleration : 1;

        // Lógica Biológica
        var simResult = CropLogic.ApplyAcceleration(slot, cropData, acceleration);

        GridEventType finalEvent;
        string msg;

        // Trata todos os casos de resultado
        switch (simResult.EventType)
        {
            case GrowthEventType.WitheredByOverdose:
                finalEvent = GridEventType.Withered;
                msg = "Você regou demais! A planta morreu por overdose.";
                break;

            case GrowthEventType.Matured:
                finalEvent = GridEventType.Matured;
                msg = $"A aceleração fez {cropData.Name} amadurecer!";
                break;

            case GrowthEventType.Growing:
                finalEvent = GridEventType.Watered;
                msg = $"{cropData.Name} foi regada e está crescendo.";
                break;

            default:
                finalEvent = GridEventType.Watered;
                msg = $"{cropData.Name} foi regada com sucesso.";
                break;
        }

        return InteractionResult.Success(msg, finalEvent, consume: true);
    }
}


