/// <summary>
/// Implementação padrão das regras de tempo.
/// 
/// REGRAS:
/// - Dias 1-5: Produção (pode dormir)
/// - Dias 6-7: Fim de semana (não pode dormir, usa "Trabalhar" da loja)
/// - Dia 5: Último dia antes do fim de semana
/// 
/// FUTURO:
/// Criar variantes:
/// - RelaxedTimePolicy (permite dormir no fim de semana)
/// - EventTimePolicy (altera baseado em eventos especiais)
/// </summary>
public class DefaultTimePolicy : ITimePolicy
{
    private const int DAYS_IN_PRODUCTION = 5;
    private const int DAY_WEEKEND_START = 6;
    private const int DAY_WEEKEND_END = 7;

    public bool CanSleep(int currentDay, RunPhase currentPhase)
    {
        // Só pode dormir durante produção (Dias 1-5)
        if (currentPhase != RunPhase.Production)
            return false;

        // Não pode dormir no fim de semana
        if (IsWeekend(currentDay))
            return false;

        return true;
    }

    public bool IsWeekend(int currentDay)
    {
        return currentDay >= DAY_WEEKEND_START && currentDay <= DAY_WEEKEND_END;
    }

    public bool IsLastProductionDay(int currentDay)
    {
        return currentDay == DAYS_IN_PRODUCTION;
    }
}
