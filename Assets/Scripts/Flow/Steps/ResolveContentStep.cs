using System.Collections;

public class ResolveContentStep : IFlowStep
{
    private readonly IWeekendContentResolver _resolver;
    private readonly RunData _runData;

    public ResolveContentStep(IWeekendContentResolver resolver, RunData runData)
    {
        _resolver = resolver;
        _runData = runData;
    }

    public IEnumerator Execute()
    {
        // Aqui o resolver decide se abre a Loja Normal, Loja Especial, Evento, etc.
        _resolver.ResolveContent(_runData);

        // Se a resolução de conteúdo envolvesse carregar assets pesados (Addressables),
        // faríamos um yield return aqui. Por enquanto, é síncrono.
        yield return null;
    }
}