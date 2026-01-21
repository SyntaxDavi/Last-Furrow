using Cysharp.Threading.Tasks; 

public class ChangeStateStep : IFlowStep
{
    private readonly IWeekendStateFlow _stateFlow;
    private readonly bool _enterWeekend;

    // Injeção de Dependência via Construtor (Mantida igual)
    public ChangeStateStep(IWeekendStateFlow stateFlow, bool enterWeekend)
    {
        _stateFlow = stateFlow;
        _enterWeekend = enterWeekend;
    }

    public async UniTask Execute(FlowControl control)
    {
        if (_enterWeekend)
        {
            _stateFlow.EnterWeekendState(); // Ex: GameState.Shopping
        }
        else
        {
            _stateFlow.ExitWeekendState(); // Ex: GameState.Playing
        }

        // Equivalente a yield return null: Espera 1 frame para garantir sincronia
        await UniTask.Yield();
    }
}