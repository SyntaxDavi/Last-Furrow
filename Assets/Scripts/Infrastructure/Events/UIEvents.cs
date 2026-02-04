using System;

public class UIEvents
{
    // Eventos de Saída (Flow -> UI)
    public event Action<HUDMode> OnHUDModeChanged;
    public event Action<bool> OnToggleHandVisibility; // (Legado, pode manter por enquanto)

    // Eventos de Entrada (Input -> Lógica)
    public event Action OnToggleShopRequested;
    public event Action OnExitWeekendRequested;

    // Disparadores
    public void RequestHUDMode(HUDMode mode) => OnHUDModeChanged?.Invoke(mode);
    public void RequestToggleShop() => OnToggleShopRequested?.Invoke();
    public void RequestExitWeekend() => OnExitWeekendRequested?.Invoke();
    public void TriggerToggleHand(bool show) => OnToggleHandVisibility?.Invoke(show);
}