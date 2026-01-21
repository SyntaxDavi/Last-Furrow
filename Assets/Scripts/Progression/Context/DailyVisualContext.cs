/// <summary>
/// Context Pattern: Agrupa todas as dependências VISUAIS do pipeline diário.
/// Resolve problema de FindFirstObjectByType (service locator visual).
/// Injetado via Inspector no DailyResolutionSystem.
/// </summary>
public class DailyVisualContext
{
    // Visual Controllers (MonoBehaviours da cena)
    public AnalyzingPhaseController Analyzer { get; }
    public GridSlotScanner Scanner { get; }
    public PatternUIManager UIManager { get; }
    
    public DailyVisualContext(
        AnalyzingPhaseController analyzer,
        GridSlotScanner scanner,
        PatternUIManager uiManager)
    {
        Analyzer = analyzer;
        Scanner = scanner;
        UIManager = uiManager;
    }
    
    /// <summary>
    /// Valida se todas as dependências visuais foram injetadas.
    /// </summary>
    public bool IsValid()
    {
        return Analyzer != null && Scanner != null && UIManager != null;
    }
}
