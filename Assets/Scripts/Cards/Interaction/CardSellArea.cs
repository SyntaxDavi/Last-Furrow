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
            // 1. Executa a Venda ($$)
            SellCard(cardView.Data);

            // 2. REMOÇÃO LÓGICA E VISUAL (A Correção é aqui)
            // Removemos a instância específica. O HandManager ouve isso e destrói o objeto.
            AppCore.Instance.Events.Player.TriggerCardRemoved(cardView.Instance);

            // Opcional: Manter o evento de consumo se você usa para quests/estatísticas
            // AppCore.Instance.Events.Player.TriggerCardConsumed(cardView.Data.ID);

            // 3. Feedback Visual
            if (_sellEffect != null) _sellEffect.Play();
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