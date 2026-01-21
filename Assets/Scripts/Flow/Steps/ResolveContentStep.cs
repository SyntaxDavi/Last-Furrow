using Cysharp.Threading.Tasks;

public class ResolveContentStep : IFlowStep
{
    private readonly IWeekendContentResolver _resolver;
    private readonly RunData _runData;

    public ResolveContentStep(IWeekendContentResolver resolver, RunData runData)
    {
        _resolver = resolver;
        _runData = runData;
    }

    public async UniTask Execute(FlowControl control)
    {
        // Aqui o resolver decide se abre a Loja Normal, Loja Especial, Evento, etc.
        _resolver.ResolveContent(_runData);

        // Se a resolução de conteúdo envolvesse carregar assets pesados (Addressables),
        // usaríamos await aqui. Por enquanto, esperamos 1 frame.
        await UniTask.Yield();
    }
}