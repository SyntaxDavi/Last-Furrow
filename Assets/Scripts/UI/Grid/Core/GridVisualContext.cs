using System;

/// <summary>
/// Contexto de dependências para sistema visual do Grid.
/// Análogo ao UIContext, fornece acesso read-only a serviços e eventos.
/// 
/// RESPONSABILIDADE:
/// - Agregar todas as dependências que GridManager e GridSlotView precisam
/// - Imutável após criação (readonly fields)
/// - Facilitar injeção de dependências e testes
/// 
/// SOLID:
/// - Dependency Inversion: Views dependem do contexto, não de AppCore.Instance
/// - Interface Segregation: Apenas o necessário é exposto
/// - Single Responsibility: Apenas agregar dependências
/// 
/// BENEFÍCIOS:
/// - Testável: Mock completo em testes unitários
/// - Desacoplado: Grid não conhece AppCore
/// - Centralizado: Um único ponto de configuração
/// 
/// USO:
/// GridVisualBootstrapper cria o contexto e injeta em GridManager.
/// GridManager passa para cada GridSlotView.
/// </summary>
public class GridVisualContext
{
    // Serviços de domínio
    public readonly IGridService GridService;
    public readonly IGameLibrary Library;
    
    // Validação de drop
    public readonly IDropValidator DropValidator;
    
    // Eventos
    public readonly GridEvents GridEvents;
    public readonly GameStateEvents GameStateEvents;
    
    // State providers (read-only access)
    public readonly GameStateManager GameStateManager;
    
    // Configuração visual
    public readonly GridVisualConfig VisualConfig;

    /// <summary>
    /// Construtor valida que todas as dependências são não-nulas.
    /// Falha rápido se algo estiver faltando (fail-fast).
    /// </summary>
    public GridVisualContext(
        IGridService gridService,
        IGameLibrary library,
        IDropValidator dropValidator,
        GridEvents gridEvents,
        GameStateEvents gameStateEvents,
        GameStateManager gameStateManager,
        GridVisualConfig visualConfig)
    {
        GridService = gridService ?? throw new ArgumentNullException(nameof(gridService));
        Library = library ?? throw new ArgumentNullException(nameof(library));
        DropValidator = dropValidator ?? throw new ArgumentNullException(nameof(dropValidator));
        GridEvents = gridEvents ?? throw new ArgumentNullException(nameof(gridEvents));
        GameStateEvents = gameStateEvents ?? throw new ArgumentNullException(nameof(gameStateEvents));
        GameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
        VisualConfig = visualConfig ?? throw new ArgumentNullException(nameof(visualConfig));
    }
}
