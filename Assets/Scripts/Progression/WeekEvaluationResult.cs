public struct WeekEvaluationResult
{
    public bool IsWeekEnd;      // Hoje foi dia de pagamento?
    public bool IsSuccess;      // Passou na meta?
    public int ScoreAchieved;   // Quantos pontos fez
    public int TargetGoal;      // Qual era a meta
    public int NextGoal;        // Qual a próxima meta

    // Factory methods para clareza
    public static WeekEvaluationResult NotEndOfWeek()
        => new WeekEvaluationResult { IsWeekEnd = false };

    public static WeekEvaluationResult Finished(bool success, int score, int target, int next)
        => new WeekEvaluationResult
        {
            IsWeekEnd = true,
            IsSuccess = success,
            ScoreAchieved = score,
            TargetGoal = target,
            NextGoal = next
        };
}