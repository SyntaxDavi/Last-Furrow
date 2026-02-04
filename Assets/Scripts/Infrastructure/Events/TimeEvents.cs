using System;

public class TimeEvents
{
    public event Action<int> OnDayChanged;
    public event Action<int> OnWeekChanged;
    public event Action OnWeekendStarted;
    
    /// <summary>
    /// Disparado quando uma nova semana começa (após Weekend).
    /// Sistemas como PatternTracking escutam este evento para reset semanal.
    /// Arquitetura: Evento de domínio substitui callback direto.
    /// </summary>
    public event Action<int> OnWeekStarted;

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
    public void TriggerWeekStarted(int week) => OnWeekStarted?.Invoke(week);
    public void TriggerRunStarted() => OnRunStarted?.Invoke();
    public void TriggerRunEnded(RunEndReason reason) => OnRunEnded?.Invoke(reason);
    public void TriggerResolutionStarted() => OnResolutionStarted?.Invoke();
    public void TriggerResolutionEnded() => OnResolutionEnded?.Invoke();
    public void TriggerResolutionSequenceComplete() => OnResolutionSequenceComplete?.Invoke();
}
