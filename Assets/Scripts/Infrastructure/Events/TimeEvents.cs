using System;

public class TimeEvents
{
    public event Action<int> OnDayChanged;
    public event Action<int> OnWeekChanged;
    public event Action OnWeekendStarted;

    // Ciclo da Run
    public event Action OnRunStarted;
    public event Action<RunEndReason> OnRunEnded;

    // Sequência de Resolução Diária (Cálculos de fim de dia)
    public event Action OnResolutionStarted;
    public event Action OnResolutionEnded;
    public event Action OnResolutionSequenceComplete;

    public void TriggerDayChanged(int day) => OnDayChanged?.Invoke(day);
    public void TriggerWeekChanged(int week) => OnWeekChanged?.Invoke(week);
    public void TriggerWeekendStarted() => OnWeekendStarted?.Invoke();
    public void TriggerRunStarted() => OnRunStarted?.Invoke();
    public void TriggerRunEnded(RunEndReason reason) => OnRunEnded?.Invoke(reason);
    public void TriggerResolutionStarted() => OnResolutionStarted?.Invoke();
    public void TriggerResolutionEnded() => OnResolutionEnded?.Invoke();
    public void TriggerResolutionSequenceComplete() => OnResolutionSequenceComplete?.Invoke();
}
