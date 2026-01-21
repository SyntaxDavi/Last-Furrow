using Cysharp.Threading.Tasks;

public class UpdateHUDModeStep : IFlowStep
{
    private readonly IWeekendUIFlow _uiFlow;
    private readonly bool _isWeekend;

    public UpdateHUDModeStep(IWeekendUIFlow uiFlow, bool isWeekend)
    {
        _uiFlow = uiFlow;
        _isWeekend = isWeekend;
    }

    public async UniTask Execute(FlowControl control)
    {
        if (_isWeekend)
        {
            // Configura UI para Loja/Fim de Semana
            _uiFlow.SetupUIForWeekend();
        }
        else
        {
            // Limpa UI e volta para Gameplay/Produção
            _uiFlow.CleanupUIAfterWeekend();
        }

        // Espera 1 frame para garantir que a UI se atualize/reconstrua layout
        await UniTask.Yield();
    }
}