using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIView : MonoBehaviour
{
    [Header("Configuração Base")]
    [SerializeField] protected bool _startHidden = true;
    [SerializeField] protected float _fadeDuration = 0.3f;

    protected CanvasGroup _canvasGroup;
    public bool IsVisible { get; private set; }

    private Coroutine _fadeCoroutine;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_startHidden) HideImmediate();
        else ShowImmediate();
    }

    public virtual void Show()
    {
        if (IsVisible) return;
        IsVisible = true;

        SetInteractable(true);
        RunFade(1f);
    }

    public virtual void Hide()
    {
        if (!IsVisible) return;
        IsVisible = false;

        SetInteractable(false);
        RunFade(0f);
    }

    public void ShowImmediate()
    {
        IsVisible = true;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        _canvasGroup.alpha = 1f;
        SetInteractable(true);
    }

    public void HideImmediate()
    {
        IsVisible = false;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        _canvasGroup.alpha = 0f;
        SetInteractable(false);
    }

    private void SetInteractable(bool state)
    {
        _canvasGroup.interactable = state;
        _canvasGroup.blocksRaycasts = state;
    }

    private void RunFade(float targetAlpha)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = _canvasGroup.alpha;
        float time = 0;

        while (time < _fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / _fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = targetAlpha;
        _fadeCoroutine = null;
    }
}