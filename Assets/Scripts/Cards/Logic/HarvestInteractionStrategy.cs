using UnityEngine;

/// <summary>
/// Estrat�gia para colheita de plantas.
/// 
/// Recebe RunIdentityContext via inje��o (n�o acessa AppCore).
/// Interface fica limpa: Execute(slot, card).
/// 
/// Valida��es robustas contra:
/// - Null references
/// - Estados inv�lidos
/// - Dados desconhecidos
/// </summary>
public class HarvestInteractionStrategy : ICardInteractionStrategy
{
    private readonly RunIdentityContext _context;

    public HarvestInteractionStrategy(RunIdentityContext context)
    {
        // Valida��o no construtor
        if (context.Library == null)
            throw new System.ArgumentNullException(nameof(context), 
                "[HarvestInteractionStrategy] RunIdentityContext.Library � nulo!");
        
        if (context.Economy == null)
            throw new System.ArgumentNullException(nameof(context), 
                "[HarvestInteractionStrategy] RunIdentityContext.Economy � nulo!");
        
        _context = context;
    }

    public bool CanInteract(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlotReadOnly(index);

        // Null checks robustos
        if (slot == null || card == null)
            return false;

        // Prote��es b�sicas
        if (slot.IsEmpty || slot.IsWithered)
            return false;

        // IMPORTANTE: N�o apenas verificar CurrentGrowth,
        // mas garantir que a planta foi plantada h� pelo menos 1 dia
        // Uma planta no DIA 0 n�o pode ser colhida mesmo que acelerada
        if (_context.Library.TryGetCrop(slot.CropID, out var cropData))
        {
            return slot.CurrentGrowth >= cropData.DaysToMature;
        }

        return false;
    }

    public InteractionResult Execute(int index, IGridService grid, CardData card)
    {
        var slot = grid.GetSlot(index);

        // Valida��es defensivas
        if (slot == null)
             return InteractionResult.Fail("[ERRO] CropState n�o encontrado!");

        if (card == null)
            return InteractionResult.Fail("[ERRO] CardData � null!");

        if (slot.IsEmpty)
            return InteractionResult.Fail("Nada para colher.");

        if (slot.IsWithered)
            return InteractionResult.Fail("A planta est� morta e n�o pode ser colhida.");

        // 1. Busca dados com erro tratado
        if (!_context.Library.TryGetCrop(slot.CropID, out CropData cropData))
        {
            Debug.LogWarning($"[HarvestInteractionStrategy] Crop desconhecido: {slot.CropID}");
            return InteractionResult.Fail("Dados da planta n�o encontrados no sistema.");
        }

        if (cropData == null)
            return InteractionResult.Fail("Dados da planta corrompidos.");

        // 2. Valida��o de Matura��o
        bool isMature = slot.CurrentGrowth >= cropData.DaysToMature;
        if (!isMature)
        {
            int remainingDays = cropData.DaysToMature - slot.CurrentGrowth;
            return InteractionResult.Fail($"A planta ainda n�o est� madura! Faltam {remainingDays} dias.");
        }

        // 3. Valida��o de Tempo M�nimo
        // Garante que pelo menos 1 dia passou (CurrentGrowth > 0)
        if (slot.CurrentGrowth <= 0)
            return InteractionResult.Fail("A planta foi plantada hoje. Espere at� amanh�.");

        // 4. Economia - com prote��o contra valores inv�lidos
        int value = cropData.BaseSellValue;
        if (value < 0)
        {
            Debug.LogWarning($"[HarvestInteractionStrategy] Valor negativo para {cropData.Name}! Usando 0.");
            value = 0;
        }

        // ? VALIDA��O EXTRA: Garantir que EconomyService est� dispon�vel
        if (_context.Economy == null)
        {
            Debug.LogError("[HarvestInteractionStrategy] EconomyService � NULL! N�o � poss�vel processar colheita.");
            return InteractionResult.Fail("Sistema econ�mico n�o dispon�vel. Tente novamente.");
        }

        try
        {
            int moneyBefore = _context.Economy.CurrentMoney;
            _context.Economy.Earn(value, TransactionType.Harvest);
            int moneyAfter = _context.Economy.CurrentMoney;
            
            // Valida��o p�s-transa��o: verificar se o dinheiro realmente aumentou
            if (moneyAfter < moneyBefore + value)
            {
                Debug.LogError($"[HarvestInteractionStrategy] ERRO: Dinheiro n�o foi adicionado corretamente! Antes: ${moneyBefore}, Depois: ${moneyAfter}, Esperado: ${moneyBefore + value}");
                return InteractionResult.Fail("Erro ao processar pagamento. Tente novamente.");
            }
            
            Debug.Log($"[HarvestInteractionStrategy] ? Colheita processada: +${value} (${moneyBefore} -> ${moneyAfter})");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HarvestInteractionStrategy] Erro ao ganhar moedas: {ex.Message}");
            Debug.LogError($"StackTrace: {ex.StackTrace}");
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

