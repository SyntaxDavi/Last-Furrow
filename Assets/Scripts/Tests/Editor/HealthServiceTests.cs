using NUnit.Framework;
using UnityEngine;
using System;

[TestFixture]
public class HealthServiceTests
{
    private MockSaveManager _mockSave;
    private HealthService _healthService;

    [SetUp]
    public void SetUp()
    {
        _mockSave = new MockSaveManager();
        _mockSave.Data.CurrentRun = new RunData
        {
            CurrentLives = 3,
            MaxLives = 5
        };

        _healthService = new HealthService(_mockSave);
    }

    [Test]
    public void Heal_IncreasesCurrentLives_UpToMax()
    {
        _healthService.Heal(1);
        Assert.AreEqual(4, _mockSave.Data.CurrentRun.CurrentLives);

        _healthService.Heal(10); // Overheal
        Assert.AreEqual(5, _mockSave.Data.CurrentRun.CurrentLives);
    }

    [Test]
    public void TakeDamage_DecreasesCurrentLives_DownToZero()
    {
        _healthService.TakeDamage(1);
        Assert.AreEqual(2, _mockSave.Data.CurrentRun.CurrentLives);

        _healthService.TakeDamage(10); // Overdamage
        Assert.AreEqual(0, _mockSave.Data.CurrentRun.CurrentLives);
    }

    [Test]
    public void Heal_TriggersOnHealthChanged()
    {
        int eventCurrent = -1;
        int eventMax = -1;
        _healthService.OnHealthChanged += (c, m) => {
            eventCurrent = c;
            eventMax = m;
        };

        _healthService.Heal(1);

        Assert.AreEqual(4, eventCurrent);
        Assert.AreEqual(5, eventMax);
    }

    [Test]
    public void IsAtFullHealth_ReturnsTrue_WhenLivesAreMax()
    {
        Assert.IsFalse(_healthService.IsAtFullHealth);
        _healthService.Heal(2);
        Assert.IsTrue(_healthService.IsAtFullHealth);
    }

    // Manual Mock Implementation
    private class MockSaveManager : ISaveManager
    {
        public GameData Data { get; } = new GameData();
        public void SaveGame() { }
        public void LoadGame() { }
        public void DeleteSave() { }
    }
}
