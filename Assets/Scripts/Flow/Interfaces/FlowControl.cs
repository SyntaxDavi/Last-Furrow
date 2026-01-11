public class FlowControl
{
    public bool ShouldAbort { get; private set; }

    // Chamado pelo Step quando algo crítico acontece (Game Over)
    public void AbortPipeline()
    {
        ShouldAbort = true;
    }
}