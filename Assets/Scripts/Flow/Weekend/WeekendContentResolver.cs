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
        // 1. Lógica de Decisão: Qual loja abrir?
        ShopProfileSO selectedProfile = _defaultShop;

        // Exemplo: A cada 4 semanas, abre uma loja especial (se houver alguma na lista)
        if (_specialShops != null && _specialShops.Count > 0)
        {
            if (currentRun.CurrentWeek % 4 == 0)
            {
                // Pega a primeira especial (ou poderia ser aleatória / baseada em progresso)
                selectedProfile = _specialShops[0];
                Debug.Log($"[ContentResolver] Semana {currentRun.CurrentWeek}: Abrindo Loja Especial!");
            }
        }

        // 2. Cria a estratégia e executa o serviço
        var strategy = new ConfigurableShopStrategy(selectedProfile);
        _shopService.OpenShop(strategy);
    }
}