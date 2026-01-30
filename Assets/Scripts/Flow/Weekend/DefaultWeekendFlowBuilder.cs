using System.Collections.Generic;

public class DefaultWeekendFlowBuilder : IWeekendFlowBuilder
{
    // Dependências (Os "ingredientes" que os passos precisam)
    private readonly IWeekendStateFlow _stateFlow;
    private readonly IWeekendUIFlow _uiFlow;
    private readonly IWeekendContentResolver _contentResolver;
    private readonly ShopService _shopService;
    private readonly DailyHandSystem _handSystem;
    private readonly CardDrawPolicy _drawPolicy;

    // Injeção via Construtor (Puro C#)
    public DefaultWeekendFlowBuilder(
        IWeekendStateFlow stateFlow,
        IWeekendUIFlow uiFlow,
        IWeekendContentResolver contentResolver,
        ShopService shopService,
        DailyHandSystem handSystem = null,
        CardDrawPolicy drawPolicy = null)
    {
        _stateFlow = stateFlow;
        _uiFlow = uiFlow;
        _contentResolver = contentResolver;
        _shopService = shopService;
        _handSystem = handSystem;
        _drawPolicy = drawPolicy ?? new CardDrawPolicy();
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
        var pipeline = new List<IFlowStep>
        {
            new ScreenFadeStep(false, 0.5f),
            new ClearShopSessionStep(_shopService),
            new ChangeStateStep(_stateFlow, false),
            new UpdateHUDModeStep(_uiFlow, false),
            new ScreenFadeStep(true, 0.5f)
        };

        // ? SOLID: Adiciona draw de cartas ap�s shop (semana 2+)
        // Isso garante que cartas sejam dadas na manh� do dia 1 da semana 2+ ap�s o shop
        if (_handSystem != null)
        {
            pipeline.Add(new WeekendCardDrawStep(_handSystem, runData, _drawPolicy));
        }

        return pipeline;
    }
}