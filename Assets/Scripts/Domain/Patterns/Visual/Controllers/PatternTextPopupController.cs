using System.Collections;
using System.Threading; 
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks; 

/// <summary>
/// Controla o popup de texto que aparece quando um padrão é detectado.
/// Versão: Migrada para UniTask (Async/Await).
/// </summary>
public class PatternTextPopupController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _patternNameText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 1.5f;
    [SerializeField] private float _startScale = 0.8f;
    [SerializeField] private float _endScale = 1.2f;

    // Controle de cancelamento para substituir StopAllCoroutines
    private CancellationTokenSource _cts;

    private void Awake()
    {
        Debug.Log("[PatternTextPopup] ========== AWAKE ==========");

        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        HideImmediate();
    }

    private void OnDestroy()
    {
        // Limpeza de segurança
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>
    /// MÉTODO PRINCIPAL: Mostra popup (Fire-and-Forget).
    /// </summary>
    public void ShowPattern(PatternMatch match)
    {
        // Inicia e "esquece" (.Forget evita warning de task não aguardada)
        ShowPatternAsync(match).Forget();
    }

    /// <summary>
    /// MÉTODO PÚBLICO: Retorna UniTask para ser aguardada pelo UIManager.
    /// Substitui o antigo ShowPatternCoroutine.
    /// </summary>
    public async UniTask ShowPatternAsync(PatternMatch match)
    {
        Debug.Log($"[PatternTextPopup] ?? ShowPatternAsync: {match.DisplayName}");

        if (_patternNameText == null)
        {
            Debug.LogError("[PatternTextPopup] ? PatternNameText is NULL!");
            return;
        }

        // 1. Cancelar animação anterior se houver
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // 2. Configurar Textos (Igual ao original)
        SetupVisuals(match);

        // 3. Executar Animação
        try
        {
            await AnimatePopup(token);
        }
        catch (System.OperationCanceledException)
        {
            // Normal: foi cancelado por um novo popup ou destruição do objeto
            // Não precisamos fazer nada, o próximo popup vai resetar o estado
        }
    }

    // Mantido por compatibilidade de nome, mas redireciona para o Async
    // Caso algum script antigo ainda tente chamar Coroutine, isso evita erro de compilação imediato,
    // mas o ideal é atualizar quem chama.
    public IEnumerator ShowPatternCoroutine(PatternMatch match)
    {
        return ShowPatternAsync(match).ToCoroutine();
    }

    private void SetupVisuals(PatternMatch match)
    {
        _patternNameText.text = match.DisplayName.ToUpper();

        int tier = CalculateTier(match.BaseScore);
        Color tierColor = _config != null ? _config.GetTierColor(tier) : Color.white;

        _patternNameText.color = tierColor;

        if (_scoreText != null)
        {
            _scoreText.text = $"+{match.BaseScore}";
            _scoreText.color = tierColor;
        }
    }

    private async UniTask AnimatePopup(CancellationToken token)
    {
        // Estado inicial
        transform.localScale = Vector3.one * _startScale;
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(true);

        // FASE 1: Fade in + Scale up (25%)
        float fadeInDuration = _animationDuration * 0.25f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            // Verifica cancelamento a cada frame
            if (token.IsCancellationRequested) return;

            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            _canvasGroup.alpha = t;
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);

            // Espera o próximo frame (Update)
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        _canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * _endScale;

        // FASE 2: Hold (50%)
        // Converte duração para milissegundos
        int holdDelay = (int)((_animationDuration * 0.5f) * 1000);
        await UniTask.Delay(holdDelay, cancellationToken: token);

        // FASE 3: Fade out (25%)
        float fadeOutDuration = _animationDuration * 0.25f;
        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            if (token.IsCancellationRequested) return;

            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            _canvasGroup.alpha = 1f - t;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        HideImmediate();
    }

    private int CalculateTier(int baseScore)
    {
        if (baseScore >= 80) return 4;
        if (baseScore >= 35) return 3;
        if (baseScore >= 15) return 2;
        return 1;
    }

    private void HideImmediate()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(false);
    }
}