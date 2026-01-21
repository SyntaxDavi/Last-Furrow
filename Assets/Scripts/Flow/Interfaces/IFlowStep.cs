using Cysharp.Threading.Tasks;

/// <summary>
/// Interface: Define contrato para steps do pipeline.
/// </summary>
public interface IFlowStep
{
    /// <summary>
    /// Executa o step do pipeline.
    /// Step pode abortar pipeline via control.AbortPipeline().
    /// </summary>
    UniTask Execute(FlowControl control);
    
    /// <summary>
    /// Permite pular steps condicionalmente sem erro.
    /// Default: true (sempre executável).
    /// </summary>
    bool CanExecute() => true;
    
    /// <summary>
    /// Nome do step para logs e telemetry.
    /// Default: Nome da classe.
    /// </summary>
    string GetStepName() => GetType().Name;
}
