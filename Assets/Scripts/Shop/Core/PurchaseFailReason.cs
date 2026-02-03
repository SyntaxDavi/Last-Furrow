public enum PurchaseFailReason
{
    None,
    InsufficientFunds,
    ConditionsNotMet, // Ex: Mao cheia
    PlayerLifeFull,  // Erro especifico para pocao/cura
    ItemUnavailable
}
