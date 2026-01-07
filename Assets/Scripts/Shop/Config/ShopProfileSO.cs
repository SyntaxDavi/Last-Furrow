using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Shop Profile")]
public class ShopProfileSO : ScriptableObject
{
    [Header("Configuração Visual")]
    public string ShopTitle = "Mercado";

    [Header("Fontes de Estoque")]
    [Tooltip("Arraste aqui os módulos que geram itens (Garantidos, Aleatórios, Especiais...)")]
    public List<ShopInventorySourceSO> InventorySources;
}