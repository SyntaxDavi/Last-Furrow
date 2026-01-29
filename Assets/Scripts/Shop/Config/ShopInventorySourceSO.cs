using UnityEngine;
using System.Collections.Generic;

public abstract class ShopInventorySourceSO : ScriptableObject
{
    /// <summary>
    /// Gera uma lista de itens para a loja.
    /// Deve usar o random provider fornecido para garantir determinismo.
    /// </summary>
    public abstract List<IPurchasable> GenerateItems(RunData run, IGameLibrary library, IRandomProvider random);
}