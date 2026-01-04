using System;

public class UIEvents
{
    public event Action<bool> OnToggleHandVisibility;

    public void TriggerToggleHand(bool show) => OnToggleHandVisibility?.Invoke(show);
}