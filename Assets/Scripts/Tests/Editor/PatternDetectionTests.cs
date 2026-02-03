using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;

[TestFixture]
public class PatternDetectionTests
{
    private RunData _runData;
    private HorizontalLineDetector _detector;
    private PatternDefinitionSO _mockDefinition;

    [SetUp]
    public void SetUp()
    {
        _runData = new RunData();
        // Setup 5x5 grid (Standard for Horizontal Line detection)
        _runData.GridSlots = new CropState[25];
        for (int i = 0; i < 25; i++) _runData.GridSlots[i] = new CropState(CropID.Empty);
        
        _mockDefinition = ScriptableObject.CreateInstance<PatternDefinitionSO>();
        _mockDefinition.PatternID = "FULL_LINE";
        _mockDefinition.DisplayName = "Full Line";
        _mockDefinition.BaseScore = 100;

        _detector = new HorizontalLineDetector(_mockDefinition);
    }

    [TearDown]
    public void TearDown()
    {
        if (_mockDefinition != null) UnityEngine.Object.DestroyImmediate(_mockDefinition);
    }

    [Test]
    public void DetectAt_ReturnsMatch_WhenHorizontalLineExists()
    {
        // Setup: Uma linha completa no topo (0, 1, 2, 3, 4)
        for (int i = 0; i < 5; i++)
        {
            _runData.GridSlots[i] = new CropState(new CropID("tomato")); // Qualquer crop vivo
        }

        // Mock do GridService
        var mockGrid = new MockGridService(_runData);

        // Act
        var match = _detector.DetectAt(mockGrid, 0, new int[] { 0, 1, 2, 3, 4 });

        // Assert
        Assert.IsNotNull(match, "Deveria detectar a linha horizontal");
        Assert.AreEqual(100, match.BaseScore);
        Assert.AreEqual(5, match.SlotIndices.Count);
    }

    [Test]
    public void DetectAt_ReturnsNull_WhenLineIsBroken()
    {
        // Setup: Linha incompleta (falta o slot 2)
        _runData.GridSlots[0] = new CropState(new CropID("potato"));
        _runData.GridSlots[1] = new CropState(new CropID("potato"));
        _runData.GridSlots[2] = new CropState(CropID.Empty);
        _runData.GridSlots[3] = new CropState(new CropID("potato"));
        _runData.GridSlots[4] = new CropState(new CropID("potato"));

        var mockGrid = new MockGridService(_runData);

        // Act
        var match = _detector.DetectAt(mockGrid, 0, new int[] { 0, 1, 2, 3, 4 });

        // Assert
        Assert.IsNull(match, "Não deveria detectar linha incompleta");
    }

    // --- MOCK SIMPLES ---
    private class MockGridService : IGridService
    {
        private RunData _data;
        public MockGridService(RunData data) => _data = data;

        // Events required by IGridService
        public event Action<GridChangeEvent> OnGridChanged;

        // Properties
        public int SlotCount => _data.GridSlots.Length;
        public GridConfiguration Config => null;

        // Methods
        public CropState GetSlot(int index) => _data.GridSlots[index];
        public IReadOnlyCropState GetSlotReadOnly(int index) => _data.GridSlots[index];
        public GridSlotState GetSlotStateReadOnly(int index) => null;

        public bool IsSlotUnlocked(int index) => true;
        public bool CanUnlockSlot(int index) => false;
        public bool TryUnlockSlot(int index) => false;

        public bool ProcessNightCycleForSlot(int slotIndex, out GridChangeEvent result, bool silent = false)
        {
            result = default;
            return false;
        }

        public void ForceVisualRefresh(int index) { }

        public bool CanReceiveCard(int index, CardData card) => true;
        public float GetGridContaminationPercentage() => 0f;

        public InteractionResult ApplyCard(int index, CardData card) 
            => InteractionResult.Ok();

        // Helper methods used by detector (if any)
        public int GetColumns() => 5;
        public int GetRows() => 5;
    }
}
