/// <summary>
/// Contexto completo de dependências para UI.
/// 
/// RESPONSABILIDADE:
/// - Centralizar acesso a eventos
/// - Fornecer dados read-only
/// - Fornecer policies de regras
/// 
/// ARQUITETURA:
/// - Criado por UIBootstrapper
/// - Injetado em todos componentes UI
/// - Imutável após criação
/// 
/// BENEFÍCIOS:
/// - UI não depende de AppCore.Instance
/// - Testável (pode mockar contexto)
/// - Single point of configuration
/// </summary>
public class UIContext
{
    // === EVENTOS ===
    public readonly ProgressionEvents ProgressionEvents;
    public readonly TimeEvents TimeEvents;
    public readonly GameStateEvents GameStateEvents;
    // public readonly EconomyEvents EconomyEvents; // TODO: Criar quando necessário
    public readonly GridEvents GridEvents;
    public readonly PlayerEvents PlayerEvents;

    // === DADOS (Read-Only) ===
    public readonly IRunDataProvider RunData;

    // === POLICIES (Regras) ===
    public readonly ITimePolicy TimePolicy;

    public UIContext(
        ProgressionEvents progressionEvents,
        TimeEvents timeEvents,
        GameStateEvents gameStateEvents,
        // EconomyEvents economyEvents, // TODO
        GridEvents gridEvents,
        PlayerEvents playerEvents,
        IRunDataProvider runData,
        ITimePolicy timePolicy)
    {
        ProgressionEvents = progressionEvents ?? throw new System.ArgumentNullException(nameof(progressionEvents));
        TimeEvents = timeEvents ?? throw new System.ArgumentNullException(nameof(timeEvents));
        GameStateEvents = gameStateEvents ?? throw new System.ArgumentNullException(nameof(gameStateEvents));
        // EconomyEvents = economyEvents ?? throw new System.ArgumentNullException(nameof(economyEvents));
        GridEvents = gridEvents ?? throw new System.ArgumentNullException(nameof(gridEvents));
        PlayerEvents = playerEvents ?? throw new System.ArgumentNullException(nameof(playerEvents));
        RunData = runData ?? throw new System.ArgumentNullException(nameof(runData));
        TimePolicy = timePolicy ?? throw new System.ArgumentNullException(nameof(timePolicy));
    }
}
