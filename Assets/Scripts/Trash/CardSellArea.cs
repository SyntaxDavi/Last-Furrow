using UnityEngine;

public class CardSellArea : MonoBehaviour, IDropTarget
{
    [Header("Configuração")]
    [SerializeField] private ParticleSystem _sellEffect;

    // Removemos a variável fixa "_defaultSellValue = 5"

    public bool CanReceive(IDraggable draggable)
    {
        return draggable is CardView;
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            // 1. Executa a Venda passando os dados da carta
            SellCard(cardView.Data);

            // 2. Avisa o sistema para remover a carta lógica e visual
            AppCore.Instance.Events.Player.TriggerCardConsumed(cardView.Data.ID);

            // 3. Feedback Visual
            if (_sellEffect != null) _sellEffect.Play();
        }
    }

    private void SellCard(CardData card)
    {
        // --- CORREÇÃO AQUI ---
        // Agora lemos o valor definido no ScriptableObject da carta
        int value = card.BaseSellValue;

        // Se por acaso esquecer de configurar no asset, usa 1 como segurança
        if (value <= 0) value = 1;

        AppCore.Instance.EconomyService.Earn(value, TransactionType.CardSale);
    }
}