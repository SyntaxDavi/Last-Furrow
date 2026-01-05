using UnityEngine;
using TMPro;
using System.Collections;

public class MainHudController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private TextMeshProUGUI _deltaText;
    private void Start()
    {
        // Garante estado inicial
        if (AppCore.Instance != null)
        {
            UpdateUI(AppCore.Instance.EconomyService.CurrentMoney);

            // Observer Pattern: Assina o evento
            AppCore.Instance.EconomyService.OnBalanceChanged += HandleBalanceChanged;
        }

        if (_deltaText != null) _deltaText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.EconomyService.OnBalanceChanged -= HandleBalanceChanged;
        }
    }

    // Callback desacoplado
    private void HandleBalanceChanged(int newBalance, int delta, TransactionType type)
    {
        UpdateUI(newBalance);

        if (_deltaText != null)
            StartCoroutine(ShowDeltaRoutine(delta));
    }

    private void UpdateUI(int amount)
    {
        if (_moneyText != null)
            _moneyText.text = $"${amount}";
    }

    private IEnumerator ShowDeltaRoutine(int delta)
    {
        _deltaText.text = delta >= 0 ? $"+{delta}" : $"{delta}";
        _deltaText.color = delta >= 0 ? Color.green : Color.red;
        _deltaText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        _deltaText.gameObject.SetActive(false);
    }
}