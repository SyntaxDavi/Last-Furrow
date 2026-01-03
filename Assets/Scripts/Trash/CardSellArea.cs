using UnityEngine;

// Implementa IDropTarget para ser reconhecido pelo PlayerInteraction
public class CardSellArea : MonoBehaviour, IDropTarget
{
    [Header("Configuração")]
    [SerializeField] private int _defaultSellValue = 5;
    [SerializeField] private ParticleSystem _sellEffect;

    // Parte da Interface IDropTarget
    public bool CanReceive(IDraggable draggable)
    {
        // Só aceita se for uma Carta
        return draggable is CardView;
    }

    // Parte da Interface IDropTarget
    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            // 1. Executa a Venda
            SellCard(cardView.Data);

            // 2. Avisa o sistema que a carta visual foi consumida
            // Isso fará o HandManager remover a carta da mão e do RunData
            AppCore.Instance.Events.Player.TriggerCardConsumed(cardView.Data.ID);

            // 3. Feedback Visual Local
            if (_sellEffect != null) _sellEffect.Play();
        }
    }

    private void SellCard(CardData card)
    {
        // Futuro: Ler o valor de venda de dentro do CardData se existir
        // Por enquanto, valor fixo para "queimar" carta
        int value = _defaultSellValue;

        // Usa o serviço global de economia
        AppCore.Instance.EconomyService.Earn(value, TransactionType.CardSale);
    }
}