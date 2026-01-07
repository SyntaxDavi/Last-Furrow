using UnityEngine;

public interface IPurchasable
{
    string DisplayName { get; }
    string Description { get; }
    Sprite Icon { get; }
    int Price { get; }

    PurchaseFailReason CanPurchase(PurchaseContext ctx);
    void OnPurchased(PurchaseContext ctx);
}
