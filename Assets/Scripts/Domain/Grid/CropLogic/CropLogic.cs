public struct CropSimulationResult
{
    public GrowthEventType EventType;
    public string DebugMessage; // Apenas para log, n�o para UI final
}

public static class CropLogic
{
    /// <summary>
    /// Processa o ciclo natural da noite (Envelhecimento).
    /// 
    /// NOTA: A remo��o de �gua (IsWatered = false) � feita por GridService.ProcessNightCycleForSlot
    /// antes de chamar este m�todo, para garantir que TODOS os slots sejam processados corretamente.
    /// </summary>
    public static CropSimulationResult ProcessNightlyGrowth(CropState state, CropData data)
    {
        var result = new CropSimulationResult { EventType = GrowthEventType.None };

        // Agora sim, verificamos se vale a pena processar biologia
        if (state.IsWithered || state.IsEmpty || data == null)
            return result;

        bool wasMatureBefore = state.CurrentGrowth >= data.DaysToMature;

        if (!wasMatureBefore)
        {
            // FASE DE CRESCIMENTO
            state.CurrentGrowth++;

            if (state.CurrentGrowth >= data.DaysToMature)
            {
                result.EventType = GrowthEventType.Matured;
                result.DebugMessage = "A planta amadureceu durante a noite.";
            }
            else
            {
                result.EventType = GrowthEventType.Growing;
            }
        }
        else
        {
            // FASE DE JANELA DE FRESCOR (ENVELHECIMENTO)
            state.DaysMature++;

            if (state.DaysMature > data.FreshnessWindow)
            {
                state.IsWithered = true;
                result.EventType = GrowthEventType.WitheredByAge;
                result.DebugMessage = "A planta apodreceu de velha.";
            }
            else if (state.DaysMature == data.FreshnessWindow)
            {
                result.EventType = GrowthEventType.LastFreshDayWarning;
                result.DebugMessage = "Cuidado! Vai apodrecer amanh�.";
            }
        }

        return result;
    }

    /// <summary>
    /// Aplica acelera��o externa (�gua/Fertilizante).
    /// Centraliza a regra de "Risco T�tico".
    /// </summary>
    public static CropSimulationResult ApplyAcceleration(CropState state, CropData data, int amount)
    {
        var result = new CropSimulationResult { EventType = GrowthEventType.None };

        if (state.IsWithered || state.IsEmpty || data == null)
            return result;

        bool isMature = state.CurrentGrowth >= data.DaysToMature;

        if (isMature)
        {
            // REGRA: Se j� est� madura, �gua consome a janela de frescor.
            // RISCO T�TICO: Se acelerar no �ltimo dia, morre instantaneamente.

            // Verifica se a acelera��o empurra para al�m do limite
            if (state.DaysMature + amount > data.FreshnessWindow)
            {
                state.IsWithered = true;
                result.EventType = GrowthEventType.WitheredByOverdose;
                result.DebugMessage = "�gua demais na fase final matou a planta!";
            }
            else
            {
                state.DaysMature += amount;
                result.EventType = GrowthEventType.LastFreshDayWarning; // Assumindo que acelerar sempre aproxima do fim
                result.DebugMessage = $"Janela de frescor reduzida em {amount} dias.";
            }
        }
        else
        {
            // CRESIMENTO ACELERADO
            state.CurrentGrowth += amount;

            // Checa se maturou com a acelera��o
            if (state.CurrentGrowth >= data.DaysToMature)
            {
                result.EventType = GrowthEventType.Matured;
                // Nota: Opcionalmente, o excedente poderia virar DaysMature aqui
                // mas vamos manter simples: teta na matura��o.
            }
            else
            {
                result.EventType = GrowthEventType.Growing;
            }
        }

        return result;
    }
}   