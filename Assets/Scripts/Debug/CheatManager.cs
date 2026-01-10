using UnityEngine;

public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [Header("Debug Info")]
    [SerializeField] private bool _showDebugUI = true;
    [SerializeField] private ProgressionSettingsSO _progressionSettings; 

    // Lazy References
    private RunManager _runManager => AppCore.Instance?.RunManager;
    private SaveManager _saveManager => AppCore.Instance?.SaveManager;
    private DailyResolutionSystem _resolutionSystem => AppCore.Instance?.DailyResolutionSystem;

    private void OnGUI()
    {
        if (!_showDebugUI || AppCore.Instance == null) return;

        // Se estiver na loja, esconde para não atrapalhar a UI
        if (AppCore.Instance.GameStateManager.CurrentState == GameState.Shopping) return;

        GUILayout.BeginArea(new Rect(10, 10, 280, 700));
        GUILayout.Box("CHEAT MENU (DEBUG)");

        // --- RESET ---
        GUI.color = Color.red;
        if (GUILayout.Button("DELETAR SAVE & RESTART"))
        {
            ResetSaveData();
        }
        GUI.color = Color.white;

        var run = _saveManager?.Data?.CurrentRun;
        if (run == null)
        {
            GUILayout.Label("Nenhuma Run Ativa.");
            GUILayout.EndArea();
            return;
        }

        // --- TEMPO ---
        GUILayout.Space(10);
        GUILayout.Label($"--- TEMPO (Semana: {run.CurrentWeek} | Dia: {run.CurrentDay}) ---");
        GUILayout.Label($"Fase: {_runManager.CurrentPhase}");

        GUI.color = Color.cyan;
        if (GUILayout.Button("ENCERRAR DIA (Noite)"))
        {
            GoToNextDay();
        }
        GUI.color = Color.white;

        // --- PROGRESSÃO ---
        GUILayout.Space(10);
        GUILayout.Label("--- METAS & VIDAS ---");
        GUILayout.Label($"Vidas: {run.CurrentLives} / {run.MaxLives}");
        GUILayout.Label($"Meta: {run.CurrentWeeklyScore} / {run.WeeklyGoalTarget}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+50 Score"))
        {
            run.CurrentWeeklyScore += 50;
            AppCore.Instance.Events.Progression.TriggerScoreUpdated(run.CurrentWeeklyScore, run.WeeklyGoalTarget);
        }
        if (GUILayout.Button("-1 Vida"))
        {
            run.CurrentLives--;
            AppCore.Instance.Events.Progression.TriggerLivesChanged(run.CurrentLives);
        }
        GUILayout.EndHorizontal();

        // --- CORREÇÃO DO PROBLEMA DA META ---
        if (_progressionSettings != null)
        {
            if (GUILayout.Button("Forçar Meta do Config (Refresh)"))
            {
                int correctGoal = _progressionSettings.GetGoalForWeek(run.CurrentWeek);
                run.WeeklyGoalTarget = correctGoal;
                AppCore.Instance.Events.Progression.TriggerScoreUpdated(run.CurrentWeeklyScore, run.WeeklyGoalTarget);
                Debug.Log($"Meta atualizada para {correctGoal} com base no Config.");
            }
        }
        else
        {
            GUILayout.Label("(Arraste ProgressionSettings no Inspector para atualizar meta)");
        }

        // --- ECONOMIA & MÃO ---
        GUILayout.Space(10);
        GUILayout.Label($"Dinheiro: ${run.Money}");
        GUILayout.Label($"Mão: {run.Hand.Count} cartas");

        if (GUILayout.Button("+ $100 Grana"))
        {
            AppCore.Instance.EconomyService.Earn(100, TransactionType.Debug);
        }

        if (GUILayout.Button("Limpar Mão (Dados)"))
        {
            run.Hand.Clear();
            _saveManager.SaveGame();
            // Para atualizar visualmente, precisaria recarregar a cena ou disparar evento complexo
            Debug.Log("Mão limpa. Recarregue a cena ou espere o próximo draw.");
        }

        // --- DADOS TÉCNICOS ---
        GUILayout.Space(10);
        int plants = 0;
        foreach (var s in run.GridSlots) if (!s.IsEmpty) plants++;
        GUILayout.Label($"Plantas Vivas: {plants}");

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