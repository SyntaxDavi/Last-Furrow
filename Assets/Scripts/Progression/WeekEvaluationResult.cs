public enum WeekResultType
{
    Success,      // >= 100%
    PartialFail,  // 60% - 99%
    CriticalFail  // < 60%
}
public struct WeekEvaluationResult
{
    public bool IsWeekEnd;
    public WeekResultType ResultType;
    public int NextGoal; // Calculado aqui para evitar lógica espalhada

    public static WeekEvaluationResult NotEndOfWeek()
        => new WeekEvaluationResult { IsWeekEnd = false };

    public static WeekEvaluationResult Finished(WeekResultType type, int nextGoal)
        => new WeekEvaluationResult
        {
            IsWeekEnd = true,
            ResultType = type,
            NextGoal = nextGoal
        };
}