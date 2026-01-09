using System.Collections;

public class ChangeStateStep : IFlowStep
{
    private readonly IWeekendStateFlow _stateFlow;
    private readonly bool _enterWeekend;

    // Injeção de Dependência via Construtor
    public ChangeStateStep(IWeekendStateFlow stateFlow, bool enterWeekend)
    {
        _stateFlow = stateFlow;
        _enterWeekend = enterWeekend;
    }

    public IEnumerator Execute()
    {
        if (_enterWeekend)
        {
            _stateFlow.EnterWeekendState(); // Ex: GameState.Shopping
        }
        else
        {
            _stateFlow.ExitWeekendState(); // Ex: GameState.Playing
        }

        // Como mudança de estado é instantânea, esperamos um frame para garantir sincronia
        yield return null;
    }
}