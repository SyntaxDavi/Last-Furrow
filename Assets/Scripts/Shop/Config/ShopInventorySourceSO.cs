using UnityEngine;
using System.Collections.Generic;

public abstract class ShopInventorySourceSO : ScriptableObject
{
    public abstract List<IPurchasable> GenerateItems(RunData run, IGameLibrary library);
}