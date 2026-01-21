using System.Collections;
using UnityEngine;

/// <summary>
/// Anima respiração do grid e reações a padrões detectados.
/// </summary>
public class GridBreathingController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("References")]
    [SerializeField] private Transform _gridTransform;
    [SerializeField] private AnimationCurve _breathingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Vector3 _originalScale;
    private Coroutine _breathingCoroutine;
    
    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_gridTransform == null)
        {
            _gridTransform = transform;
        }
        
        _originalScale = _gridTransform.localScale;
    }
    
    private void Start()
    {
        StartBreathing();
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected += OnPatternsDetected;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= OnPatternsDetected;
        }
    }
    
    private void OnPatternsDetected(System.Collections.Generic.List<PatternMatch> matches, int totalPoints)
    {
        if (matches == null || matches.Count == 0) return;
        StartCoroutine(ReactToPattern());
    }
    
    private void StartBreathing()
    {
        if (_breathingCoroutine != null)
        {
            StopCoroutine(_breathingCoroutine);
        }
        
        _breathingCoroutine = StartCoroutine(BreathingRoutine());
    }
    
    private IEnumerator BreathingRoutine()
    {
        if (_config == null || _gridTransform == null) yield break;
        
        float time = 0f;
        
        while (true)
        {
            if (_config.freezeAnimations)
            {
                yield return null;
                continue;
            }
            
            time += Time.deltaTime * _config.breathingSpeed;
            float curveValue = _breathingCurve.Evaluate(Mathf.PingPong(time, 1f));
            float scaleOffset = Mathf.Lerp(-_config.breathingAmount, _config.breathingAmount, curveValue);
            
            _gridTransform.localScale = _originalScale * (1f + scaleOffset);
            
            yield return null;
        }
    }
    
    private IEnumerator ReactToPattern()
    {
        if (_config == null) yield break;
        
        if (_breathingCoroutine != null)
        {
            StopCoroutine(_breathingCoroutine);
        }
        
        Vector3 targetScale = _originalScale * (1f + _config.patternReactionStrength);
        float elapsed = 0f;
        
        while (elapsed < _config.reactionDuration / 2f)
        {
            elapsed += Time.deltaTime;
            _gridTransform.localScale = Vector3.Lerp(_originalScale, targetScale, elapsed / (_config.reactionDuration / 2f));
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < _config.reactionDuration / 2f)
        {
            elapsed += Time.deltaTime;
            _gridTransform.localScale = Vector3.Lerp(targetScale, _originalScale, elapsed / (_config.reactionDuration / 2f));
            yield return null;
        }
        
        _gridTransform.localScale = _originalScale;
        StartBreathing();
    }
}
