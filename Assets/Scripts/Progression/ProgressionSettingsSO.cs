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

    public int GetGoalForWeek(int weekNumber)
    {
        return BaseWeeklyGoal + ((weekNumber - 1) * GoalIncreasePerWeek);
    }
}