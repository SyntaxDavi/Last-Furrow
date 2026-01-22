using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

/// <summary>
/// Controlador DEDICADO ao highlight.
/// Evolução: Separação clara entre FX Transitórios (Error) e Estados Contínuos (Analyzing, Hover, Pattern).
/// </summary>
[System.Serializable]
public class SlotHighlightController
{
    private readonly SpriteRenderer _renderer;
    private readonly GridVisualConfig _config;

    // --- ESTADOS (Canais de Prioridade) ---
    private bool _isLockedByTransient; // Bloqueio total (ex: Erro)
    private bool _isAnalyzing;         // Estado de jogo (Fim do dia)
    private bool _isHovered;           // Interação do jogador
    private bool _hasPattern;          // Informação passiva

    private Color _patternColor;

    // --- TWEENS SEPARADOS (Evita "matança" acidental) ---
    private Tween _transientTween; // Para one-shots (Flash)
    private Tween _stateTween;     // Para loops (Pulse)

    public SlotHighlightController(SpriteRenderer renderer, GridVisualConfig config)
    {
        _renderer = renderer;
        _config = config;
        _renderer.enabled = false;
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
        _renderer.enabled = true;
        _renderer.color = _config.errorFlash;
        SetAlpha(0f);

        // 2. Link de Cancelamento (Robustez Sênior)
        // Cria um token que cancela se o objeto for destruído OU se quem chamou cancelar
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalToken,
            _renderer.GetCancellationTokenOnDestroy()
        );

        try
        {
            // Sequência manual com await para controle fino
            _transientTween = _renderer.DOFade(0.8f, 0.1f).SetLink(_renderer.gameObject);
            await _transientTween.ToUniTask(cancellationToken: linkedCts.Token);

            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: linkedCts.Token);

            _transientTween = _renderer.DOFade(0f, 0.2f).SetLink(_renderer.gameObject);
            await _transientTween.ToUniTask(cancellationToken: linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Se cancelado, não faz nada, o finally limpa.
        }
        finally
        {
            // 3. Devolução de Controle (Garantida)
            _isLockedByTransient = false;
            _transientTween = null;
            RefreshVisuals(); // Restaura o estado que estava por baixo (Analyzing/Hover/etc)
        }
    }

    // =================================================================================
    // CANAL 2: ESTADO ANALYZING (Contínuo, Alta Prioridade)
    // =================================================================================

    public void SetAnalyzing(bool isAnalyzing)
    {
        if (_isAnalyzing == isAnalyzing) return;

        _isAnalyzing = isAnalyzing;

        if (_isAnalyzing)
        {
            // Entrando no estado: Inicia o Pulse Loop
            KillStateTween(); // Limpa pulse anterior se houver

            // Configuração base (caso não tenha transient rodando por cima)
            if (!_isLockedByTransient)
            {
                _renderer.enabled = true;
                _renderer.color = _config.analyzingPulse;
                SetAlpha(0.3f);
            }

            // O tween roda independente do render estar visível ou não
            // Assim, se o ErrorFlash terminar, o Pulse já está rodando corretamente
            _stateTween = _renderer.DOFade(0.8f, _config.pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(_renderer.gameObject)
                .SetId("AnalyzingPulse"); // Facilitar debug
        }
        else
        {
            // Saindo do estado
            KillStateTween();
        }

        RefreshVisuals();
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
        // 1. Se estiver rodando um FX Crítico (Erro), ele manda.
        if (_isLockedByTransient)
        {
            // O tween transitório está controlando a cor/alpha. Não tocamos.
            return;
        }

        // 2. Analyzing (Pulse)
        if (_isAnalyzing)
        {
            _renderer.enabled = true;
            // Nota: Não setamos a cor aqui a cada frame pois o _stateTween está animando ela.
            // Mas garantimos que a base é a correta caso o tween tenha sido recém criado.
            // Apenas garantimos que o renderer está ligado.

            // Segurança: Se o tween morreu por algum motivo externo, recriamos? 
            // Por enquanto, assumimos que SetAnalyzing cuidou disso.

            // Importante: Se viemos de um ErrorFlash, a cor pode estar errada.
            // Restauramos a cor base do pulso, mantendo o alpha atual do tween se possível,
            // mas DOTween sobrescreve. Vamos forçar a cor base do pulso.
            Color c = _config.analyzingPulse;
            c.a = _renderer.color.a; // Mantém o alpha que o tween está manipulando
            _renderer.color = c;
            return;
        }

        // Limpeza de Estado Contínuo (se não é analyzing, mata o pulse)
        // Isso já foi feito no SetAnalyzing(false), mas reforçamos visualmente.

        // 3. Hover
        if (_isHovered)
        {
            _renderer.enabled = true;
            _renderer.color = _config.validHover;
            SetAlpha(_config.validHover.a); // Hover é estático (sem tween de loop)
            return;
        }

        // 4. Pattern
        if (_hasPattern)
        {
            _renderer.enabled = true;
            _renderer.color = _patternColor;
            SetAlpha(_patternColor.a);
            return;
        }

        // 5. Idle
        _renderer.enabled = false;
    }
    public async UniTaskVoid PlayScannerPulse(float duration, CancellationToken token)
    {
        // 1. Evita sobreposição se já estiver rodando (ex: scanner muito rápido)
        if (_isAnalyzing || _isLockedByTransient) return;

        // 2. Ativa o canal de prioridade (Analyzing > Hover > Pattern)
        _isAnalyzing = true;

        // Configura visual inicial
        KillStateTween(); // Garante limpeza
        _renderer.enabled = true;
        _renderer.color = _config.analyzingPulse;
        SetAlpha(0f); // Começa invisível

        // 3. Cria token linkado (Segurança: cancela se o slot for destruído)
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            token,
            _renderer.GetCancellationTokenOnDestroy()
        );

        try
        {
            // 4. Executa o Pulse (Fade In -> Fade Out)
            // Usamos Yoyo com 2 loops (Ida e Volta) = 1 pulso completo
            _stateTween = _renderer.DOFade(0.7f, duration / 2f) // Sobe até 0.7 alpha
                .SetLoops(2, LoopType.Yoyo)                     // Vai e volta
                .SetLink(_renderer.gameObject);

            await _stateTween.ToUniTask(cancellationToken: linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Se cancelado, silêncio. O finally limpa.
        }
        finally
        {
            // 5. AUTO-LIMPEZA
            // O pulso acabou, liberamos o estado.
            _isAnalyzing = false;
            _stateTween = null;
            RefreshVisuals(); // Volta para Pattern ou Hover se existirem
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

    private void KillTransientTween()
    {
        if (_transientTween != null && _transientTween.IsActive()) _transientTween.Kill();
        _transientTween = null;
    }

    private void KillStateTween()
    {
        if (_stateTween != null && _stateTween.IsActive()) _stateTween.Kill();
        _stateTween = null;
    }

    private void SetAlpha(float alpha)
    {
        Color c = _renderer.color;
        c.a = alpha;
        _renderer.color = c;
    }
}