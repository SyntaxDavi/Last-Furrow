using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Exibe e anima o texto de Dia/Semana (ex: "Dia 3 - Semana 1").
/// 
/// RESPONSABILIDADE:
/// - Atualizar texto quando dia/semana mudam
/// - Executar animação de pulse/bounce
/// 
/// ARQUITETURA:
/// - Event-driven: Escuta TimeEvents.OnDayChanged
/// - SOLID: Não acessa RunData diretamente
/// - Extensível: Adicionar efeitos visuais sem modificar código
/// 
/// FUTURO:
/// - Cor especial no último dia da semana
/// - Efeitos de partículas em transição de semana
/// - Localization support
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DayWeekDisplay : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Duração da animação de pulse quando muda de dia")]
    [SerializeField] private float _pulseDuration = 0.4f;

    [Tooltip("Escala máxima durante o pulse")]
    [SerializeField] private float _pulseScale = 1.2f;

    [Tooltip("Curva de animação do pulse")]
    [SerializeField] private AnimationCurve _pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

    [Header("Format Settings")]
    [Tooltip("Formato do texto. Use {0} para dia e {1} para semana")]
    [SerializeField] private string _textFormat = "Dia {0} - Semana {1}";

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    private TextMeshProUGUI _text;
    private RectTransform _rectTransform;
    private Coroutine _currentAnimation;

    private int _currentDay = 1;
    private int _currentWeek = 1;

    private bool _isInitialized = false;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        // Espera AppCore
        while (AppCore.Instance == null)
        {
            yield return null;
        }

        // Espera RunData
        while (AppCore.Instance.SaveManager?.Data?.CurrentRun == null)
        {
            yield return null;
        }

        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        // Lê estado inicial
        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;
        _currentDay = runData.CurrentDay;
        _currentWeek = runData.CurrentWeek;

        // Atualiza texto inicial (sem animação)
        UpdateText(immediate: true);

        // Escuta eventos
        AppCore.Instance.Events.Time.OnDayChanged += HandleDayChanged;
        AppCore.Instance.Events.Time.OnWeekChanged += HandleWeekChanged;

        _isInitialized = true;

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplay] ? Inicializado. Dia {_currentDay}, Semana {_currentWeek}");
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Time.OnDayChanged -= HandleDayChanged;
            AppCore.Instance.Events.Time.OnWeekChanged -= HandleWeekChanged;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        _currentDay = newDay;
        UpdateText(immediate: false);

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplay] Dia atualizado: {_currentDay}");
    }

    private void HandleWeekChanged(int newWeek)
    {
        _currentWeek = newWeek;
        UpdateText(immediate: false);

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplay] Semana atualizada: {_currentWeek}");
    }

    private void UpdateText(bool immediate)
    {
        // Atualiza texto
        _text.text = string.Format(_textFormat, _currentDay, _currentWeek);

        // Anima se não for imediato
        if (!immediate)
        {
            AnimatePulse();
        }
    }

    private void AnimatePulse()
    {
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
        }

        _currentAnimation = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;

        while (elapsed < _pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _pulseDuration;

            // Pulse: 1 ? pulseScale ? 1
            float curveValue = _pulseCurve.Evaluate(t);
            float scale = Mathf.Lerp(1f, _pulseScale, Mathf.Sin(curveValue * Mathf.PI));

            _rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        _rectTransform.localScale = Vector3.one;
        _currentAnimation = null;
    }
}
