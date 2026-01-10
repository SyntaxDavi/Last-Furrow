using UnityEngine;
using TMPro;

public class WeeklyGoalHUDView : UIView
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _goalText;

    // Formato do texto. {0} = Score Atual, {1} = Meta
    [SerializeField] private string _format = "Meta: {0} / {1}";

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            // 1. Escuta mudanças nos pontos
            AppCore.Instance.Events.Progression.OnScoreUpdated += UpdateDisplay;

            // 2. Escuta mudanças de modo (Esconder em cutscenes, mostrar em jogo)
            AppCore.Instance.Events.UI.OnHUDModeChanged += HandleHUDMode;

            // 3. Atualização Inicial (Crucial para não começar vazio)
            RefreshImmediate();
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Progression.OnScoreUpdated -= UpdateDisplay;
            AppCore.Instance.Events.UI.OnHUDModeChanged -= HandleHUDMode;
        }
    }

    private void RefreshImmediate()
    {
        var run = AppCore.Instance.SaveManager.Data.CurrentRun;
        if (run != null)
        {
            UpdateDisplay(run.CurrentWeeklyScore, run.WeeklyGoalTarget);
        }
    }

    private void UpdateDisplay(int currentScore, int targetScore)
    {
        if (_goalText != null)
        {
            _goalText.text = string.Format(_format, currentScore, targetScore);

            // Opcional: Mudar cor se atingiu a meta
            if (currentScore >= targetScore)
                _goalText.color = Color.green;
            else
                _goalText.color = Color.white;
        }
    }

    private void HandleHUDMode(HUDMode mode)
    {
        // Regra visual: Mostra na Produção e na Loja. Esconde em Cutscenes/Menus.
        bool shouldShow = (mode == HUDMode.Production || mode == HUDMode.Shopping);

        if (shouldShow) Show();
        else Hide();
    }
}