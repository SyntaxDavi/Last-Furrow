using UnityEngine;

public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    [Header("Debug Info")]
    [SerializeField] private bool _showDebugUI = true;

    private RunManager _runManager;
    private SaveManager _saveManager;

    private void OnGUI()
    {
        if (!_showDebugUI) return;

        // --- CORREÇÃO DE SEGURANÇA (Lazy Load) ---
        // Garante que as referências existam antes de desenhar
        if (_runManager == null || _saveManager == null)
        {
            if (AppCore.Instance != null)
            {
                _runManager = AppCore.Instance.RunManager;
                _saveManager = AppCore.Instance.SaveManager;
            }
            else
            {
                // Fallback local caso AppCore não esteja pronto
                _runManager = GetComponent<RunManager>();
                _saveManager = GetComponent<SaveManager>();
            }

            // Se ainda assim não achou, sai para não dar erro
            if (_runManager == null || _saveManager == null) return;
        }
        // ------------------------------------------

        GUILayout.BeginArea(new Rect(10, 10, 200, 450));
        GUILayout.Box("CHEAT MENU");

        GUI.color = Color.green;
        if (GUILayout.Button("INICIAR RUN (New Game)"))
        {
            StartRun();
        }
        GUI.color = Color.white;

        if (GUILayout.Button("Avançar Dia (+1)")) SkipDay();

        if (GUILayout.Button("Forçar Game Over")) KillRun();

        GUI.color = Color.red;
        if (GUILayout.Button("Deletar Save & Reiniciar")) ResetSaveData();
        GUI.color = Color.white;

        if (_runManager.IsRunActive)
        {
            var run = _saveManager.Data.CurrentRun;
            GUILayout.Label($"Week: {run?.CurrentWeek}");
            GUILayout.Label($"Day: {run?.CurrentDay}");
            GUILayout.Label($"State: {AppCore.Instance.GameStateManager.CurrentState}");
        }
        else
        {
            GUILayout.Label("STATUS: Sem Run Ativa");
        }

        GUILayout.EndArea();
    }

    private void StartRun()
    {
        if (_runManager.IsRunActive)
        {
            Debug.LogWarning("Já existe uma run ativa! Use 'Deletar Save' se quiser reiniciar.");
            return;
        }

        Debug.Log("[CHEAT] Iniciando Nova Run...");
        _runManager.StartNewRun();
    }

    private void SkipDay()
    {
        if (_runManager.IsRunActive)
            _runManager.AdvanceDay();
        else
            Debug.LogWarning("[CHEAT] Nenhuma run ativa para avançar.");
    }

    private void KillRun()
    {
        if (_runManager.IsRunActive)
        {
            Debug.Log("[CHEAT] Forçando Game Over...");
            _runManager.EndRun(RunEndReason.Abandoned);
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