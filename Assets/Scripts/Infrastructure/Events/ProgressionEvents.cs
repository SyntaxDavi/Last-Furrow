using System;

public class ProgressionEvents
{
    // Atualização visual da barra (Score Atual, Meta Total)
    public event Action<int, int> OnScoreUpdated;

    // Fim de Semana: (Sucesso?, Vidas Restantes)
    public event Action<bool, int> OnWeeklyGoalEvaluated;

    // Mudança de Vidas (Dano ou Cura)
    public event Action<int> OnLivesChanged;

    // Vitória: Todas as semanas completadas
    public event Action OnVictory;

    public void TriggerScoreUpdated(int current, int target) => OnScoreUpdated?.Invoke(current, target);
    public void TriggerWeeklyGoalEvaluated(bool success, int lives) => OnWeeklyGoalEvaluated?.Invoke(success, lives);
    public void TriggerLivesChanged(int lives) => OnLivesChanged?.Invoke(lives);
    public void TriggerVictory() => OnVictory?.Invoke();
}