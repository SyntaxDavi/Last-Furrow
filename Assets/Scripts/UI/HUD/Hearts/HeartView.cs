using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Visual de um coração individual que pode representar até 3 vidas.
/// Suporta tiers (ex: Coração Normal Vermelho vs Coração Extra Azul).
/// 
/// ESTADO 0:
/// Coração Normal (Vermelho) -> Sprite Quebrado (Visível).
/// Coração Extra (Azul)      -> Desaparece (Alpha 0).
/// </summary>
[RequireComponent(typeof(Image), typeof(CanvasGroup), typeof(RectTransform))]
public class HeartView : MonoBehaviour
{
    [Header("Base Heart Sprites (Red)")]
    [Tooltip("Sprite de vida 0 (Quebrado/Fodido)")]
    [SerializeField] private Sprite _spriteBroken;
    
    [Tooltip("Sprite de vida 1 (1/3)")]
    [SerializeField] private Sprite _spriteOneThird;
    
    [Tooltip("Sprite de vida 2 (2/3)")]
    [SerializeField] private Sprite _spriteTwoThirds;
    
    [Tooltip("Sprite de vida 3 (Full)")]
    [SerializeField] private Sprite _spriteFull;

    [Header("Extra Heart Sprites (Blue)")]
    [Tooltip("Sprite do coração extra (Vida 4)")]
    [SerializeField] private Sprite _spriteExtraFull;

    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 0.25f;
    [SerializeField] private AnimationCurve _popCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Image _image;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    private int _currentFill = -1;
    private bool _isExtra = false;
    private Coroutine _currentAnimation;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        
        // Inicializa com alpha 0 para o primeiro nascimento
        if (_currentFill == -1) _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Define o estado visual do coração.
    /// </summary>
    public void SetState(int fill, bool isExtra, bool immediate = false)
    {
        fill = Mathf.Clamp(fill, 0, 3);
        
        // Só atualiza se algo mudou visualmente
        if (fill == _currentFill && isExtra == _isExtra && _canvasGroup.alpha > 0) return;

        _currentFill = fill;
        _isExtra = isExtra;

        Sprite targetSprite = GetTargetSprite();
        if (targetSprite != null) _image.sprite = targetSprite;

        // Regra de Visibilidade:
        // Coração Normal (Vermelho): Sempre visível (fica com sprite de quebrado se fill=0)
        // Coração Extra (Azul): Só visível se tiver vida (fill > 0)
        float targetAlpha = (!_isExtra || _currentFill > 0) ? 1f : 0f;

        if (immediate)
        {
            StopCurrentAnimation();
            _canvasGroup.alpha = targetAlpha;
            _rectTransform.localScale = Vector3.one;
        }
        else
        {
            AnimatePop(targetAlpha);
        }
    }

    private Sprite GetTargetSprite()
    {
        if (_isExtra)
        {
            // Para o coração azul (Extra), só usamos o sprite cheio ou vazio
            return _currentFill > 0 ? _spriteExtraFull : null;
        }

        // Para o coração vermelho (Base)
        return _currentFill switch
        {
            0 => _spriteBroken,
            1 => _spriteOneThird,
            2 => _spriteTwoThirds,
            3 => _spriteFull,
            _ => _spriteFull
        };
    }

    private void AnimatePop(float targetAlpha)
    {
        StopCurrentAnimation();
        _currentAnimation = StartCoroutine(PopRoutine(targetAlpha));
    }

    private IEnumerator PopRoutine(float targetAlpha)
    {
        float elapsed = 0f;
        float startAlpha = _canvasGroup.alpha;

        while (elapsed < _animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _animationDuration;
            
            float curveValue = _popCurve.Evaluate(t);
            _rectTransform.localScale = Vector3.one * curveValue;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        _rectTransform.localScale = Vector3.one;
        _canvasGroup.alpha = targetAlpha;
        _currentAnimation = null;
    }

    public void Hide()
    {
        StopCurrentAnimation();
        _canvasGroup.alpha = 0f;
        _currentFill = -1;
    }

    private void StopCurrentAnimation()
    {
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }
    }

    private void OnDestroy() => StopCurrentAnimation();
}
