using System;
using System.Collections.Generic;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline
{
    /// <summary>
    /// Resultado de uma fase individual.
    /// </summary>
    public struct PhaseResult
    {
        public bool Success;
        public int ScoreDelta;
        public string Message;
    }

    /// <summary>
    /// Progresso de uma fase (para UI/Loading).
    /// </summary>
    public struct PhaseProgress
    {
        public string PhaseName;
        public float Percentage; // 0.0 a 1.0
        public string CurrentAction;
    }

    /// <summary>
    /// Relatório final da análise completa.
    /// </summary>
    public class AnalysisReport
    {
        public bool Success;
        public bool Cancelled;
        public string Error;
        public int TotalScoreDelta;
        public List<PhaseResult> PhaseResults = new List<PhaseResult>();
    }
}
