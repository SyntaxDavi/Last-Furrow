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
    private readonly CardDropLibrarySO _cardDropLibrary;
    private readonly GameSettingsSO _gameSettings;

    public DomainModule(
        ServiceRegistry registry, 
        AppCore app, 
        ProgressionSettingsSO settings, 
        CardDropLibrarySO cardDropLibrary = null,
        GameSettingsSO gameSettings = null) : base(registry, app)
    {
        _progressionSettings = settings;
        _cardDropLibrary = cardDropLibrary;
        _gameSettings = gameSettings;
    }

    public override void Initialize()
    {
        // 1. Cria calendário com valores do ProgressionSettings
        int productionDays = _progressionSettings != null ? _progressionSettings.ProductionDays : 5;
        int weekendDays = _progressionSettings != null ? _progressionSettings.WeekendDays : 2;
        var calendar = new RunCalendar(productionDays, weekendDays);

        // 2. Inicializa RunManager com todas as dependências
        App.RunManager.Initialize(
            Registry.Save,
            Registry.GridConfig,
            Registry.Events.Time,
            Registry.State,
            calendar,
            _progressionSettings,
            _cardDropLibrary,
            _gameSettings
        );

        // 3. Cria Serviços Puros
        var economy = new EconomyService(Registry.Run, Registry.Save);
        var health = new HealthService(Registry.Save);
        
        // FIX: Usa RunDeckStrategy ao invés de SeasonalCardStrategy
        var cardStrategy = new RunDeckStrategy(Registry.GameLibrary);
        var dailyHand = new DailyHandSystem(Registry.GameLibrary, economy, cardStrategy, Registry.Events.Player);
        
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

// ===== CARD SOURCE STRATEGIES =====

/// <summary>
/// Estratégia que saca cartas do Run Deck deterministicamente.
/// </summary>
public class RunDeckStrategy : ICardSourceStrategy
{
    private readonly IGameLibrary _library;

    public RunDeckStrategy(IGameLibrary library)
    {
        _library = library;
    }

    public System.Collections.Generic.List<CardID> GetNextCardIDs(int amount, RunData currentRun)
    {
        var result = new System.Collections.Generic.List<CardID>();
        
        if (currentRun == null || currentRun.RunDeckCardIDs == null || currentRun.RunDeckCardIDs.Count == 0)
        {
            Debug.LogWarning("[RunDeckStrategy] RunDeck vazio ou não inicializado!");
            return result;
        }

        int cardsRemaining = currentRun.RunDeckCardIDs.Count - currentRun.RunDeckDrawIndex;
        int cardsToDraw = UnityEngine.Mathf.Min(amount, cardsRemaining);

        for (int i = 0; i < cardsToDraw; i++)
        {
            string cardIDString = currentRun.RunDeckCardIDs[currentRun.RunDeckDrawIndex];
            CardID id = (CardID)cardIDString;
            
            if (id.IsValid)
            {
                result.Add(id);
                currentRun.RunDeckDrawIndex++;
            }
            else
            {
                Debug.LogError($"[RunDeckStrategy] CardID inválido no deck: '{cardIDString}'");
            }
        }

        if (result.Count < amount)
        {
            Debug.LogWarning($"[RunDeckStrategy] Deck esgotado! Solicitado {amount}, disponível {result.Count}");
        }

        return result;
    }
}
