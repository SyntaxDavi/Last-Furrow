using UnityEngine;

public class MoneyView : UIView
{
    private void OnEnable()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.UI.OnHUDModeChanged += HandleHUDMode;
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.UI.OnHUDModeChanged -= HandleHUDMode;
    }

    private void HandleHUDMode(HUDMode mode)
    {
        // Regra de Negócio Visual:
        // O dinheiro deve aparecer durante o jogo e na loja.
        // Deve sumir em cutscenes (Hidden).

        bool shouldShow = (mode == HUDMode.Production || mode == HUDMode.Shopping);

        if (shouldShow) Show();
        else Hide();
    }
}