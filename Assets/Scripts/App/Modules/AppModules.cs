using UnityEngine;

public class CoreModule : BaseModule
{
    public CoreModule(ServiceRegistry registry, AppCore app) : base(registry, app) { }

    public override void Initialize()
    {
        var saveManager = App.SaveManager;
        var gridConfig = App.GridConfiguration;
        var events = App.Events;

        // 1. Inicializa MonoBehaviours Básicos
        if (App.GameStateManager == null) 
            App.GameStateManager = App.GetComponent<GameStateManager>() ?? App.gameObject.AddComponent<GameStateManager>();
        
        App.InputManager.Initialize();
        App.AudioManager.Initialize();

        // 2. Registra no Registry
        Registry.RegisterCore(saveManager, gridConfig, events, App.GameLibrary);
        Registry.RegisterSystems(App.GameStateManager, App.TimeManager, App.InputManager);

        // 3. Inicializa SaveManager
        if (gridConfig != null)
            saveManager.Initialize(gridConfig);
        else
            saveManager.Initialize();

        Debug.Log("[CoreModule] ✓ Inicializado com sucesso.");
    }
}

public class DomainModule : BaseModule
{
    private readonly ProgressionSettingsSO _progressionSettings;

    public DomainModule(ServiceRegistry registry, AppCore app, ProgressionSettingsSO settings) : base(registry, app)
    {
        _progressionSettings = settings;
    }

    public override void Initialize()
    {
        // 1. Cria calendário com valores do ProgressionSettings
        int productionDays = _progressionSettings != null ? _progressionSettings.ProductionDays : 5;
        int weekendDays = _progressionSettings != null ? _progressionSettings.WeekendDays : 2;
        var calendar = new RunCalendar(productionDays, weekendDays);

        // 2. Inicializa RunManager com calendário injetado
        App.RunManager.Initialize(
            Registry.Save,
            Registry.GridConfig,
            Registry.Events.Time,
            Registry.State,
            calendar,
            _progressionSettings
        );

        // 3. Cria Serviços Puros
        var economy = new EconomyService(Registry.Run, Registry.Save);
        var health = new HealthService(Registry.Save);
        var dailyHand = new DailyHandSystem(Registry.GameLibrary, economy, new SeasonalCardStrategy(), Registry.Events.Player);
        var weeklyGoal = new WeeklyGoalSystem(Registry.GameLibrary, Registry.Events.Progression, _progressionSettings);

        // 4. Registra no Registry
        Registry.RegisterDomain(Registry.Run, economy, health, dailyHand, weeklyGoal);

        Debug.Log("[DomainModule] ✓ Inicializado com RunCalendar configurável.");
    }
}

public class PatternModule : BaseModule
{
    private readonly PatternLibrary _patternLibrary;

    public PatternModule(ServiceRegistry registry, AppCore app, PatternLibrary library) : base(registry, app)
    {
        _patternLibrary = library;
    }

    public override void Initialize()
    {
        if (_patternLibrary == null)
        {
            Debug.LogError("[PatternModule] PatternLibrary não atribuída!");
            return;
        }

        // 1. Cria Factory e Serviços
        var factory = new PatternFactory();
        var detector = new PatternDetector(_patternLibrary, factory);
        var calculator = new PatternScoreCalculator(Registry.GameLibrary);
        var shop = new ShopService(Registry.Economy, Registry.Save, Registry.GameLibrary, Registry.Events, Registry.Health);

        // 2. Registra no Registry
        Registry.RegisterGameplay(shop, detector, calculator);

        Debug.Log("[PatternModule] ✓ Inicializado com sucesso.");
    }
}
