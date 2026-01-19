using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Cheat Manager refatorado para desenvolvimento.
/// 
/// CONTROLE:
/// - Toggle: F1 (mostrar/esconder)
/// 
/// FUNCIONALIDADES:
/// - Add Money (customizável)
/// - Deletar Save (recomeça imediatamente)
/// - Add Vida
/// - Desbloquear Todo Grid
/// - Spawn Card Específica
/// - Limpar Grid
/// 
/// ARQUITETURA:
/// - Apenas em Development builds
/// - Posicionamento: Lateral esquerda, meio da tela
/// - Eventos para feedback visual
/// </summary>
public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [Header("Toggle Settings")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    
    [Header("UI Position")]
    [SerializeField] private float _uiX = 20f;
    [SerializeField] private float _uiY = 200f;
    [SerializeField] private float _uiWidth = 280f;
    [SerializeField] private float _uiHeight = 600f;

    [Header("Money Settings")]
    [SerializeField] private int _defaultMoneyAmount = 100;

    [Header("Card Spawn")]
    [SerializeField] private List<string> _quickSpawnCardIDs = new List<string>
    {
        "card_corn",
        "card_water",
        "card_fertilizer"
    };

    private bool _showUI = false;
    private string _customMoneyInput = "100";
    private Vector2 _scrollPosition = Vector2.zero;

    // Lazy References
    private RunManager _runManager => AppCore.Instance?.RunManager;
    private SaveManager _saveManager => AppCore.Instance?.SaveManager;
    private DailyResolutionSystem _resolutionSystem => AppCore.Instance?.DailyResolutionSystem;
    private IGridService _gridService => AppCore.Instance?.GetGridLogic();
    private DailyHandSystem _handSystem => AppCore.Instance?.DailyHandSystem;

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _showUI = !_showUI;
        }
    }

    private void OnGUI()
    {
        if (!_showUI || AppCore.Instance == null) return;

        GUILayout.BeginArea(new Rect(_uiX, _uiY, _uiWidth, _uiHeight));
        GUILayout.Box("?? CHEAT MENU (F1)");

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        var run = _saveManager?.Data?.CurrentRun;
        if (run == null)
        {
            GUILayout.Label("? Nenhuma Run Ativa.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        DrawEconomySection(run);
        GUILayout.Space(10);
        DrawLivesSection(run);
        GUILayout.Space(10);
        DrawGridSection();
        GUILayout.Space(10);
        DrawCardsSection(run);
        GUILayout.Space(10);
        DrawSaveSection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawEconomySection(RunData run)
    {
        GUILayout.Label("?? ECONOMIA");
        GUILayout.Label($"Dinheiro: ${run.Money}");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Valor:", GUILayout.Width(50));
        _customMoneyInput = GUILayout.TextField(_customMoneyInput, GUILayout.Width(80));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("+ Adicionar Dinheiro"))
        {
            if (int.TryParse(_customMoneyInput, out int amount))
            {
                AppCore.Instance.EconomyService.Earn(amount, TransactionType.Debug);
                Debug.Log($"[Cheat] +${amount}");
            }
        }
    }

    private void DrawLivesSection(RunData run)
    {
        GUILayout.Label("? VIDAS");
        GUILayout.Label($"Vidas: {run.CurrentLives} / {run.MaxLives}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+1 Vida"))
        {
            if (run.CurrentLives < run.MaxLives)
            {
                run.CurrentLives++;
                AppCore.Instance.Events.Progression.TriggerLivesChanged(run.CurrentLives);
                Debug.Log($"[Cheat] Vida adicionada: {run.CurrentLives}");
            }
        }
        if (GUILayout.Button("-1 Vida"))
        {
            if (run.CurrentLives > 0)
            {
                run.CurrentLives--;
                AppCore.Instance.Events.Progression.TriggerLivesChanged(run.CurrentLives);
                Debug.Log($"[Cheat] Vida removida: {run.CurrentLives}");
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawGridSection()
    {
        GUILayout.Label("?? GRID");

        if (GUILayout.Button("Desbloquear Todo Grid"))
        {
            CheatUnlockAllSlots();
        }

        if (GUILayout.Button("Limpar Grid (Remove Plantas)"))
        {
            CheatClearGrid();
        }

        if (GUILayout.Button("Regar Todo Grid"))
        {
            CheatWaterAll();
        }

        if (GUILayout.Button("Amadurecer Tudo"))
        {
            CheatMatureAll();
        }
    }

    private void DrawCardsSection(RunData run)
    {
        GUILayout.Label("?? CARTAS");
        GUILayout.Label($"Mão: {run.Hand.Count}/{run.MaxHandSize}");

        if (GUILayout.Button("Comprar 3 Cartas"))
        {
            _handSystem?.ProcessDailyDraw(run);
            Debug.Log("[Cheat] Cartas compradas");
        }

        GUILayout.Label("Spawn Rápido:");
        foreach (var cardID in _quickSpawnCardIDs)
        {
            if (GUILayout.Button($"+ {cardID}"))
            {
                CheatSpawnCard(cardID);
            }
        }
    }

    private void DrawSaveSection()
    {
        GUILayout.Label("?? SAVE");
        
        GUI.color = Color.red;
        if (GUILayout.Button("? DELETAR SAVE & RESTART"))
        {
            ResetSaveData();
        }
        GUI.color = Color.white;

        if (GUILayout.Button("Salvar Agora"))
        {
            _saveManager?.SaveGame();
            Debug.Log("[Cheat] Save forçado");
        }
    }

    private void CheatUnlockAllSlots()
    {
        if (_gridService == null) return;

        var run = _saveManager.Data.CurrentRun;
        int unlockedCount = 0;

        for (int i = 0; i < run.SlotStates.Length; i++)
        {
            if (!run.SlotStates[i].IsUnlocked)
            {
                run.SlotStates[i].IsUnlocked = true;
                unlockedCount++;
            }
        }

        // Força refresh visual via SaveManager (dispara evento OnDataDirty)
        _saveManager?.SaveGame();
        
        // Recarrega cena para aplicar mudanças
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

        Debug.Log($"[Cheat] {unlockedCount} slots desbloqueados. Recarregando cena...");
    }

    private void CheatClearGrid()
    {
        if (_gridService == null) return;

        var run = _saveManager.Data.CurrentRun;
        int clearedCount = 0;

        for (int i = 0; i < run.GridSlots.Length; i++)
        {
            if (!run.GridSlots[i].IsEmpty)
            {
                run.GridSlots[i] = new CropState();
                clearedCount++;
            }
        }

        _saveManager?.SaveGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

        Debug.Log($"[Cheat] {clearedCount} slots limpos. Recarregando cena...");
    }

    private void CheatWaterAll()
    {
        if (_gridService == null) return;

        var run = _saveManager.Data.CurrentRun;
        int wateredCount = 0;

        for (int i = 0; i < run.GridSlots.Length; i++)
        {
            if (!run.GridSlots[i].IsEmpty && !run.GridSlots[i].IsWatered)
            {
                run.GridSlots[i].IsWatered = true;
                wateredCount++;
            }
        }

        _saveManager?.SaveGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

        Debug.Log($"[Cheat] {wateredCount} plantas regadas. Recarregando cena...");
    }

    private void CheatMatureAll()
    {
        if (_gridService == null) return;

        var run = _saveManager.Data.CurrentRun;
        int maturedCount = 0;

        for (int i = 0; i < run.GridSlots.Length; i++)
        {
            var slot = run.GridSlots[i];
            if (!slot.IsEmpty && slot.DaysMature == 0)
            {
                slot.CurrentGrowth = 100;
                slot.DaysMature = 1;
                maturedCount++;
            }
        }

        _saveManager?.SaveGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );

        Debug.Log($"[Cheat] {maturedCount} plantas amadurecidas. Recarregando cena...");
    }

    private void CheatSpawnCard(string cardID)
    {
        if (_handSystem == null || AppCore.Instance.GameLibrary == null) return;

        var run = _saveManager.Data.CurrentRun;
        
        if (run.Hand.Count >= run.MaxHandSize)
        {
            Debug.LogWarning("[Cheat] Mão cheia!");
            return;
        }

        if (!AppCore.Instance.GameLibrary.TryGetCard(new CardID(cardID), out CardData cardData))
        {
            Debug.LogWarning($"[Cheat] Card não encontrada: {cardID}");
            return;
        }

        var instance = new CardInstance(new CardID(cardID));
        run.Hand.Add(instance);
        AppCore.Instance.Events.Player.TriggerCardAdded(instance);

        Debug.Log($"[Cheat] Card adicionada: {cardID}");
    }

    private void ResetSaveData()
    {
        if (_saveManager != null)
        {
            _saveManager.DeleteSave();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
#endif
}