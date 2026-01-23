using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Testes unitários para o EconomyService.
/// Validam a lógica de transações financeiras sem dependência do Unity Engine real.
/// </summary>
[TestFixture]
public class EconomyServiceTests
{
    private EconomyService _economyService;
    private MockRunManager _mockRun;
    private MockSaveManager _mockSave;

    [SetUp]
    public void SetUp()
    {
        _mockRun = new MockRunManager();
        _mockSave = new MockSaveManager();
        
        // Criar dados iniciais para o teste
        var runData = new RunData(); 
        runData.Money = 100;
        runData.TotalMoneyEarned = 100;
        _mockSave.Data.CurrentRun = runData;

        _economyService = new EconomyService(_mockRun, _mockSave);
    }

    [Test]
    public void CurrentMoney_ReturnsValueFromSaveManager()
    {
        // Assert
        Assert.AreEqual(100, _economyService.CurrentMoney);
    }

    [Test]
    public void Earn_IncreasesMoneyAndSaves()
    {
        // Act
        _economyService.Earn(50, TransactionType.SeedSale);

        // Assert
        Assert.AreEqual(150, _economyService.CurrentMoney);
        Assert.AreEqual(150, _mockSave.Data.CurrentRun.TotalMoneyEarned);
        Assert.IsTrue(_mockSave.SaveCalled, "SaveGame deveria ter sido chamado.");
    }

    [Test]
    public void TrySpend_SucceedsWhenEnoughMoney()
    {
        // Act
        bool success = _economyService.TrySpend(40, TransactionType.ShopPurchase);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(60, _economyService.CurrentMoney);
        Assert.IsTrue(_mockSave.SaveCalled);
    }

    [Test]
    public void TrySpend_FailsWhenInsufficientFunds()
    {
        // Act
        bool success = _economyService.TrySpend(200, TransactionType.ShopPurchase);

        // Assert
        Assert.IsFalse(success);
        Assert.AreEqual(100, _economyService.CurrentMoney, "O dinheiro não deveria ter mudado.");
        Assert.IsFalse(_mockSave.SaveCalled, "SaveGame não deveria ter sido chamado em falha.");
    }

    [Test]
    public void Earn_TriggersBalanceChangedEvent()
    {
        // Setup
        int eventNewBalance = 0;
        int eventDelta = 0;
        TransactionType eventType = TransactionType.None;
        
        _economyService.OnBalanceChanged += (balance, delta, type) => {
            eventNewBalance = balance;
            eventDelta = delta;
            eventType = type;
        };

        // Act
        _economyService.Earn(25, TransactionType.SeedSale);

        // Assert
        Assert.AreEqual(125, eventNewBalance);
        Assert.AreEqual(25, eventDelta);
        Assert.AreEqual(TransactionType.SeedSale, eventType);
    }

    // --- MOCKS AUXILIARES ---

    private class MockRunManager : IRunManager
    {
        public RunPhase CurrentPhase => RunPhase.Production;
        public bool IsRunActive => true;
        public void StartNewRun() { }
        public void AdvanceDay() { }
        public void StartNextWeek(RunData run) { }
    }

    private class MockSaveManager : ISaveManager
    {
        public GameData Data { get; } = new GameData();
        public bool SaveCalled { get; private set; }

        public void SaveGame() => SaveCalled = true;
        public void LoadGame() { }
        public void DeleteSave() { }
    }
}
