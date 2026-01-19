using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Componente visual individual de um coração (View).
/// 
/// RESPONSABILIDADE:
/// - Gerenciar aparência (cheio/vazio)
/// - Executar animações (pop-up, fade, bounce)
/// - Responder a comandos do Manager
/// 
/// SOLID:
/// - Single Responsibility: Apenas visual/animação
/// - Open/Closed: Adicionar novas animações não quebra código existente
/// - Dependency Injection: Não depende de AppCore, apenas recebe comandos
/// 
/// NÃO FAZ:
/// - Acessar RunData
/// - Decidir quando ganhar/perder vida
/// - Gerenciar outros corações
/// </summary>
[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class HeartView : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color _fullColor = new Color(1f, 0f, 0f, 1f); // Vermelho
    [SerializeField] private Color _emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Cinza escuro

    [Header("Animation Settings")]
    [SerializeField] private float _spawnDuration = 0.3f;
    [SerializeField] private float _loseDuration = 0.4f;
    [SerializeField] private float _healDuration = 0.5f;
    [SerializeField] private AnimationCurve _popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Componentes (cached)
    private Image _image;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    // Estado interno
    private bool _isFull;
    private Coroutine _currentAnimation;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();

        // Estado inicial invisível (Manager vai spawnar)
        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Define estado inicial sem animação (para pooling/reset).
    /// </summary>
    public void SetState(bool isFull, bool immediate = false)
    {
        _isFull = isFull;
        _image.color = isFull ? _fullColor : _emptyColor;

        if (immediate)
        {
            _canvasGroup.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Animação de spawn inicial (pop-up suave).
    /// </summary>
    public void AnimateSpawn()
    {
        StopCurrentAnimation();
        _currentAnimation = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Animação de perda de vida (fica cinza, não desaparece).
    /// Preparado para adicionar "quebrar" no futuro.
    /// </summary>
    public void AnimateLose()
    {
        StopCurrentAnimation();
        _currentAnimation = StartCoroutine(LoseRoutine());
    }

    /// <summary>
    /// Animação de cura (cinza ? vermelho com bounce).
    /// </summary>
    public void AnimateHeal()
    {
        StopCurrentAnimation();
        _currentAnimation = StartCoroutine(HealRoutine());
    }

    /// <summary>
    /// Esconde o coração (para reposicionamento/cleanup).
    /// </summary>
    public void Hide()
    {
        StopCurrentAnimation();
        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = Vector3.zero;
    }

    // --- ROTINAS DE ANIMAÇÃO ---

    private IEnumerator SpawnRoutine()
    {
        _isFull = true;
        _image.color = _fullColor;
        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < _spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _spawnDuration;
            float curveValue = _popCurve.Evaluate(t);

            _canvasGroup.alpha = t;
            _rectTransform.localScale = Vector3.one * curveValue;

            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _rectTransform.localScale = Vector3.one;
        _currentAnimation = null;
    }

    private IEnumerator LoseRoutine()
    {
        // TODO FUTURO: Adicionar animação de "quebrar" (particles, shake)
        // Por enquanto: transição suave para cinza

        _isFull = false;

        float elapsed = 0f;
        Color startColor = _image.color;

        while (elapsed < _loseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _loseDuration;

            _image.color = Color.Lerp(startColor, _emptyColor, t);

            // Pequeno "pulse" negativo
            float scale = 1f - (Mathf.Sin(t * Mathf.PI) * 0.2f);
            _rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        _image.color = _emptyColor;
        _rectTransform.localScale = Vector3.one;
        _currentAnimation = null;
    }

    private IEnumerator HealRoutine()
    {
        _isFull = true;

        float elapsed = 0f;
        Color startColor = _image.color;

        while (elapsed < _healDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _healDuration;

            _image.color = Color.Lerp(startColor, _fullColor, t);

            // Bounce effect
            float bounceScale = 1f + (Mathf.Sin(t * Mathf.PI * 2) * 0.3f * (1f - t));
            _rectTransform.localScale = Vector3.one * bounceScale;

            yield return null;
        }

        _image.color = _fullColor;
        _rectTransform.localScale = Vector3.one;
        _currentAnimation = null;
    }

    private void StopCurrentAnimation()
    {
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }
    }

    private void OnDestroy()
    {
        StopCurrentAnimation();
    }
}
