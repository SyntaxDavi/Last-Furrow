using System;
using System.Collections.Generic;
using UnityEngine;
using LastFurrow.Traditions;

/// <summary>
/// Registro centralizado de todos os serviços globais do jogo.
/// Facilita o desacoplamento do AppCore e permite injeção de dependência mais limpa.
/// </summary>
public class ServiceRegistry
{
    // Módulo: Core (Configurações e Infra)
    public ISaveManager Save { get; private set; }
    public GridConfiguration GridConfig { get; private set; }
    public GameEvents Events { get; private set; }
    public IGameLibrary GameLibrary { get; private set; }
    
    // Módulo: Domínio (Lógica de Jogo)
    public IRunManager Run { get; private set; }
    public IEconomyService Economy { get; private set; }
    public DailyHandSystem DailyHand { get; private set; }
    public WeeklyGoalSystem WeeklyGoal { get; private set; }
    
    // Módulo: Sistemas (MonoBehaviours)
    public IGameStateProvider State { get; private set; }
    public TimeManager Time { get; private set; }
    public ITraditionService Traditions { get; private set; }
    public InputManager Input { get; private set; }
    
    // Módulo: Gameplay
    public ShopService Shop { get; private set; }
    public PatternDetector PatternDetector { get; private set; }
    public PatternScoreCalculator PatternCalculator { get; private set; }
    public PatternTrackingService PatternTracking { get; private set; }
    
    public ServiceRegistry()
    {
    }

    public void RegisterCore(ISaveManager save, GridConfiguration gridConfig, GameEvents events, IGameLibrary library)
    {
        Save = save;
        GridConfig = gridConfig;
        Events = events;
        GameLibrary = library;
    }

    public void RegisterDomain(IRunManager run, IEconomyService economy, DailyHandSystem dailyHand, WeeklyGoalSystem weeklyGoal)
    {
        Run = run;
        Economy = economy;
        DailyHand = dailyHand;
        WeeklyGoal = weeklyGoal;
    }

    public void RegisterSystems(IGameStateProvider state, TimeManager time, InputManager input)
    {
        State = state;
        Time = time;
        Input = input;
    }

    public void RegisterGameplay(ShopService shop, PatternDetector detector, PatternScoreCalculator calculator)
    {
        Shop = shop;
        PatternDetector = detector;
        PatternCalculator = calculator;
    }

    public void SetPatternTracking(PatternTrackingService tracking)
    {
        PatternTracking = tracking;
    }

    public void SetTraditions(ITraditionService traditions)
    {
        Traditions = traditions;
    }
}
