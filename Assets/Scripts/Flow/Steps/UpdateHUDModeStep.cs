using System.Collections;

public class UpdateHUDModeStep : IFlowStep
{
    private readonly IWeekendUIFlow _uiFlow;
    private readonly bool _isWeekend;

    public UpdateHUDModeStep(IWeekendUIFlow uiFlow, bool isWeekend)
    {
        _uiFlow = uiFlow;
        _isWeekend = isWeekend;
    }

    public IEnumerator Execute()
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

        yield return null;
    }
}