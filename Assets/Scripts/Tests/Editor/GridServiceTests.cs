using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Collections.Generic;
using LastFurrow.Traditions;

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
    private GridConfiguration _mockConfig;

    [SetUp]
    public void SetUp()
    {
        _mockState = new MockGameStateProvider();
        _runData = new RunData();
        
        // Cria configuração mock para o grid (ScriptableObject)
        _mockConfig = ScriptableObject.CreateInstance<GridConfiguration>();
        _mockConfig.Columns = 3;
        _mockConfig.Rows = 3;
        
        // Setup de um grid pequeno 3x3 para teste
        _runData.GridSlots = new CropState[9];
        for (int i = 0; i < 9; i++) _runData.GridSlots[i] = new CropState(CropID.Empty);
        _runData.SlotStates = new GridSlotState[9];
        for (int i = 0; i < 9; i++) _runData.SlotStates[i] = new GridSlotState(true); // Tudo liberado

        // Inicializa com dependências
        _gridService = new GridService(
            _runData,
            null, // Library não necessária para este teste
            _mockState,
            _mockConfig
        );

        // Inicializa o InteractionFactory para evitar o erro de Factory não inicializado
        if (!InteractionFactory.IsInitialized)
        {
            var mockRun = new MockRunManager();
            var mockSave = new MockSaveManager();
            var mockEco = new MockEconomy();
            var mockLib = new MockLibrary();
            var mockRand = new SeededRandomProvider(123);
            var pEvents = new PlayerEvents();
            var gEvents = new GameEvents();
            
            var identity = new RunIdentityContext(mockRun, mockSave, mockEco, mockLib, mockRand, pEvents, gEvents);
            var runtime = new RunRuntimeContext(_gridService);
            
            InteractionFactory.Initialize(identity, runtime);
        }
    }
    
    [TearDown]
    public void TearDown()
    {
        // Limpa ScriptableObject criado
        if (_mockConfig != null)
            UnityEngine.Object.DestroyImmediate(_mockConfig);

        // Limpa o Factory para não poluir outros testes
        InteractionFactory.Cleanup();
    }

    [Test]
    public void ApplyCard_ReturnsFail_WhenNotPlayingState()
    {
        // Setup: Estado do jogo é Shopping, não Playing
        _mockState.CurrentState = GameState.Shopping;
        _mockState.PreviousState = GameState.Initialization;
        var card = ScriptableObject.CreateInstance<CardData>(); // Dummy card

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
        _mockState.SetState(GameState.Playing);
        var card = ScriptableObject.CreateInstance<CardData>();

        // Act
        var result = _gridService.ApplyCard(99, card); // Fora do range 0-8

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.That(result.Message, Does.Contain("Slot inválido"));
    }

    [Test]
    public void ApplyCard_UpdatesRunData_WhenSuccess()
    {
        // Setup
        _mockState.SetState(GameState.Playing);
        var card = ScriptableObject.CreateInstance<CardData>();
        card.ID = new CardID("TEST_CROP");
        card.Type = CardType.Plant;
        card.CropToPlant = ScriptableObject.CreateInstance<CropData>();
        card.CropToPlant.ID = new CropID("trio");

        // Act
        var result = _gridService.ApplyCard(0, card);

        // Assert
        Assert.IsTrue(result.IsSuccess, $"Erro ao aplicar carta: {result.Message}");
        Assert.AreEqual(new CropID("trio"), _runData.GridSlots[0].CropID);
    }

    // --- MOCKS AUXILIARES ---
    
    private class MockRunManager : IRunManager {
        public void StartNewRun() {}
        public void AdvanceDay() {}
        public bool IsRunActive => true;
        public void StartNextWeek(RunData run) {}
        public void EndRun(RunEndReason reason) {}
        public RunPhase CurrentPhase => RunPhase.Production;
        
        public event System.Action<RunData> OnWeekendStarted;
        public event System.Action<RunData> OnProductionStarted;
        
        // Suprime warning de eventos não usados
        private void SuppressWarnings() { OnWeekendStarted?.Invoke(null); OnProductionStarted?.Invoke(null); }
    }

    private class MockSaveManager : ISaveManager {
        public void SaveGame() {}
        public void LoadGame() {}
        public GameData Data => null;
        public void DeleteSave() {}
    }

    private class MockEconomy : IEconomyService {
        public int CurrentMoney => 0;
        public void Earn(int amount, TransactionType source) {}
        public bool TrySpend(int amount, TransactionType reason) => true;
#pragma warning disable 67
        public event Action<int, int, TransactionType> OnBalanceChanged;
#pragma warning restore 67
    }

    private class MockLibrary : IGameLibrary {
        public bool TryGetCrop(CropID id, out CropData data) { 
            data = ScriptableObject.CreateInstance<CropData>();
            data.ID = id;
            data.Name = "Mock " + id.Value;
            data.DaysToMature = 3;
            return true; 
        }
        public bool TryGetCard(CardID id, out CardData data) { data = null; return false; }
        public bool TryGetTradition(TraditionID id, out TraditionData data) { data = null; return false; }
        public List<CardData> GetRandomCards(int count, IRandomProvider random = null) => new List<CardData>();
        public List<TraditionData> GetRandomTraditions(int count, IRandomProvider random = null) => new List<TraditionData>();
        public IEnumerable<CropData> GetAllCrops() => new List<CropData>();
        public IEnumerable<CardData> GetAllCards() => new List<CardData>();
        public IEnumerable<TraditionData> GetAllTraditions() => new List<TraditionData>();
    }

    private class MockGameStateProvider : IGameStateProvider
    {
        public GameState CurrentState { get; set; } = GameState.Playing;
        public GameState PreviousState { get; set; } = GameState.Initialization;

        public event Action<GameState> OnStateChanged;

        public bool IsGameplayActive() => CurrentState == GameState.Playing;

        public void SetState(GameState newState)
        {
            PreviousState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
