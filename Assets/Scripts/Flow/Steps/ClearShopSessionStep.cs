using Cysharp.Threading.Tasks;
using UnityEngine;

public class ClearShopSessionStep : IFlowStep
{
    private readonly ShopService _shopService;

    public ClearShopSessionStep(ShopService shopService)
    {
        _shopService = shopService;
    }

    public async UniTask Execute(FlowControl control)
    {
        if (_shopService != null)
        {
            _shopService.CloseShop();
        }
        await UniTask.Yield();
    }
}

