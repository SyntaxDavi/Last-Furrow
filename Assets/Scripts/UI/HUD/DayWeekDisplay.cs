using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Exibe e anima o texto de Dia/Semana - REFATORADO.
/// 
/// ?? RENOMEAR PARA DayWeekDisplay após deletar o antigo!
/// 
/// RESPONSABILIDADE:
/// - Atualizar texto quando dia/semana mudam
/// - Executar animação de pulse/bounce
/// - Debounce para pulse único (agrupa mudanças de dia+semana)
/// 
/// ARQUITETURA:
/// - Event-driven: Escuta TimeEvents via UIContext
/// - Dependency Injection: Recebe UIContext, não usa AppCore.Instance
/// - SOLID: Não acessa RunData diretamente
/// 
/// REFATORAÇÕES:
/// - ? Injeção via UIContext
/// - ? Debounce para pulse único
/// - ? Removido AppCore.Instance
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DayWeekDisplayRefactored : MonoBehaviour
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

    [Header("Debounce Settings")]
    [Tooltip("Tempo de espera para agrupar mudanças de dia+semana (segundos)")]
    [SerializeField] private float _debounceTime = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    private TextMeshProUGUI _text;
    private RectTransform _rectTransform;
    private Coroutine _currentAnimation;
    private Coroutine _debounceCoroutine;

    private int _currentDay = 1;
    private int _currentWeek = 1;
    private bool _pendingUpdate = false;

    // Contexto injetado
    private UIContext _context;
    private bool _isInitialized = false;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Inicialização via UIBootstrapper (injeção de dependências).
    /// </summary>
    public void Initialize(UIContext context)
    {
        if (_isInitialized)
        {
            if (_showDebugLogs)
                Debug.LogWarning("[DayWeekDisplay] Já foi inicializado!");
            return;
        }

        _context = context ?? throw new System.ArgumentNullException(nameof(context));

        // Lê estado inicial via interface
        _currentDay = _context.RunData.CurrentDay;
        _currentWeek = _context.RunData.CurrentWeek;

        // Atualiza texto inicial (sem animação)
        UpdateText(immediate: true);

        // Escuta eventos via contexto
        _context.TimeEvents.OnDayChanged += HandleDayChanged;
        _context.TimeEvents.OnWeekChanged += HandleWeekChanged;

        _isInitialized = true;

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplay] ? Inicializado. Dia {_currentDay}, Semana {_currentWeek}");
    }

    private void OnDestroy()
    {
        if (_context != null)
        {
            _context.TimeEvents.OnDayChanged -= HandleDayChanged;
            _context.TimeEvents.OnWeekChanged -= HandleWeekChanged;
        }
    }

    /// <summary>
    /// Handler de mudança de dia (com debounce).
    /// </summary>
    private void HandleDayChanged(int newDay)
    {
        _currentDay = newDay;
        _pendingUpdate = true;
        StartDebounce();

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplay] Dia atualizado: {_currentDay}");
    }

    /// <summary>
    /// Handler de mudança de semana (com debounce).
    /// </summary>
    private void HandleWeekChanged(int newWeek)
    {
        _currentWeek = newWeek;
        _pendingUpdate = true;
        StartDebounce();

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplay] Semana atualizada: {_currentWeek}");
    }

    /// <summary>
    /// Sistema de debounce: Aguarda pequeno intervalo antes de animar.
    /// Isso agrupa mudanças de dia+semana em um único pulse.
    /// </summary>
    private void StartDebounce()
    {
        if (_debounceCoroutine != null)
        {
            StopCoroutine(_debounceCoroutine);
        }

        _debounceCoroutine = StartCoroutine(DebounceRoutine());
    }

    private IEnumerator DebounceRoutine()
    {
        yield return new WaitForSeconds(_debounceTime);

        if (_pendingUpdate)
        {
            UpdateText(immediate: false);
            _pendingUpdate = false;
        }

        _debounceCoroutine = null;
    }

    /// <summary>
    /// Atualiza o texto exibido.
    /// </summary>
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

    /// <summary>
    /// Executa animação de pulse/bounce.
    /// </summary>
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
