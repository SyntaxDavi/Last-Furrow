using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Controlador DEDICADO ao highlight.
/// Evolução: Separação clara entre FX Transitórios (Error) e Estados Contínuos (Analyzing, Hover, Pattern).
/// </summary>
[System.Serializable]
public class SlotHighlightController
{
    private readonly SpriteRenderer _overlay;
    private readonly SpriteRenderer _cursorRenderer;
    private readonly Animator _cursorAnimator; 
    private readonly GridVisualConfig _config;

    private bool _isLockedByTransient;
    private bool _isAnalyzing;
    private bool _isHovered;
    private bool _hasPattern;
    private Color _patternColor;
    private Tween _transientTween;
    private Tween _stateTween;

    public SlotHighlightController(SpriteRenderer overlay, SpriteRenderer cursorRenderer, Animator cursorAnimator, GridVisualConfig config)
    {
        _overlay = overlay;
        _cursorRenderer = cursorRenderer;
        _cursorAnimator = cursorAnimator; 
        _config = config;

        _overlay.enabled = false;
        _cursorRenderer.enabled = false;

        // Garante que o animator comece desligado
        if (_cursorAnimator != null) _cursorAnimator.enabled = false;
    }

    // =================================================================================
    // CANAL 1: FX TRANSITÓRIOS (Crítico / Bloqueante)
    // =================================================================================

    /// <summary>
    /// Toca um flash de erro. Bloqueia visualmente outros estados enquanto roda.
    /// </summary>
    public async UniTask PlayErrorFlash(CancellationToken externalToken)
    {
        // 1. Tomada de Controle
        KillTransientTween(); // Mata apenas FX anteriores, não o estado base
        _isLockedByTransient = true;

        // Setup visual imediato
        _overlay.enabled = true;
        _overlay.color = _config.errorFlash;
        SetOverlayAlpha(0f);
        _cursorRenderer.enabled = false;

        // 2. Link de Cancelamento (Robustez Sênior)
        // Cria um token que cancela se o objeto for destruído OU se quem chamou cancelar
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalToken, _overlay.GetCancellationTokenOnDestroy());

        try
        {
            _transientTween = _overlay.DOFade(0.8f, 0.1f).SetLink(_overlay.gameObject);
            await _transientTween.ToUniTask(cancellationToken: linkedCts.Token);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: linkedCts.Token);
            _transientTween = _overlay.DOFade(0f, 0.2f).SetLink(_overlay.gameObject);
            await _transientTween.ToUniTask(cancellationToken: linkedCts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isLockedByTransient = false;
            _transientTween = null;
            RefreshVisuals();
        }
    }

    // =================================================================================
    // CANAL 2: ESTADO ANALYZING (Contínuo, Alta Prioridade)
    // =================================================================================

    public async UniTaskVoid PlayScannerPulse(float duration, CancellationToken token)
    {
        if (_isAnalyzing || _isLockedByTransient) return;

        _isAnalyzing = true;

        // Limpa estado visual anterior
        KillStateTween();

        // 1. Configura Overlay (AMARELO)
        _overlay.enabled = true;
        _overlay.color = _config.analyzingPulse; // <--- Certifique-se que essa cor é AMARELA no Inspector!
        SetOverlayAlpha(0f);

        // 2. Configura Cursor (Opcional: quer setas durante o analyze? Vou deixar false por padrão)
        _cursorRenderer.enabled = false;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            token, _overlay.GetCancellationTokenOnDestroy());

        try
        {
            // O Pulse amarelo
            _stateTween = _overlay.DOFade(0.7f, duration / 2f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(_overlay.gameObject);

            await _stateTween.ToUniTask(cancellationToken: linkedCts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isAnalyzing = false;
            _stateTween = null;
            RefreshVisuals();
        }
    }

    // =================================================================================
    // CANAL 3 & 4: INTERAÇÃO E INFO (Baixa Prioridade)
    // =================================================================================

    public void SetHover(bool state)
    {
        _isHovered = state;
        RefreshVisuals();
    }

    public void SetPattern(Color color, bool active)
    {
        _hasPattern = active;
        _patternColor = color;
        RefreshVisuals();
    }

    // =================================================================================
    // NÚCLEO DE RESOLUÇÃO (PRIORIDADE)
    // =================================================================================

    /// <summary>
    /// A Única Fonte da Verdade™ para a cor final.
    /// Chamado sempre que qualquer estado muda.
    /// </summary>
    private void RefreshVisuals()
    {
        if (_isLockedByTransient) return;

        // --- ESTADO 1: ANALYZING (Prioridade sobre Hover) ---
        if (_isAnalyzing)
        {
            _overlay.enabled = true;
            // Mantém cor atual do tween ou reseta
            Color c = _config.analyzingPulse;
            c.a = _overlay.color.a;
            _overlay.color = c;

            ToggleCursor(false); 
            return;
        }

        KillStateTween(); // Limpa pulse se sobrou

        // --- HOVER (Verde + Setas) ---
        if (_isHovered)
        {
            // Camada 1: Overlay (Fundo)
            _overlay.enabled = true;
            _overlay.color = _config.validHover;
            SetOverlayAlpha(_config.validHover.a);

            // Camada 2: Cursor Animado
            ToggleCursor(true);
            return;
        }

        // --- ESTADO 3: PATTERN ---
        if (_hasPattern)
        {
            _overlay.enabled = true;
            _overlay.color = _patternColor;
            SetOverlayAlpha(_patternColor.a);

            ToggleCursor(false);
            return;
        }

        // --- IDLE ---
        _overlay.enabled = false;
        ToggleCursor(false);
    }
    private void ToggleCursor(bool enable)
    {
        if (_cursorRenderer == null) return;

        _cursorRenderer.enabled = enable;

        if (_cursorAnimator != null)
        {
            _cursorAnimator.enabled = enable;

            if (enable)
            {
                // APLICA O AJUSTE DE POSIÇÃO (OFFSET) AQUI
                // Como _cursorRenderer é filho do slot, usamos localPosition
                _cursorRenderer.transform.localPosition = _config.cursorLocalOffset;
                _cursorRenderer.transform.localScale = _config.cursorLocalScale;

                // Se a animação tiver travado num frame estranho, isso reseta para o começo
                _cursorAnimator.Rebind();
                _cursorAnimator.Update(0f);
            }
        }
    }

    // =================================================================================
    // HELPERS & CLEANUP
    // =================================================================================

    public void KillAll()
    {
        KillTransientTween();
        KillStateTween();
    }

    private void KillTransientTween() { if (_transientTween != null && _transientTween.IsActive()) _transientTween.Kill(); _transientTween = null; }
    private void KillStateTween() { if (_stateTween != null && _stateTween.IsActive()) _stateTween.Kill(); _stateTween = null; }
    private void SetOverlayAlpha(float alpha) { Color c = _overlay.color; c.a = alpha; _overlay.color = c; } 
}