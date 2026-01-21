public class FlowControl
{
    public bool ShouldAbort { get; private set; }
    public string AbortReason { get; private set; }
    public System.Exception LastError { get; private set; }

    /// <summary>
    /// Chamado pelo Step quando algo crítico acontece (Game Over, erro fatal).
    /// </summary>
    public void AbortPipeline(string reason = "Motivo não especificado")
    {
        ShouldAbort = true;
        AbortReason = reason;
        UnityEngine.Debug.Log($"[FlowControl] Pipeline abortado: {reason}");
    }
    
    /// <summary>
    /// Registra erro mas não aborta necessariamente (step não-crítico).
    /// </summary>
    public void LogError(System.Exception exception)
    {
        LastError = exception;
    }
    
    /// <summary>
    /// Verifica se houve algum erro registrado.
    /// </summary>
    public bool HasError => LastError != null;
}