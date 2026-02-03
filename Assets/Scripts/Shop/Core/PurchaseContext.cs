public struct PurchaseContext
{
    public RunData RunData;
    public IHealthService Health;
    public ProgressionEvents ProgressionEvents;
    public PlayerEvents PlayerEvents;

    public PurchaseContext(RunData run, IHealthService health, GameEvents events)
    {
        RunData = run;
        Health = health;
        ProgressionEvents = events.Progression;
        PlayerEvents = events.Player;
    }
}