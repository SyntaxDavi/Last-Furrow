using System.Collections.Generic;

public class DefaultWeekendFlowBuilder : IWeekendFlowBuilder
{
    // Dependências (Os "ingredientes" que os passos precisam)
    private readonly IWeekendStateFlow _stateFlow;
    private readonly IWeekendUIFlow _uiFlow;
    private readonly IWeekendContentResolver _contentResolver;

    // Injeção via Construtor (Puro C#)
    public DefaultWeekendFlowBuilder(
        IWeekendStateFlow stateFlow,
        IWeekendUIFlow uiFlow,
        IWeekendContentResolver contentResolver)
    {
        _stateFlow = stateFlow;
        _uiFlow = uiFlow;
        _contentResolver = contentResolver;
    }

    public List<IFlowStep> BuildEnterPipeline(RunData runData)
    {
        var pipeline = new List<IFlowStep>();

        // 1. Fade Out
        pipeline.Add(new ScreenFadeStep(false, 0.5f));

        // 2. Mudanças de Bastidores
        pipeline.Add(new ChangeStateStep(_stateFlow, true));
        pipeline.Add(new UpdateHUDModeStep(_uiFlow, true));

        // 3. Decisão de Conteúdo
        // No futuro: if (runData.CurrentWeek == 1) pipeline.Add(new TutorialStep());
        pipeline.Add(new ResolveContentStep(_contentResolver, runData));

        // 4. Fade In
        pipeline.Add(new ScreenFadeStep(true, 0.5f));

        return pipeline;
    }

    public List<IFlowStep> BuildExitPipeline(RunData runData)
    {
        return new List<IFlowStep>
        {
            new ScreenFadeStep(false, 0.5f),
            new ChangeStateStep(_stateFlow, false),
            new UpdateHUDModeStep(_uiFlow, false),
            new ScreenFadeStep(true, 0.5f)
        };
    }
}