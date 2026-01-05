using UnityEngine;
using TMPro;
using System.Collections;

// Agora herda de UIView para ganhar os poderes de Fade e Controle
public class WeeklyReportUI : UIView
{
    [Header("Textos & Mensagens")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _detailsText;

    [SerializeField] private string _successTitle = "META BATIDA!";
    [SerializeField] private string _successDetail = "A produção foi suficiente.";
    [SerializeField] private string _failTitle = "FALHA NA META";

    [Header("Configuração")]
    [SerializeField] private float _displayDuration = 3.0f;
    [SerializeField] private Color _successColor = Color.green;
    [SerializeField] private Color _failColor = Color.red;

    private Coroutine _hideTimerCoroutine;

    protected override void Awake()
    {
        _startHidden = true; 
        base.Awake(); 
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Progression.OnWeeklyGoalEvaluated += ShowReport;
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Progression.OnWeeklyGoalEvaluated -= ShowReport;
        }
    }

    private void ShowReport(bool success, int currentLives)
    {
        if (success)
        {
            _titleText.text = _successTitle;
            _titleText.color = _successColor;
            _detailsText.text = _successDetail;
        }
        else
        {
            _titleText.text = _failTitle;
            _titleText.color = _failColor;
            _detailsText.text = $"A fazenda sofreu prejuízos.\nRestam {currentLives} corações.";
        }

        // Usa o método Show() da classe pai (UIView)
        Show();

        // Reinicia o timer de esconder
        if (_hideTimerCoroutine != null) StopCoroutine(_hideTimerCoroutine);
        _hideTimerCoroutine = StartCoroutine(AutoHideRoutine());
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSeconds(_displayDuration);
        Hide(); // Usa o método Hide() da classe pai (com fade out)
    }
}