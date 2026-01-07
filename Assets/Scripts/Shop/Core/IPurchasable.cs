using UnityEngine;

public interface IPurchasable
{
    string DisplayName { get; }
    string Description { get; }
    Sprite Icon { get; }
    int Price { get; }

    // Validação: Retorna o motivo da falha ou None se puder comprar
    PurchaseFailReason CanPurchase(PurchaseContext ctx);

    // Ação: Executa a lógica de compra
    void OnPurchased(PurchaseContext ctx);
}