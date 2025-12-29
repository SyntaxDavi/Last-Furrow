using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private GridSlotView _slotPrefab;
    [SerializeField] private Transform _gridOrigin;
    [SerializeField] private Vector2 _spacing = new Vector2(1.5f, 1.1f);

    private List<GridSlotView> _spawnedSlots = new List<GridSlotView>();

    private void Start()
    {
        GenerateGrid();
        RefreshAllSlots(0);

        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.OnDayChanged += RefreshAllSlots;
            AppCore.Instance.Events.OnRunStarted += HandleRunStarted;
        }
    }
    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.OnAnalyzeCropSlot += HighlightSlot;
        }
    }
    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.OnDayChanged -= RefreshAllSlots;
            AppCore.Instance.Events.OnRunStarted -= HandleRunStarted;
            AppCore.Instance.Events.OnAnalyzeCropSlot -= HighlightSlot;
        }
    }

    private void HighlightSlot(int index)
    {
        // Validação de índice (segurança visual)
        if (index < 0 || index >= _spawnedSlots.Count) return;

        var slotView = _spawnedSlots[index];
        if (slotView != null)
        {
            // Aqui você chamaria um método no GridSlotView
            // Ex: slotView.PlayAnalyzeAnimation(); 
            // Por enquanto, apenas um debug visual ou log
            Debug.Log($"[Grid Visual] Destacando slot {index}");
        }
    }

    private void HandleRunStarted()
    {
        // Reseta visualmente para o dia 1
        RefreshAllSlots(1);
    }

    private void GenerateGrid()
    {
        if (_slotPrefab == null || _gridOrigin == null)
        {
            Debug.LogError("GRID MANAGER: Referências faltantes.");
            return;
        }

        // Limpeza segura (Iterando de trás pra frente)
        for (int i = _gridOrigin.childCount - 1; i >= 0; i--)
        {
            if (_gridOrigin.GetChild(i) != null)
                Destroy(_gridOrigin.GetChild(i).gameObject);
        }
        _spawnedSlots.Clear();

        for (int i = 0; i < 9; i++)
        {
            // Criação do Grid (igual ao anterior...)
            int row = i / 3;
            int col = i % 3;

            var newSlot = Instantiate(_slotPrefab, _gridOrigin);

            float xPos = (col - 1) * _spacing.x;
            float yPos = (1 - row) * _spacing.y;

            newSlot.transform.localPosition = new Vector2(xPos, yPos);
            newSlot.name = $"Slot_{i} [{col},{row}]";
            newSlot.Initialize(i);
            _spawnedSlots.Add(newSlot);
        }
    }

    private void RefreshAllSlots(int currentDay)
    {
        if (AppCore.Instance == null || AppCore.Instance.SaveManager == null) return;

        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        if (runData == null) return;

        if (runData.GridSlots == null || runData.GridSlots.Length != 9)
        {
            runData.GridSlots = new CropState[9];
        }

        int safeLimit = Mathf.Min(_spawnedSlots.Count, runData.GridSlots.Length);

        for (int i = 0; i < safeLimit; i++)
        {
            CropState state = runData.GridSlots[i];

            if (_spawnedSlots[i] != null)
            {
                Sprite spriteToRender = null;

                if (state != null && !string.IsNullOrEmpty(state.CropID))
                {
                    // Busca na Library aqui, não na View
                    if (GameLibrary.Instance != null)
                    {
                        CropData data = GameLibrary.Instance.GetCrop(state.CropID);
                        if (data != null)
                        {
                            spriteToRender = data.GetSpriteForStage(state.CurrentGrowth);
                        }
                    }
                }

                // Passa apenas o Sprite final para a View
                _spawnedSlots[i].SetPlantVisual(spriteToRender);
            }
        }
    }
}