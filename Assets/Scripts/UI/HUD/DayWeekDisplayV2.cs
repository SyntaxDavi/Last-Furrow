using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Exibe e anima o texto de Dia/Semana V2 - ROBUSTO E DEFENSIVO.
/// 
/// VERSÃO 2: Null checks completos + validações + logs verbosos
/// 
/// INSTALAÇÃO:
/// 1. Delete DayWeekDisplay antigo do GameObject
/// 2. Add Component: DayWeekDisplayV2
/// 3. Play e veja logs detalhados
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DayWeekDisplayV2 : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float _pulseDuration = 0.4f;
    [SerializeField] private float _pulseScale = 1.2f;
    [SerializeField] private AnimationCurve _pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

    [Header("Format Settings")]
    [SerializeField] private string _textFormat = "Dia {0} - Semana {1}";

    [Header("Debounce Settings")]
    [SerializeField] private float _debounceTime = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    private TextMeshProUGUI _text;
    private RectTransform _rectTransform;
    private Coroutine _currentAnimation;
    private Coroutine _debounceCoroutine;

    private int _currentDay = 1;
    private int _currentWeek = 1;
    private bool _pendingUpdate = false;

    private UIContext _context;
    private bool _isInitialized = false;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();

        if (_text == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? CRITICAL: Componente TextMeshProUGUI não encontrado!");
        }

        if (_rectTransform == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? CRITICAL: RectTransform não encontrado!");
        }
    }

    public void Initialize(UIContext context)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[DayWeekDisplayV2] ? Já foi inicializado!");
            return;
        }

        // === VALIDAÇÕES CRÍTICAS ===
        if (context == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? CRITICAL: UIContext é NULL!");
            return;
        }

        if (context.RunData == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? CRITICAL: context.RunData é NULL!");
            return;
        }

        if (context.TimeEvents == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? CRITICAL: context.TimeEvents é NULL!");
            return;
        }

        if (_text == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? CRITICAL: TextMeshProUGUI null! Verifique componente.");
            return;
        }

        _context = context;

        // Lê estado inicial
        _currentDay = _context.RunData.CurrentDay;
        _currentWeek = _context.RunData.CurrentWeek;

        Debug.Log($"[DayWeekDisplayV2] ? Estado inicial: Dia {_currentDay}, Semana {_currentWeek}");

        // Atualiza texto inicial
        UpdateText(immediate: true);

        // Escuta eventos
        _context.TimeEvents.OnDayChanged += HandleDayChanged;
        _context.TimeEvents.OnWeekChanged += HandleWeekChanged;

        _isInitialized = true;
        Debug.Log("[DayWeekDisplayV2] ?? INICIALIZADO COM SUCESSO!");
    }

    private void OnDestroy()
    {
        if (_context != null && _context.TimeEvents != null)
        {
            _context.TimeEvents.OnDayChanged -= HandleDayChanged;
            _context.TimeEvents.OnWeekChanged -= HandleWeekChanged;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DayWeekDisplayV2] ? HandleDayChanged chamado mas não inicializado!");
            return;
        }

        _currentDay = newDay;
        _pendingUpdate = true;
        StartDebounce();

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplayV2] ?? Dia atualizado: {_currentDay}");
    }

    private void HandleWeekChanged(int newWeek)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DayWeekDisplayV2] ? HandleWeekChanged chamado mas não inicializado!");
            return;
        }

        _currentWeek = newWeek;
        _pendingUpdate = true;
        StartDebounce();

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplayV2] ?? Semana atualizada: {_currentWeek}");
    }

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
            Debug.Log($"[DayWeekDisplayV2] ? DEBOUNCE: Atualizando texto após {_debounceTime}s");
            UpdateText(immediate: false);
            _pendingUpdate = false;
        }

        _debounceCoroutine = null;
    }

    private void UpdateText(bool immediate)
    {
        if (_text == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? Texto null em UpdateText!");
            return;
        }

        _text.text = string.Format(_textFormat, _currentDay, _currentWeek);

        if (_showDebugLogs)
            Debug.Log($"[DayWeekDisplayV2] ?? Texto atualizado: \"{_text.text}\" (immediate={immediate})");

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
        if (_rectTransform == null)
        {
            Debug.LogError("[DayWeekDisplayV2] ? RectTransform null em PulseRoutine!");
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < _pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _pulseDuration;

            float curveValue = _pulseCurve.Evaluate(t);
            float scale = Mathf.Lerp(1f, _pulseScale, Mathf.Sin(curveValue * Mathf.PI));

            _rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        _rectTransform.localScale = Vector3.one;
        _currentAnimation = null;
    }
}
