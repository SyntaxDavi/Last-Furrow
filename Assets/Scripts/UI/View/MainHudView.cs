using UnityEngine;
using TMPro;

public class MainHudView : UIView
{
    [Header("Sub-Componentes")]
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private FeedbackText _moneyDeltaFeedback;

    [Header("Configuração Visual")]
    [SerializeField] private Color _positiveColor = Color.green;
    [SerializeField] private Color _negativeColor = Color.red;

    // NOTA: No Inspector, desmarque 'Start Hidden' para este objeto!

    private void Start()
    {
        if (AppCore.Instance != null)
        {
            UpdateMoneyText(AppCore.Instance.EconomyService.CurrentMoney);
            AppCore.Instance.EconomyService.OnBalanceChanged += HandleBalanceChanged;
        }
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.EconomyService.OnBalanceChanged -= HandleBalanceChanged;
        }
    }

    private void HandleBalanceChanged(int newBalance, int delta, TransactionType type)
    {
        UpdateMoneyText(newBalance);

        if (_moneyDeltaFeedback != null)
        {
            Color color = delta >= 0 ? _positiveColor : _negativeColor;
            string symbol = delta >= 0 ? "+" : "";
            _moneyDeltaFeedback.ShowFeedback($"{symbol}{delta}", color);
        }
    }

    private void UpdateMoneyText(int value)
    {
        _moneyText.text = $"${value}";
    }
}