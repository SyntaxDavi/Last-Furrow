using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionSettings", menuName = "Last Furrow/Settings/Progression")]
public class ProgressionSettingsSO : ScriptableObject
{
    [Header("Estrutura Temporal")]
    [Tooltip("Quantos dias o jogador trabalha e gera pontos? (Ex: 5)")]
    [Min(1)] public int ProductionDays = 5;

    [Tooltip("Quantos dias dura o fim de semana (loja)? (Ex: 2)")]
    [Min(0)] public int WeekendDays = 2;

    // Propriedade calculada: Ciclo Total (5 + 2 = 7 dias)
    public int DaysPerCycle => ProductionDays + WeekendDays;

    [Header("Curva de Metas")]
    public int BaseWeeklyGoal = 150;
    public int GoalIncreasePerWeek = 50;

    [Header("Sobrevivência")]
    public int MaxLives = 3;

    [Header("Metas Manuais (7 Semanas)")]
    [Tooltip("Define as 7 metas semanais manualmente. Permite curvas de dificuldade não-lineares.")]
    public int[] ManualWeeklyGoals = new int[] { 100, 150, 200, 220, 280, 350, 450 };

    [Tooltip("Se true, usa ManualWeeklyGoals. Se false, usa fórmula linear.")]
    public bool UseManualGoals = true;

    /// <summary>
    /// Retorna a meta base para uma semana específica.
    /// Traditions podem modificar este valor via GoalModifier.
    /// </summary>
    public int GetGoalForWeek(int weekNumber)
    {
        if (UseManualGoals && ManualWeeklyGoals != null && ManualWeeklyGoals.Length > 0)
        {
            int index = Mathf.Clamp(weekNumber - 1, 0, ManualWeeklyGoals.Length - 1);
            return ManualWeeklyGoals[index];
        }

        // Fallback para fórmula linear
        return BaseWeeklyGoal + ((weekNumber - 1) * GoalIncreasePerWeek);
    }

    /// <summary>
    /// Número total de semanas para vitória.
    /// </summary>
    public int TotalWeeksToWin => UseManualGoals ? ManualWeeklyGoals.Length : 7;
}