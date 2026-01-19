using UnityEngine;

/// <summary>
/// Implementação padrão de IDropValidator.
/// Valida todas as regras de negócio para uso de cartas no grid.
/// 
/// REGRAS VALIDADAS:
/// 1. GameState deve ser Playing (não valida durante Shopping/Weekend)
/// 2. Slot deve estar desbloqueado
/// 3. Regras específicas do GridService (vazio, tipo correto, etc)
/// 
/// ARQUITETURA:
/// - Não conhece a UI (não sabe de hover, highlight, etc)
/// - Delega validação de domínio para GridService
/// - Centraliza lógica de mensagens de erro
/// </summary>
public class DefaultDropValidator : IDropValidator
{
    private readonly IGridService _gridService;
    private readonly GameStateManager _gameStateManager;
    
    private string _lastErrorMessage = string.Empty;

    public DefaultDropValidator(IGridService gridService, GameStateManager gameStateManager)
    {
        _gridService = gridService ?? throw new System.ArgumentNullException(nameof(gridService));
        _gameStateManager = gameStateManager ?? throw new System.ArgumentNullException(nameof(gameStateManager));
    }

    public bool CanDrop(int slotIndex, CardData cardData)
    {
        if (cardData == null)
        {
            _lastErrorMessage = "Carta invalida";
            return false;
        }

        // Validação 1: GameState
        GameState currentState = _gameStateManager.CurrentState;
        if (currentState != GameState.Playing)
        {
            _lastErrorMessage = $"Nao e possivel usar cartas durante {currentState}";
            return false;
        }

        // Validação 2: Slot desbloqueado
        if (!_gridService.IsSlotUnlocked(slotIndex))
        {
            _lastErrorMessage = "Este slot esta bloqueado. Desbloqueie primeiro.";
            return false;
        }

        // Validação 3: Regras de domínio (GridService)
        bool canReceive = _gridService.CanReceiveCard(slotIndex, cardData);
        
        if (!canReceive)
        {
            // GridService não retorna mensagem, então inferimos o motivo
            var state = _gridService.GetSlotReadOnly(slotIndex);
            
            if (state != null && state.CropID.IsValid)
            {
                if (cardData.Type == CardType.Plant)
                {
                    _lastErrorMessage = "Ja existe uma planta neste slot";
                }
                else if (cardData.Type == CardType.Modify || cardData.Type == CardType.Care)
                {
                    if (state.IsWatered)
                    {
                        _lastErrorMessage = "Este slot ja esta regado";
                    }
                    else
                    {
                        _lastErrorMessage = "Nao e possivel regar agora";
                    }
                }
                else
                {
                    _lastErrorMessage = "Acao nao permitida neste slot";
                }
            }
            else if (cardData.Type == CardType.Modify || cardData.Type == CardType.Care)
            {
                _lastErrorMessage = "Nao ha planta para regar";
            }
            else
            {
                _lastErrorMessage = "Acao nao permitida";
            }
        }

        return canReceive;
    }

    public string GetErrorMessage()
    {
        return _lastErrorMessage;
    }
}

