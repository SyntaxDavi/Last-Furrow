using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Shop Profile")]
public class ShopProfileSO : ScriptableObject
{
    [Header("Configuração Visual")]
    public string ShopTitle = "Mercado";

    [Header("Itens Garantidos")]
    [Tooltip("Itens que SEMPRE aparecem nesta loja (ex: Cura, Pá)")]
    public List<ShopItemFactorySO> GuaranteedItems;

    [Header("Geração de Cartas")]
    public int RandomCardsCount = 3;

    // Futuro: Você pode criar uma "LootTableSO" com pesos para raridade //
    // Por enquanto, usaremos a Library inteira, mas filtrada por "Tier" se quiser
    public bool IncludeRandomCards = true;
}