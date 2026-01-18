using UnityEngine;

/// <summary>
/// Estratégia para colheita de plantas.
/// 
/// Recebe RunIdentityContext via injeção (não acessa AppCore).
/// Interface fica limpa: Execute(slot, card).
/// 
/// Validações robustas contra:
/// - Null references
/// - Estados inválidos
/// - Dados desconhecidos
/// </summary>
public class HarvestInteractionStrategy : ICardInteractionStrategy
{
    private readonly RunIdentityContext _context;

    public HarvestInteractionStrategy(RunIdentityContext context)
    {
        // Validação no construtor
        if (context.Library == null)
            throw new System.ArgumentNullException(nameof(context), 
                "[HarvestInteractionStrategy] RunIdentityContext.Library é nulo!");
        
        if (context.Economy == null)
            throw new System.ArgumentNullException(nameof(context), 
                "[HarvestInteractionStrategy] RunIdentityContext.Economy é nulo!");
        
        _context = context;
    }

    public bool CanInteract(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlotReadOnly(index);

        // Null checks robustos
        if (slot == null || card == null)
            return false;

        // Proteções básicas
        if (slot.IsEmpty || slot.IsWithered)
            return false;

        // IMPORTANTE: Não apenas verificar CurrentGrowth,
        // mas garantir que a planta foi plantada há pelo menos 1 dia
        // Uma planta no DIA 0 não pode ser colhida mesmo que acelerada
        return slot.DaysMature >= 0 && slot.CurrentGrowth > 0;
    }

    public InteractionResult Execute(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlot(index);

        // Validações defensivas
        if (slot == null)
             return InteractionResult.Fail("[ERRO] CropState não encontrado!");

        if (card == null)
            return InteractionResult.Fail("[ERRO] CardData é null!");

        if (slot.IsEmpty)
            return InteractionResult.Fail("Nada para colher.");

        if (slot.IsWithered)
            return InteractionResult.Fail("A planta está morta e não pode ser colhida.");

        // 1. Busca dados com erro tratado
        if (!_context.Library.TryGetCrop(slot.CropID, out CropData cropData))
        {
            Debug.LogWarning($"[HarvestInteractionStrategy] Crop desconhecido: {slot.CropID}");
            return InteractionResult.Fail("Dados da planta não encontrados no sistema.");
        }

        if (cropData == null)
            return InteractionResult.Fail("Dados da planta corrompidos.");

        // 2. Validação de Maturação
        bool isMature = slot.CurrentGrowth >= cropData.DaysToMature;
        if (!isMature)
        {
            int remainingDays = cropData.DaysToMature - slot.CurrentGrowth;
            return InteractionResult.Fail($"A planta ainda não está madura! Faltam {remainingDays} dias.");
        }

        // 3. Validação de Tempo Mínimo
        // Garante que pelo menos 1 dia passou (CurrentGrowth > 0)
        if (slot.CurrentGrowth <= 0)
            return InteractionResult.Fail("A planta foi plantada hoje. Espere até amanhã.");

        // 4. Economia - com proteção contra valores inválidos
        int value = cropData.BaseSellValue;
        if (value < 0)
        {
            Debug.LogWarning($"[HarvestInteractionStrategy] Valor negativo para {cropData.Name}! Usando 0.");
            value = 0;
        }

        try
        {
            _context.Economy.Earn(value, TransactionType.Harvest);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HarvestInteractionStrategy] Erro ao ganhar moedas: {ex.Message}");
            return InteractionResult.Fail("Erro ao processar colheita. Tente novamente.");
        }

        // 5. Limpeza
        slot.Clear();

        return InteractionResult.Success(
            $"Colhido com sucesso! +${value}",
            GridEventType.Harvested,
            consume: true
        );
    }
}

