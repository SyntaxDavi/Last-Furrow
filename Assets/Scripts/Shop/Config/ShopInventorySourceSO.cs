using UnityEngine;
using System.Collections.Generic;

public abstract class ShopInventorySourceSO : ScriptableObject
{
    /// <summary>
    /// Gera uma lista de itens baseada no contexto atual.
    /// </summary>
    public abstract IEnumerable<IPurchasable> GenerateItems(RunData run, IGameLibrary library);
}