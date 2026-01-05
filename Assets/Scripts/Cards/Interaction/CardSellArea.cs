using UnityEngine;

public class CardSellArea : MonoBehaviour, IDropTarget
{
    [Header("Configuração")]
    [SerializeField] private ParticleSystem _sellEffect;

    // --- INTERFACE IDROPTARGET ---

    public bool CanReceive(IDraggable draggable)
    {
        // 1. Proteção de Estado (Igual ao GridSlotView)
        // Só aceita interação se estivermos Jogando ou na Loja.
        // Se estiver Pausado ou em Game Over, bloqueia.
        var currentState = AppCore.Instance.GameStateManager.CurrentState;

        bool isAllowedState = (currentState == GameState.Playing || currentState == GameState.Shopping);

        if (!isAllowedState)
        {
            return false;
        }

        // 2. Validação de Tipo
        return draggable is CardView;
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            // 1. Executa a Venda
            SellCard(cardView.Data);

            // 2. Avisa o sistema para remover a carta (Lógica e Visual)
            // O HandManager vai escutar isso e remover a carta da mão e dos dados
            AppCore.Instance.Events.Player.TriggerCardConsumed(cardView.Data.ID);

            // 3. Feedback Visual (Partículas)
            if (_sellEffect != null) _sellEffect.Play();

            // O Feedback de Texto (+Gold) é automático porque a UI escuta o EconomyService
        }
    }

    private void SellCard(CardData card)
    {
        if (card == null) return;

        // Usa o valor do ScriptableObject
        int value = card.BaseSellValue;

        // Segurança para não vender por 0 ou negativo sem querer
        if (value < 0) value = 0;

        // Usa o tipo de transação correto para métricas
        AppCore.Instance.EconomyService.Earn(value, TransactionType.CardSale);
    }
}