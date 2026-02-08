using UnityEngine;

/// <summary>
/// Implementa��o de calend�rio baseada em valores primitivos.
/// Encapsula toda a l�gica temporal da Run.
/// 
/// ARQUITETURA: Recebe int ao invs de ScriptableObject para evitar
/// problemas de assembly e manter independncia do domnio.
/// </summary>
public class RunCalendar : IRunCalendar
{
    private readonly int _productionDays;
    private readonly int _weekendDays;

    /// <summary>
    /// Cria calendrio com valores explcitos.
    /// </summary>
    /// <param name="productionDays">Dias de trabalho (ex: 5)</param>
    /// <param name="weekendDays">Dias de weekend (ex: 2)</param>
    public RunCalendar(int productionDays = 5, int weekendDays = 2)
    {
        _productionDays = productionDays > 0 ? productionDays : 5;
        _weekendDays = weekendDays >= 0 ? weekendDays : 2;
    }

    public int ProductionDays => _productionDays;
    public int WeekendStartDay => _productionDays + 1;
    public int DaysPerCycle => _productionDays + _weekendDays;

    public bool IsProductionDay(int day) => day >= 1 && day <= _productionDays;
    public bool IsWeekendStart(int day) => day == WeekendStartDay;
    public bool IsPastCycle(int day) => day > WeekendStartDay;

    public RunPhase GetPhaseForDay(int day)
    {
        return IsProductionDay(day) ? RunPhase.Production : RunPhase.Weekend;
    }
}
