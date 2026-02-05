using UnityEngine;

/// <summary>
/// SOLID: Single Responsibility Principle
/// Centraliza todas as regras de negócio sobre quando cartas devem ser distribuídas.
/// 
/// REGRAS:
/// - Dias 1-5 (Production): Ganha cartas toda manhã
/// - Weekend (Dias 6-7): NÃO ganha cartas
/// - Semana 2+: Ganha cartas na manhã do dia 1, APÓS o shop (no pipeline de saída do weekend)
/// </summary>
public class CardDrawPolicy
{
    private const int DAYS_IN_PRODUCTION = 5;
    private const int WEEKEND_START_DAY = 6;

    /// <summary>
    /// Determina se cartas devem ser distribuídas no momento atual.
    /// </summary>
    public bool ShouldDrawCards(RunData runData, RunPhase currentPhase)
    {
        if (runData == null) return false;

        // REGRA 1: Weekend nunca ganha cartas
        if (currentPhase == RunPhase.Weekend)
        {
            return false;
        }

        // REGRA 2: Dias 1-5 sempre ganham cartas (Production phase)
        // FIX: Inclui Dia 1 de qualquer semana SE ainda não deu draw hoje
        if (runData.CurrentDay >= 1 && runData.CurrentDay <= DAYS_IN_PRODUCTION)
        {
            // A idempotência do DailyHandSystem.ProcessDailyDraw já previne double draw
            // via HasDrawnDailyHand, então não precisamos bloquear aqui.
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determina se cartas devem ser distribuídas após o shop (semana 2+).
    /// </summary>
    public bool ShouldDrawCardsAfterShop(RunData runData)
    {
        if (runData == null) return false;

        // Apenas semana 2+ no dia 1 (após shop)
        return runData.CurrentWeek > 1 && runData.CurrentDay == 1;
    }

    /// <summary>
    /// Valida se é um dia válido para produção (1-5).
    /// </summary>
    public bool IsProductionDay(int day)
    {
        return day >= 1 && day <= DAYS_IN_PRODUCTION;
    }
}
