using System.Collections.Generic;

public class CheatContext
{
    private static CheatContext _instance;
    public static CheatContext Instance => _instance ??= new CheatContext();

    public RunManager RunManager => AppCore.Instance?.RunManager;
    public SaveManager SaveManager => AppCore.Instance?.SaveManager;
    public DailyResolutionSystem ResolutionSystem => AppCore.Instance?.DailyResolutionSystem;
    public IGridService GridService => AppCore.Instance?.GetGridLogic();
    public DailyHandSystem HandSystem => AppCore.Instance?.DailyHandSystem;
    public IGameLibrary Library => AppCore.Instance?.GameLibrary;
    public IEconomyService EconomyService => AppCore.Instance?.EconomyService;
    public GameEvents Events => AppCore.Instance?.Events;
    public PatternTrackingService PatternTracking => AppCore.Instance?.PatternTracking;
    public PatternDetector PatternDetector => AppCore.Instance?.PatternDetector;
    public PatternScoreCalculator PatternCalculator => AppCore.Instance?.PatternCalculator;
}
