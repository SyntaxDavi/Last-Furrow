using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Configuração Visual")]
    [SerializeField] private GridSlotView _slotPrefab;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 1.1f);

    private IGridService _gridService;
    private IGameLibrary _library;
    private List<GridSlotView> _spawnedSlots = new List<GridSlotView>();

    public void Configure(IGridService service, IGameLibrary library)
    {
        if (_gridService != null)
            _gridService.OnSlotStateChanged -= RefreshSingleSlot;

        _gridService = service;
        _library = library;

        _gridService.OnSlotStateChanged += RefreshSingleSlot;

        GenerateGrid();
        RefreshAllSlots();
    }
    private void Start()
    {
        if (_gridService == null)
            Debug.LogWarning("[GridManager] Aguardando configuração via Bootstrapper...");
    }

    private void OnDestroy()
    {
        if (_gridService != null)
            _gridService.OnSlotStateChanged -= RefreshSingleSlot;
    }

    public Vector3 GetSlotPosition(int index)
    {
        if (index >= 0 && index < _spawnedSlots.Count)
            return _spawnedSlots[index].transform.position;
        return Vector3.zero;
    }

    private void GenerateGrid()
    {
        foreach (var slot in _spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        _spawnedSlots.Clear();

        foreach (Transform child in _gridContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;

            var newSlot = Instantiate(_slotPrefab, _gridContainer);

            float xPos = (col - 1) * _spacing.x;
            float yPos = (1 - row) * _spacing.y;
            newSlot.transform.localPosition = new Vector2(xPos, yPos);
            newSlot.name = $"Slot_{i}";

            newSlot.Initialize(i);
            newSlot.OnCheckDropInteraction += (index, card) => _gridService.CanReceiveCard(index, card);
            newSlot.OnDropInteraction += HandleDropInteraction;

            _spawnedSlots.Add(newSlot);
        }
    }

    private void HandleDropInteraction(int index, CardData card)
    {
        InteractionResult result = _gridService.ApplyCard(index, card);

        if (result.Success)
        {
            AppCore.Instance.Events.Player.TriggerCardConsumed(card.ID);
        }
        else
        {
            Debug.Log($"[GridManager] Ação falhou: {result.Message}");
        }
    }

    // --- ATUALIZAÇÃO VISUAL (Tradução Model -> View) ---

    public void RefreshAllSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++) RefreshSingleSlot(i);
    }

    private void RefreshSingleSlot(int index)
    {
        if (index < 0 || index >= _spawnedSlots.Count) return;

        IReadOnlyCropState state = _gridService.GetSlotReadOnly(index);
        var view = _spawnedSlots[index];

        Sprite spriteToRender = null;
        bool isWatered = false;

        if (state != null)
        {
            isWatered = state.IsWatered; 

            if (state.CropID.IsValid) 
            {
                if (_library != null)
                {
                    if (_library.TryGetCrop(state.CropID, out var data))
                    {
                        spriteToRender = data.GetSpriteForStage(
                            state.CurrentGrowth,
                            state.DaysMature, 
                            state.IsWithered
                        );
                    }
                }
            }
        }
        view.SetVisualState(spriteToRender, isWatered);
    }
}