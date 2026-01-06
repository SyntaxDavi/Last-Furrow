public struct PurchaseContext
{
    public RunData RunData;
    public ProgressionEvents ProgressionEvents; // Para itens que afetam vida/meta
    public PlayerEvents PlayerEvents;           // Para itens que afetam cartas

    public PurchaseContext(RunData run, GameEvents events)
    {
        RunData = run;
        ProgressionEvents = events.Progression;
        PlayerEvents = events.Player;
    }
}