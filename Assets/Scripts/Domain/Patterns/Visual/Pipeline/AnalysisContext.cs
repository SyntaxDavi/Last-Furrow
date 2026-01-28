namespace LastFurrow.Domain.Patterns.Visual.Pipeline
{
    /// <summary>
    /// Contexto compartilhado entre as fases do pipeline.
    /// </summary>
    public class AnalysisContext
    {
        public IGridService GridService { get; set; }
        public RunData RunData { get; set; }
        public DayAnalysisResult PreCalculatedResult { get; set; }
        public GridSlotView[] SlotViews { get; set; }
        public GameEvents Events { get; set; }

        /// <summary>
        /// Score acumulado durante o processamento (inicia com o score atual da run).
        /// </summary>
        public int RunningScore { get; set; }
    }
}
