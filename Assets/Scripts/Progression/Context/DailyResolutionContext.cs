// DailyResolutionContext.cs
// Um pacote com tudo que o Pipeline precisa para rodar a LÓGICA
public class DailyResolutionContext
{
    public RunManager RunManager { get; }
    public SaveManager SaveManager { get; }
    public InputManager InputManager { get; }
    public GameEvents Events { get; }
    public DailyHandSystem HandSystem { get; }
    public WeeklyGoalSystem GoalSystem { get; }
    public IGridService GridService { get; }
    public PatternDetector PatternDetector { get; }
    public PatternTrackingService PatternTracking { get; }
    public PatternScoreCalculator PatternCalculator { get; }

    public DailyResolutionContext(
        RunManager runManager, SaveManager saveManager, InputManager inputManager,
        GameEvents events, DailyHandSystem handSystem, WeeklyGoalSystem goalSystem,
        IGridService gridService, PatternDetector detector,
        PatternTrackingService tracking, PatternScoreCalculator calculator)
    {
        RunManager = runManager;
        SaveManager = saveManager;
        InputManager = inputManager;
        Events = events;
        HandSystem = handSystem;
        GoalSystem = goalSystem;
        GridService = gridService;
        PatternDetector = detector;
        PatternTracking = tracking;
        PatternCalculator = calculator;
    }
}