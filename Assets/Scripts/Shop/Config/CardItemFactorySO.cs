using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Factories/Card Item")]
public class CardItemFactorySO : ShopItemFactorySO
{
    [Header("Configuração")]
    [SerializeField] private CardData _card;

    [Header("Economia")]
    [Tooltip("Se marcado, ignora o custo da carta e usa o valor abaixo.")]
    [SerializeField] private bool _useCustomPrice = false;  

    // O Inspector só habilita este campo se o bool for true (se usar Editor script)
    // Mas mesmo sem editor custom, fica claro a intenção.
    [SerializeField][Min(0)] private int _customPrice = 10;

    public override IPurchasable CreateItem(RunData context)
    {
        int finalPrice = -1; // -1 sinaliza para o Item usar a lógica padrão dele

        if (_useCustomPrice)
        {
            finalPrice = _customPrice;

            // Exemplo futuro de uso do contexto:
            // if (context.HasRelic("Coupon")) finalPrice /= 2;
        }

        return new CardPurchaseItem(_card, finalPrice);
    }
}