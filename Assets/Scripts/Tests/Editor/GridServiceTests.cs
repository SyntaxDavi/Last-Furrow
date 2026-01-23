using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Testes unitários para o GridService.
/// Demonstram como o uso de IGameStateProvider permite testar regras de negócio
/// sem precisar de um GameStateManager real rodando.
/// </summary>
[TestFixture]
public class GridServiceTests
{
    private GridService _gridService;
    private MockGameStateProvider _mockState;
    private RunData _runData;

    [SetUp]
    public void SetUp()
    {
        _mockState = new MockGameStateProvider();
        _runData = new RunData();
        
        // Setup de um grid pequeno 3x3 para teste
        _runData.GridSlots = new CropState[9];
        for (int i = 0; i < 9; i++) _runData.GridSlots[i] = new CropState();
        _runData.SlotStates = new GridSlotState[9];
        for (int i = 0; i < 9; i++) _runData.SlotStates[i] = new GridSlotState(true); // Tudo liberado

        // Inicializa com dependências mínimas (Mocks/Nulls onde não for testado)
        _gridService = new GridService(
            _runData,
            null, // Library não necessária para este teste
            _mockState,
            null  // Config não necessária para este teste
        );
    }

    [Test]
    public void ApplyCard_ReturnsFail_WhenNotPlayingState()
    {
        // Setup: Estado do jogo é Shopping, não Playing
        _mockState.CurrentState = GameState.Shopping;
        var card = new CardData(); // Dummy card

        // Act
        var result = _gridService.ApplyCard(0, card);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.That(result.Message, Does.Contain("Ação bloqueada"));
    }

    [Test]
    public void ApplyCard_ReturnsFail_WhenIndexIsInvalid()
    {
        // Setup: Estado correto mas índice fora do grid
        _mockState.CurrentState = GameState.Playing;
        var card = new CardData();

        // Act
        var result = _gridService.ApplyCard(99, card); // Fora do range 0-8

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.That(result.Message, Does.Contain("Slot inválido"));
    }

    // --- MOCKS AUXILIARES ---

    private class MockGameStateProvider : IGameStateProvider
    {
        public GameState CurrentState { get; set; }
        public void SetState(GameState newState) => CurrentState = newState;
        // Interface pode exigir estes eventos, implementamos vazio
        public event System.Action<GameState> OnStateChanged { add { } remove { } }
        public event System.Action<GameState, GameState> OnStateTransition { add { } remove { } }
    }
}
