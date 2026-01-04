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
        // -----------------------------

        GUILayout.BeginArea(new Rect(10, 10, 220, 400));
        GUILayout.Box("CHEAT MENU");

        GUI.color = Color.green;
        if (GUILayout.Button("NOVA RUN"))
        {
            StartRun();
        }
        GUI.color = Color.white;

        GUILayout.Space(10);
        GUILayout.Label("--- FLUXO DE JOGO ---");

        // --- BOTÃO ÚNICO E CORRETO ---
        // Esse botão chama o DailyResolutionSystem.
        // O DailyResolutionSystem calcula o crescimento E chama o AdvanceDay() no final automaticamente.
        GUI.color = Color.cyan;
        if (GUILayout.Button("PRÓXIMO DIA (Noite)"))
        {
            GoToNextDay();
        }
        GUI.color = Color.white;

        GUILayout.Space(10);
        GUILayout.Label("--- DEBUG ---");

        if (GUILayout.Button("Matar Run (Game Over)")) KillRun();

        GUI.color = Color.red;
        if (GUILayout.Button("Resetar Save")) ResetSaveData();
        GUI.color = Color.white;

        GUILayout.Space(10);

        if (_runManager.IsRunActive)
        {
            var run = _saveManager.Data.CurrentRun;
            GUILayout.Label($"Dia: {run?.CurrentDay} (Semana {run?.CurrentWeek})");
            GUILayout.Label($"Estado: {AppCore.Instance.GameStateManager.CurrentState}");

            // Debug rápido para ver se a lógica rodou sem precisar de sprite
            int wateredCount = 0;
            int witheredCount = 0;
            foreach (var s in run.GridSlots)
            {
                if (s.IsWatered) wateredCount++;
                if (s.IsWithered) witheredCount++;
            }
            GUILayout.Label($"Slots Regados: {wateredCount}");
            GUILayout.Label($"Plantas Mortas: {witheredCount}");
        }

        GUILayout.EndArea();
    }

    private void StartRun()
    {
        if (_runManager.IsRunActive) return;
        _runManager.StartNewRun();
    }

    private void GoToNextDay()
    {
        if (_runManager.IsRunActive && _resolutionSystem != null)
        {
            Debug.Log("[CHEAT] Iniciando a Noite (Crescimento + Avançar Dia)...");
            // Isso dispara a Coroutine que:
            // 1. Processa lógica (GridService)
            // 2. Espera visualmente
            // 3. Chama _runManager.AdvanceDay() no final
            _resolutionSystem.StartEndDaySequence();
        }
    }

    private void KillRun()
    {
        if (_runManager.IsRunActive) _runManager.EndRun(RunEndReason.Abandoned);
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