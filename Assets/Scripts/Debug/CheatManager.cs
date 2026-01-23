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


    [Header("Card Spawn")]
    [SerializeField] private List<string> _quickSpawnCardIDs = new List<string>
    {
        "card_corn",
        "card_carrot",
        "card_onion",
        "card_potato",
        "card_lettuce",
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
        DrawPatternSection(run);  // ONDA 4: Pattern Debug
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
        
        GUILayout.Space(5);
        GUI.color = Color.green;
        if (GUILayout.Button("?? Plantar Auto (Qualquer Crop)"))
        {
            CheatAutoPlant();
        }
        GUI.color = Color.white;
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
        
        GUI.color = Color.red;
        if (GUILayout.Button("? Limpar Toda Mão"))
        {
            CheatClearHand(run);
        }
        GUI.color = Color.white;

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
        
        GUILayout.Space(10);
        GUILayout.Label("?? DEBUG");
        
        GUI.color = Color.cyan;
        if (GUILayout.Button("?? Testar Debug Logs"))
        {
            TestDebugLogs();
        }
        GUI.color = Color.white;
    }
    
    private void TestDebugLogs()
    {
        Debug.Log("========== TESTE DE DEBUG ==========");
        Debug.Log("[Cheat] ✓ Debug.Log funcionando!");
        Debug.LogWarning("[Cheat] ⚠️ Debug.LogWarning funcionando!");
        Debug.LogError("[Cheat] ❌ Debug.LogError funcionando!");
        Debug.Log("====================================");
        
        Debug.Log($"[Cheat] AppCore.Instance: {(AppCore.Instance != null ? "OK" : "NULL")}");
        Debug.Log($"[Cheat] SaveManager: {(_saveManager != null ? "OK" : "NULL")}");
        Debug.Log($"[Cheat] GridService: {(_gridService != null ? "OK" : "NULL")}");
        Debug.Log($"[Cheat] GameLibrary: {(AppCore.Instance?.GameLibrary != null ? "OK" : "NULL")}");
    }
    
    // ===== ONDA 4: Pattern Debug Section =====
    
    private void DrawPatternSection(RunData run)
    {
        GUILayout.Label("?? PATTERNS (Onda 4)");
        
        // Mostrar estatísticas de tracking
        var tracking = AppCore.Instance?.PatternTracking;
        if (tracking != null)
        {
            int activeCount = tracking.GetActivePatternsCount();
            GUILayout.Label($"Padrões ativos: {activeCount}");
            GUILayout.Label($"Total detectados: {run.TotalPatternsDetected}");
            GUILayout.Label($"Recorde diário: {run.HighestDailyPatternScore} pts");
        }
        else
        {
            GUILayout.Label("? PatternTracking não inicializado");
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("?? Detectar Padrões Agora"))
        {
            CheatDetectPatterns();
        }
        
        if (GUILayout.Button("?? Log Tracking Status"))
        {
            CheatLogTrackingStatus();
        }
        
        if (GUILayout.Button("?? Reset Weekly Tracking"))
        {
            CheatResetWeeklyTracking();
        }
        
        if (GUILayout.Button("? Simular Fim de Dia"))
        {
            CheatSimulateEndDay();
        }
    }
    
    private void CheatDetectPatterns()
    {
        if (_gridService == null)
        {
            Debug.LogWarning("[Cheat] GridService não disponível");
            return;
        }
        
        var detector = AppCore.Instance?.PatternDetector;
        var calculator = AppCore.Instance?.PatternCalculator;
        
        if (detector == null || calculator == null)
        {
            Debug.LogWarning("[Cheat] Pattern System não inicializado");
            return;
        }
        
        var matches = detector.DetectAll(_gridService);
        Debug.Log($"[Cheat] ??????? DETECÇÃO DE PADRÕES ???????");
        Debug.Log($"[Cheat] Padrões detectados: {matches.Count}");
        
        foreach (var match in matches)
        {
            string slots = string.Join(",", match.SlotIndices);
            Debug.Log($"[Cheat] • {match.DisplayName} (slots: {slots}) = {match.BaseScore} base pts");
        }
        
        int totalScore = calculator.CalculateTotal(matches, _gridService);
        Debug.Log($"[Cheat] TOTAL: {totalScore} pontos");
        Debug.Log($"[Cheat] ???????????????????????????????????");
    }
    
    private void CheatLogTrackingStatus()
    {
        var tracking = AppCore.Instance?.PatternTracking;
        var run = _saveManager?.Data?.CurrentRun;
        
        if (tracking == null || run == null)
        {
            Debug.LogWarning("[Cheat] Tracking não disponível");
            return;
        }
        
        Debug.Log($"[Cheat] ??????? PATTERN TRACKING STATUS ???????");
        Debug.Log($"[Cheat] Semana: {run.CurrentWeek} | Dia: {run.CurrentDay}");
        Debug.Log($"[Cheat] Padrões ativos: {tracking.GetActivePatternsCount()}");
        
        if (run.ActivePatterns != null)
        {
            foreach (var kvp in run.ActivePatterns)
            {
                var data = kvp.Value;
                float decay = Mathf.Pow(0.9f, data.DaysActive - 1);
                string recreated = data.IsRecreated ? " [RECREATED]" : "";
                Debug.Log($"[Cheat] • {data.PatternID}: Dia {data.DaysActive} ({decay:P0} pts){recreated}");
            }
        }
        
        if (run.BrokenPatternIDs != null && run.BrokenPatternIDs.Count > 0)
        {
            Debug.Log($"[Cheat] Padrões quebrados (dão bonus): {string.Join(", ", run.BrokenPatternIDs)}");
        }
        
        Debug.Log($"[Cheat] ??????????????????????????????????????");
    }
    
    private void CheatResetWeeklyTracking()
    {
        AppCore.Instance?.OnWeeklyReset();
        Debug.Log("[Cheat] Weekly tracking resetado (broken patterns limpos)");
    }
    
    private void CheatSimulateEndDay()
    {
        if (_resolutionSystem == null)
        {
            Debug.LogWarning("[Cheat] DailyResolutionSystem não disponível");
            return;
        }
        
        _resolutionSystem.StartEndDaySequence();
        Debug.Log("[Cheat] Fim de dia iniciado");
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
                run.GridSlots[i] = new CropState(default(CropID));
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
    
    private void CheatClearHand(RunData run)
    {
        if (run.Hand.Count == 0)
        {
            Debug.LogWarning("[Cheat] Mão já está vazia!");
            return;
        }
        
        int clearedCount = run.Hand.Count;
        run.Hand.Clear();
        
        _saveManager?.SaveGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
        
        Debug.Log($"[Cheat] {clearedCount} cartas removidas da mão! Recarregando cena...");
    }
    
    private void CheatAutoPlant()
    {
        Debug.Log("[Cheat] ========== INICIANDO PLANTAR AUTO ==========");
        
        if (_gridService == null)
        {
            Debug.LogError("[Cheat] ERRO: GridService é NULL!");
            return;
        }
        
        var run = _saveManager.Data.CurrentRun;
        if (run == null)
        {
            Debug.LogError("[Cheat] ERRO: RunData é NULL!");
            return;
        }
        
        var library = AppCore.Instance?.GameLibrary;
        if (library == null)
        {
            Debug.LogError("[Cheat] ERRO: GameLibrary não disponível!");
            return;
        }
        
        Debug.Log($"[Cheat] Total de slots no grid: {run.GridSlots.Length}");
        Debug.Log($"[Cheat] Total de slot states: {run.SlotStates.Length}");
        
        int plantedCount = 0;
        int unlockedCount = 0;
        int emptyCount = 0;
        
        for (int i = 0; i < run.GridSlots.Length; i++)
        {
            var slotState = run.SlotStates[i];
            var gridSlot = run.GridSlots[i];
            
            if (slotState.IsUnlocked) unlockedCount++;
            if (gridSlot.IsEmpty) emptyCount++;
            
            if (slotState.IsUnlocked && gridSlot.IsEmpty)
            {
                string randomCropID = GetRandomCropID(library);
                if (randomCropID != null)
                {
                    Debug.Log($"[Cheat] Plantando {randomCropID} no slot {i}");
                    
                    run.GridSlots[i] = new CropState(new CropID(randomCropID))
                    {
                        CurrentGrowth = 0,
                        IsWatered = false,
                        DaysMature = 0
                    };
                    plantedCount++;
                }
                else
                {
                    Debug.LogWarning($"[Cheat] Falha ao obter crop aleatório para slot {i}");
                }
            }
            else
            {
                if (!slotState.IsUnlocked)
                    Debug.Log($"[Cheat] Slot {i} BLOQUEADO");
                if (!gridSlot.IsEmpty)
                    Debug.Log($"[Cheat] Slot {i} JÁ OCUPADO com {gridSlot.CropID.Value}");
            }
        }
        
        Debug.Log($"[Cheat] Estatísticas:");
        Debug.Log($"[Cheat]   - Slots desbloqueados: {unlockedCount}");
        Debug.Log($"[Cheat]   - Slots vazios: {emptyCount}");
        Debug.Log($"[Cheat]   - Slots plantados: {plantedCount}");
        
        if (plantedCount > 0)
        {
            Debug.Log($"[Cheat] Salvando e recarregando cena...");
            _saveManager?.SaveGame();
            
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
        else
        {
            Debug.LogWarning("[Cheat] ⚠️ NENHUM SLOT FOI PLANTADO!");
            Debug.LogWarning($"[Cheat] Verifique: Slots desbloqueados={unlockedCount}, Slots vazios={emptyCount}");
        }
        
        Debug.Log("[Cheat] ========== FIM PLANTAR AUTO ==========");
    }
    
    private string GetRandomCropID(IGameLibrary library)
    {
        Debug.Log("[Cheat] Buscando crop aleatório...");
        
        // CORREÇÃO: IDs corretos com prefixo "crop_"
        var cropIDs = new string[] { "crop_corn", "crop_carrot" };
        var validCropIDs = new System.Collections.Generic.List<string>();
        
        foreach (var cropID in cropIDs)
        {
            if (library.TryGetCrop(new CropID(cropID), out CropData cropData))
            {
                validCropIDs.Add(cropID);
                Debug.Log($"[Cheat]   ✓ Crop válido encontrado: {cropID}");
            }
            else
            {
                Debug.LogWarning($"[Cheat]   ✗ Crop NÃO encontrado: {cropID}");
            }
        }
        
        Debug.Log($"[Cheat] Total de crops válidos: {validCropIDs.Count}");
        
        if (validCropIDs.Count == 0)
        {
            Debug.LogError("[Cheat] ERRO: Nenhum crop válido encontrado no GameLibrary!");
            return null;
        }
        
        string selected = validCropIDs[Random.Range(0, validCropIDs.Count)];
        Debug.Log($"[Cheat] Crop selecionado: {selected}");
        return selected;
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