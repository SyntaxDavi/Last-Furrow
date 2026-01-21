using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks; // <--- Importante
using System.Threading; // Para CancellationToken

/// <summary>
/// Anima respiração do grid e reações a padrões detectados.
/// Versão: UniTask (Async/Await)
/// </summary>
public class GridBreathingController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;

    [Header("References")]
    [SerializeField] private Transform _gridTransform;
    [SerializeField] private AnimationCurve _breathingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _originalScale;

    // Controle único para a animação atual (seja Breathing ou Reaction)
    private CancellationTokenSource _activeCts;

    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
            if (_config == null)
            {
                Debug.LogError("[GridBreathing] PatternVisualConfig not found in Resources/Patterns!");
            }
            else
            {
                Debug.Log("[GridBreathing] Config loaded successfully");
            }
        }

        if (_gridTransform == null)
        {
            _gridTransform = transform;
        }

        _originalScale = _gridTransform.localScale;
        Debug.Log($"[GridBreathing] Initialized. Original scale: {_originalScale}");
    }

    private void Start()
    {
        Debug.Log("[GridBreathing] Starting breathing animation...");
        StartBreathing();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        StopCurrentAnimation();
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

    private void OnPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        if (matches == null || matches.Count == 0) return;

        // Dispara a reação (Fire-and-Forget)
        ReactToPatternAsync().Forget();
    }

    private void StopCurrentAnimation()
    {
        if (_activeCts != null)
        {
            _activeCts.Cancel();
            _activeCts.Dispose();
            _activeCts = null;
        }
    }

    private void StartBreathing()
    {
        StopCurrentAnimation();

        _activeCts = new CancellationTokenSource();
        // Inicia o loop de respiração
        BreathingLoopAsync(_activeCts.Token).Forget();
    }

    private async UniTaskVoid BreathingLoopAsync(CancellationToken token)
    {
        if (_config == null || _gridTransform == null) return;

        float time = 0f;

        // Loop infinito seguro
        while (!token.IsCancellationRequested)
        {
            // Verifica se o objeto foi destruído (segurança extra)
            if (this == null) return;

            if (_config.freezeAnimations)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                continue;
            }

            time += Time.deltaTime * _config.breathingSpeed;
            float curveValue = _breathingCurve.Evaluate(Mathf.PingPong(time, 1f));
            float scaleOffset = Mathf.Lerp(-_config.breathingAmount, _config.breathingAmount, curveValue);

            _gridTransform.localScale = _originalScale * (1f + scaleOffset);

            // Espera o próximo frame (Update)
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private async UniTaskVoid ReactToPatternAsync()
    {
        if (_config == null) return;

        // 1. Interrompe a respiração atual
        StopCurrentAnimation();

        // 2. Cria novo token para a reação
        _activeCts = new CancellationTokenSource();
        var token = _activeCts.Token;

        Vector3 targetScale = _originalScale * (1f + _config.patternReactionStrength);
        float elapsed = 0f;
        float halfDuration = _config.reactionDuration / 2f;

        try
        {
            // Fase 1: Inflar
            while (elapsed < halfDuration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                _gridTransform.localScale = Vector3.Lerp(_originalScale, targetScale, elapsed / halfDuration);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // Fase 2: Desinflar
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                _gridTransform.localScale = Vector3.Lerp(targetScale, _originalScale, elapsed / halfDuration);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // Garante escala final exata
            _gridTransform.localScale = _originalScale;

            // 3. Volta a respirar automaticamente
            StartBreathing();
        }
        catch (System.OperationCanceledException)
        {
            // Se a reação for cancelada (ex: objeto destruído), apenas sai
        }
    }
}