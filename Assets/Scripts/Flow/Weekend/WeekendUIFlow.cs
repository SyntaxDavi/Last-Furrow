using UnityEngine;
using static UnityEngine.Rendering.DebugManager;

public class WeekendUIFlow : IWeekendUIFlow
{
    private readonly UIEvents _uiEvents;

    public WeekendUIFlow(UIEvents uiEvents)
    {
        _uiEvents = uiEvents;
    }

    public void SetupUIForWeekend()
    {
        // Avisa a UI para entrar no modo "Shopping"
        // (Esconder mão, mostrar dinheiro em destaque, preparar layout de loja)
        _uiEvents.RequestHUDMode(HUDMode.Shopping);
    }

    public void CleanupUIAfterWeekend()
    {
        // Avisa a UI para voltar ao modo "Produção"
        // (Mostrar mão, layout de fazenda)
        _uiEvents.RequestHUDMode(HUDMode.Production);
    }
}