using UnityEngine;
using System.Collections.Generic;

public class WeekendContentResolver : IWeekendContentResolver
{
    private readonly ShopService _shopService;
    private readonly ShopProfileSO _defaultShop;
    private readonly List<ShopProfileSO> _specialShops;

    public WeekendContentResolver(
        ShopService shopService,
        ShopProfileSO defaultShop,
        List<ShopProfileSO> specialShops)
    {
        _shopService = shopService;
        _defaultShop = defaultShop;
        _specialShops = specialShops;
    }

    public void ResolveContent(RunData currentRun)
    {
        // 1. DECISÃO: Qual perfil de loja vamos usar hoje?
        ShopProfileSO selectedProfile = SelectProfileLogic(currentRun);

        Debug.Log($"[WeekendResolver] Abrindo loja: {selectedProfile.ShopTitle}");

        IShopStrategy strategy = new ConfigurableShopStrategy(selectedProfile);
        _shopService.OpenShop(strategy);
    }

    // Lógica isolada para escolher o perfil (fácil de testar/alterar)  
    private ShopProfileSO SelectProfileLogic(RunData run)
    {
        // Exemplo: A cada 4 semanas, loja especial 0
        // Exemplo: A cada 6 semanas, loja especial 1

        if (_specialShops != null && _specialShops.Count > 0)
        {
            // Se for semana múltipla de 4, pega uma especial (exemplo simples)
            if (run.CurrentWeek % 4 == 0)
            {
                int index = (run.CurrentWeek / 4) % _specialShops.Count;
                return _specialShops[index];
            }
        }

        // Padrão
        return _defaultShop;
    }
}