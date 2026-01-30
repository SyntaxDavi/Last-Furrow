/// <summary>
/// Context Pattern: Agrupa todas as dependências VISUAIS do pipeline diário.        
/// Resolve problema de FindFirstObjectByType (service locator visual).
/// Injetado via Inspector no DailyResolutionSystem.
/// </summary>
public class DailyVisualContext
{
    // Visual Controllers (MonoBehaviours da cena)
    public AnalyzingPhaseOrchestrator Analyzer { get; }
    public GridSlotScanner Scanner { get; }
    public PatternUIManager UIManager { get; }
    public HandManager HandManager { get; }

    public DailyVisualContext(
        AnalyzingPhaseOrchestrator analyzer,
        GridSlotScanner scanner,
        PatternUIManager uiManager,
        HandManager handManager = null)
    {
        Analyzer = analyzer;
        Scanner = scanner;
        UIManager = uiManager;
        HandManager = handManager;
    }

    /// <summary>
    /// Valida se todas as dependências visuais foram injetadas.
    /// </summary>
    public bool IsValid()
    {
        return Analyzer != null && Scanner != null && UIManager != null;
    }
}
