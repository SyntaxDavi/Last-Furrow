using UnityEngine;

public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [Header("Debug Info")]
    [SerializeField] private bool _showDebugUI = true;

    // Referências cacheadas
    private RunManager _runManager;
    private SaveManager _saveManager;
    private DailyResolutionSystem _resolutionSystem;

    private void OnGUI()
    {
        if (!_showDebugUI) return;

        // --- LAZY LOAD (Segurança) ---
        if (AppCore.Instance != null)
        {
            if (_runManager == null) _runManager = AppCore.Instance.RunManager;
            if (_saveManager == null) _saveManager = AppCore.Instance.SaveManager;
            if (_resolutionSystem == null) _resolutionSystem = AppCore.Instance.DailyResolutionSystem;
        }

        if (_runManager == null || _saveManager == null) return;

        if (AppCore.Instance.GameStateManager.CurrentState == GameState.Shopping)
        {
            return;
        }
        // -----------------------------

        GUILayout.BeginArea(new Rect(10, 10, 250, 600)); // Aumentei a altura e largura
        GUILayout.Box("CHEAT MENU (DEBUG)");

        GUI.color = Color.green;
        if (GUILayout.Button("NOVA RUN (Reset)"))
        {
            ResetSaveData(); // Reseta e recarrega para garantir dados limpos
        }
        GUI.color = Color.white;

        // ---------------------------------------------------------
        // SEÇÃO: FLUXO DE TEMPO
        // ---------------------------------------------------------
        GUILayout.Space(10);
        GUILayout.Label($"--- TEMPO (Dia: {_saveManager.Data.CurrentRun?.CurrentDay}) ---");

        GUI.color = Color.cyan;
        if (GUILayout.Button("ENCERRAR DIA (Noite)"))
        {
            GoToNextDay();
        }

        if (GUILayout.Button("Forçar Shop (Debug)"))
        {
            _runManager.StartWeekendPhase();
        }

        GUI.color = Color.white;

        // ---------------------------------------------------------
        // SEÇÃO: PROGRESSÃO (METAS & VIDAS)
        // ---------------------------------------------------------
        GUILayout.Space(10);
        var run = _saveManager.Data.CurrentRun;

        if (run != null)
        {
            GUILayout.Label("--- METAS & VIDAS ---");
            GUILayout.Label($"Vidas: {run.CurrentLives} / {run.MaxLives}");
            GUILayout.Label($"Meta Semanal: {run.CurrentWeeklyScore} / {run.WeeklyGoalTarget}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+50 Pontos"))
            {
                run.CurrentWeeklyScore += 50;
                // Avisa o sistema para atualizar a UI (se houver)
                AppCore.Instance.Events.Progression.TriggerScoreUpdated(run.CurrentWeeklyScore, run.WeeklyGoalTarget);
            }
            if (GUILayout.Button("-1 Vida"))
            {
                run.CurrentLives--;
                AppCore.Instance.Events.Progression.TriggerLivesChanged(run.CurrentLives);
            }
            GUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------
        // SEÇÃO: ECONOMIA & CARTAS
        // ---------------------------------------------------------
        GUILayout.Space(10);
        if (run != null)
        {
            GUILayout.Label("--- ECONOMIA & MÃO ---");
            GUILayout.Label($"Dinheiro: ${run.Money}");
            GUILayout.Label($"Cartas na Mão: {run.Hand.Count} / {run.MaxHandSize}");

            if (GUILayout.Button("+ $100 Grana"))
            {
                AppCore.Instance.EconomyService.Earn(100, TransactionType.Debug);
            }

            if (GUILayout.Button("Limpar Mão (Debug)"))
            {
                run.Hand.Clear();
                // Força save para atualizar visual na proxima carga ou implementar evento de refresh total
                _saveManager.SaveGame();
                Debug.Log("Mão limpa nos dados. (O visual só atualiza se recarregar ou implementar evento de refresh)");
            }
        }

        // ---------------------------------------------------------
        // SEÇÃO: DADOS TÉCNICOS
        // ---------------------------------------------------------
        GUILayout.Space(10);
        GUILayout.Label("--- ESTADO TÉCNICO ---");
        GUILayout.Label($"Estado: {AppCore.Instance.GameStateManager.CurrentState}");

        if (run != null)
        {
            int wateredCount = 0;
            int witheredCount = 0;
            int cropCount = 0;
            foreach (var s in run.GridSlots)
            {
                if (!s.IsEmpty) cropCount++;
                if (s.IsWatered) wateredCount++;
                if (s.IsWithered) witheredCount++;
            }
            GUILayout.Label($"Plantas Vivas: {cropCount}");
            GUILayout.Label($"Regados: {wateredCount} | Mortas: {witheredCount}");
        }

        GUILayout.EndArea();
    }

    private void GoToNextDay()
    {
        if (_runManager.IsRunActive && _resolutionSystem != null)
        {
            Debug.Log("[CHEAT] Iniciando a Noite...");
            _resolutionSystem.StartEndDaySequence();
        }
    }

    private void ResetSaveData()
    {
        _saveManager.DeleteSave();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
#endif
}