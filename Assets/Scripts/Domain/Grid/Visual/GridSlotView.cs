using System;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TMPro;
using LastFurrow.Infrastructure.Visual;

/// <summary>
/// View passiva para slots do grid.
/// 
/// RESPONSABILIDADES:
/// - Receber estado visual e renderizar
/// - Disparar eventos de interação
/// - NÃO toma decisões de lógica
/// 
/// REFATORAÇÕES APLICADAS:
/// - Fase 1: Injeção de IDragStateProvider (sem FindFirstObjectByType)
/// - Fase 2: CanReceive é puro (sem side-effects)
/// - Fase 3: Update sob demanda (flag _isElevationActive)
/// - Fase 4: SetVisualState decomposição com SlotVisualState
/// - Fase 5: SetLockedState com ILockVisualStrategy
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class GridSlotView : MonoBehaviour, IInteractable, IDropTarget
{
    [Header("Componentes Visuais")]
    [SerializeField] private SpriteRenderer _baseRenderer;
    [SerializeField] private SpriteRenderer _plantRenderer;
    [SerializeField] private SpriteRenderer _stateOverlayRenderer;
    [SerializeField] private SpriteRenderer _highlightOverlayRenderer; 
    [SerializeField] private SpriteRenderer _cursorRenderer;           

    [Header("UI Feedback")]
    [SerializeField] private TextMeshPro _passiveScoreText;
    [SerializeField] private CanvasGroup _passiveScoreGroup;

    private GridVisualContext _context;
    private int _index;
    private bool _isLocked;

    private SlotHighlightController _highlightController;
    private Tween _scoreTween;
    private Tween _pulseTween;

    private Animator _cursorAnimator;
    private VisualElevationProcessor _elevationProcessor = new();
    private Vector3 _originalLocalPos;
    private Transform _visualsRoot;

    // Fase 3: Flag para Update sob demanda
    private bool _isElevationActive = false;

    public int SlotIndex => _index;
    public int InteractionPriority => 0;
    public event Action<int, CardView> OnDropInteraction;

    // Fase 1: Propriedade para acesso ao DragStateProvider
    private IDragStateProvider DragProvider => _context?.DragStateProvider;

    private void Awake()
    {
        // CRÍTICO: Garante que todos os renderers existem ANTES de qualquer uso
        ConfigureRenderers();

        // FORÇA estado inicial invisível do score popup
        if (_passiveScoreGroup != null) 
        {
            _passiveScoreGroup.alpha = 0f;
        }
        if (_passiveScoreText != null)
        {
            _passiveScoreText.alpha = 0f; // TMP tem alpha próprio além do CanvasGroup
        }
    }

    private void Update()
    {
        // Fase 3: Early exit se elevação não está ativa
        if (!_isElevationActive) return;
        
        if (_context?.VisualConfig == null) return;

        float maxOffset = _context.VisualConfig.patternElevationOffset;
        if (maxOffset <= 0) maxOffset = 0.3f; 

        float speed = _context.VisualConfig.elevationSpeed;
        if (speed <= 0) speed = 10f;

        _elevationProcessor.Update(maxOffset, speed, Time.deltaTime);

        if (_visualsRoot != null)
            _visualsRoot.localPosition = _elevationProcessor.Apply(_originalLocalPos);

        // Fase 3: Auto-desativa quando estabiliza
        if (_elevationProcessor.IsStable)
        {
            _isElevationActive = false;
        }
    }

    public void Initialize(GridVisualContext context, int index)
    {
        _context = context;
        _index = index;
        _isLocked = false;

        // Setup hierarquia visual se ainda não existir
        SetupVisualHierarchy();

        _originalLocalPos = Vector3.zero; // Local em relação ao pai (Slot Root)

        // 1. Configura Overlay
        SyncOverlaySprites();

        // 2. Configura o Cursor Animado (Setup Automático)
        if (_cursorRenderer != null && _context.VisualConfig.cursorAnimatorController != null)
        {
            if (_cursorAnimator == null)
                _cursorAnimator = _cursorRenderer.gameObject.GetComponent<Animator>();

            if (_cursorAnimator == null)
                _cursorAnimator = _cursorRenderer.gameObject.AddComponent<Animator>();

            _cursorAnimator.runtimeAnimatorController = _context.VisualConfig.cursorAnimatorController;
        }

        _highlightController = new SlotHighlightController(
            _highlightOverlayRenderer,
            _cursorRenderer,
            _cursorAnimator, 
            _context.VisualConfig
        );  

        ResetVisualState();
        ResetPassiveScore();
    }

    private void OnDestroy()
    {
        _highlightController?.KillAll();
        _scoreTween?.Kill();
    }

    // --- API PÚBLICA DE HIGHLIGHT (Delegada) ---

    public void OnHoverEnter() 
    {
        _highlightController?.SetHover(true);

        // Grid Juice: Pulse if holding a valid card
        // Fase 1: Usa DragProvider injetado ao invés de cache
        if (DragProvider != null && DragProvider.IsDragging)
        {
            if (CanReceive(DragProvider.ActiveDrag))
            {
                StartPulse();
            }
        }
    }

    public void OnHoverExit() 
    {
        _highlightController?.SetHover(false);
        StopPulse();
    }
    
    public void OnClick() { }

    public void SetPatternHighlight(Color color, bool enable = true)
    {
        _highlightController?.SetPattern(color, enable);
    }
    
    public void ClearPatternHighlight()
    {
        _highlightController?.SetPattern(Color.white, false);
        ResetPassiveScore();
        SetElevationFactor(0f);
    }
    
    public void TriggerAnalyzingPulse(Color pulseColor, float duration)
    {
        ResetPassiveScore();
        _highlightController?.PlayScannerPulse(duration, this.GetCancellationTokenOnDestroy()).Forget();
    }
    
    public void TriggerWhiteFlash()
    {
        _highlightController?.PlayWhiteFlash(this.GetCancellationTokenOnDestroy()).Forget();
    }
    
    public void SetElevationFactor(float factor)
    {
        _elevationProcessor.SetElevationFactor(factor);
        // Fase 3: Ativa Update apenas quando há movimento
        _isElevationActive = factor > 0.01f || !_elevationProcessor.IsStable;
    }
    
    public void ShowPassiveScore(int points)
    {
        if (_passiveScoreText == null || _passiveScoreGroup == null || _context?.VisualConfig == null) return;

        var cfg = _context.VisualConfig;

        // Trigger Levitation
        SetElevationFactor(1f);
        
        // Trigger White Flash FX
        TriggerWhiteFlash();

        _scoreTween?.Kill();

        _passiveScoreText.text = $"+{points}";
        _passiveScoreText.alpha = 1f; 
        _passiveScoreGroup.alpha = 0f;

        Vector3 startPos = Vector3.zero;
        Vector3 targetPos = startPos + Vector3.up * cfg.scorePopupHeight;
        _passiveScoreText.transform.localPosition = startPos;

        Sequence seq = DOTween.Sequence();
        seq.Append(_passiveScoreGroup.DOFade(1f, cfg.scorePopupFadeInDuration));
        seq.Join(_passiveScoreText.transform.DOLocalMove(targetPos, cfg.scorePopupMoveDuration).SetEase(Ease.OutBack));
        seq.AppendInterval(cfg.scorePopupWaitDuration);
        seq.Append(_passiveScoreGroup.DOFade(0f, cfg.scorePopupFadeOutDuration));
        seq.Join(_passiveScoreText.transform.DOLocalMove(targetPos + Vector3.up * cfg.scorePopupDriftHeight, cfg.scorePopupFadeOutDuration));
        seq.OnComplete(() => {
            _passiveScoreText.transform.localPosition = startPos;
            _passiveScoreText.alpha = 0f; 
            SetElevationFactor(0f);
        });

        _scoreTween = seq;
    }

    private void ResetPassiveScore()
    {
        _scoreTween?.Kill();
        if (_passiveScoreGroup != null) _passiveScoreGroup.alpha = 0f;
        if (_passiveScoreText != null) 
        {
            _passiveScoreText.transform.localPosition = Vector3.zero;
            _passiveScoreText.alpha = 0f; // Garante que TMP está invisível
        }
    }

    // --- LÓGICA DE DROP ---

    /// <summary>
    /// Fase 2: Query pura - retorna bool sem side-effects.
    /// Feedback visual deve ser chamado separadamente via PlayInvalidDropFeedback().
    /// </summary>
    public bool CanReceive(IDraggable draggable)
    {
        if (draggable is not CardView cardView) return false;
        return _context.DropValidator.CanDrop(_index, cardView.Data);
    }

    /// <summary>
    /// Fase 2: Command separado - dispara feedback visual de drop inválido.
    /// Chamado pelo sistema de drag quando drop falha.
    /// </summary>
    public void PlayInvalidDropFeedback()
    {
        _highlightController?.PlayErrorFlash(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            StopPulse();
            OnHoverExit();
            OnDropInteraction?.Invoke(SlotIndex, cardView);
        }
    }

    /// <summary>
    /// Dispara o efeito visual de feedback (pop/punch) ao receber uma carta.
    /// Chamado externamente (ex: GridManager) após confirmação de sucesso da ação.
    /// </summary>
    public void PlayReceiveJuice()
    {
        if (_context?.VisualConfig == null) return;

        float punchDelay = _context.VisualConfig.slotReceivePunchDelay;
        DOVirtual.DelayedCall(punchDelay, () => TriggerReceivePop());
    }

    private void StartPulse()
    {
        if (_pulseTween != null || _visualsRoot == null) return;

        float targetScale = 1.0f * _context.VisualConfig.slotHoverPulseScale;
        _pulseTween = _visualsRoot.DOScale(targetScale, _context.VisualConfig.slotHoverPulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StopPulse()
    {
        if (_pulseTween != null)
        {
            _pulseTween.Kill();
            _pulseTween = null;
            _visualsRoot.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void TriggerReceivePop()
    {
        if (_visualsRoot == null) return;
        _visualsRoot.DOPunchScale(Vector3.one * _context.VisualConfig.slotReceivePunchAmount, 0.3f, 10, 1f);
    }

    // --- VISUALIZAÇÃO DE DADOS ---

    /// <summary>
    /// Fase 4: Overload legado mantido para compatibilidade.
    /// Delega para o novo método com SlotVisualState.
    /// </summary>
    public void SetVisualState(Sprite plantSprite, bool isWatered, bool isMature = false, bool isWithered = false)
    {
        var state = SlotVisualState.FromLegacy(plantSprite, isWatered, isMature, isWithered);
        SetVisualState(state);
    }

    /// <summary>
    /// Fase 4: Novo método com Value Object.
    /// Decomposto em métodos menores e focados.
    /// </summary>
    public void SetVisualState(SlotVisualState state)
    {
        if (_isLocked) return;

        UpdateSoilVisual(state.IsWatered);
        UpdatePlantVisual(state.PlantSprite, state.Maturity);
    }

    private void UpdateSoilVisual(bool isWatered)
    {
        var sprite = isWatered ? _context.VisualConfig.wetSoilSprite : _context.VisualConfig.drySoilSprite;
        
        if (_baseRenderer != null)
        {
            _baseRenderer.sprite = sprite;
            _baseRenderer.color = Color.white;
            UpdateBaseRendererShadow();
        }

        SyncOverlaySprites();
    }

    private void UpdatePlantVisual(Sprite sprite, PlantMaturity maturity)
    {
        bool hasPlant = sprite != null;
        _plantRenderer.enabled = hasPlant;
        _plantRenderer.sprite = sprite;

        UpdateMaturityOverlay(hasPlant, maturity);
    }

    private void UpdateMaturityOverlay(bool hasPlant, PlantMaturity maturity)
    {
        if (_stateOverlayRenderer == null) return;

        _stateOverlayRenderer.enabled = hasPlant;
        if (!hasPlant) return;

        _stateOverlayRenderer.color = maturity switch
        {
            PlantMaturity.Mature => _context.VisualConfig.matureOverlay,
            PlantMaturity.Withered => _context.VisualConfig.witheredOverlay,
            _ => _context.VisualConfig.plantedOverlay
        };
    }

    /// <summary>
    /// Fase 5: Overload legado mantido para compatibilidade.
    /// Usa DefaultLockVisualStrategy.
    /// </summary>
    public void SetLockedState(bool isLocked)
    {
        SetLockedState(isLocked, new DefaultLockVisualStrategy());
    }

    /// <summary>
    /// Fase 5: Novo método com estratégia injetável.
    /// View apenas aplica a "skin", regra vem da estratégia.
    /// </summary>
    public void SetLockedState(bool isLocked, ILockVisualStrategy strategy)
    {
        _isLocked = isLocked;

        if (_context?.VisualConfig == null) return;

        if (_isLocked)
        {
            _baseRenderer.sprite = strategy.GetLockedSprite(_context.VisualConfig);
            _baseRenderer.color = strategy.GetLockedTint(_context.VisualConfig);
            
            if (strategy.ShouldHidePlant) 
                _plantRenderer.enabled = false;

            _highlightController?.SetHover(false);
        }
        else
        {
            _baseRenderer.sprite = _context.VisualConfig.drySoilSprite;
            _baseRenderer.color = Color.white;
        }
    }

    // --- SETUP VISUAL ---

    private void ResetVisualState()
    {
        _plantRenderer.enabled = false;
        _highlightController?.SetHover(false);
        _highlightController?.SetPattern(Color.white, false);
    }

    private void ConfigureRenderers()
    {
        if (_baseRenderer == null) _baseRenderer = GetComponent<SpriteRenderer>();
    }

    private void SetupVisualHierarchy()
    {
        if (_visualsRoot != null) return;

        GameObject root = new GameObject("Visuals_Root");
        root.transform.SetParent(this.transform, false);
        _visualsRoot = root.transform;

        ReparentIfNotNull(_plantRenderer);
        ReparentIfNotNull(_stateOverlayRenderer);
        ReparentIfNotNull(_highlightOverlayRenderer);
        ReparentIfNotNull(_cursorRenderer);

        if (_baseRenderer != null && _baseRenderer.gameObject == this.gameObject)
        {
            var originalSprite = _baseRenderer.sprite;
            _baseRenderer.enabled = false;
            _baseRenderer = CreateChildSprite("BaseSoil_Proxy", 0);
            _baseRenderer.sprite = originalSprite;
            _baseRenderer.enabled = true;
        }

        if (_plantRenderer == null) _plantRenderer = CreateChildSprite("PlantSprite", 1);
        if (_stateOverlayRenderer == null) _stateOverlayRenderer = CreateChildSprite("StateOverlay", 2);
        if (_highlightOverlayRenderer == null) _highlightOverlayRenderer = CreateChildSprite("OverlayFill", 3);
        if (_cursorRenderer == null) _cursorRenderer = CreateChildSprite("CursorArrows", 4);
    }

    private void UpdateBaseRendererShadow()
    {
        // Placeholder para sincronização de proxy se necessário
    }

    private void ReparentIfNotNull(Component comp)
    {
        if (comp != null && comp.transform.parent != _visualsRoot)
        {
            comp.transform.SetParent(_visualsRoot, false);
        }
    }

    private SpriteRenderer CreateChildSprite(string name, int orderOffset)
    {
        var child = _visualsRoot.Find(name);
        if (child != null) return child.GetComponent<SpriteRenderer>();

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(_visualsRoot, false);
        var sr = obj.AddComponent<SpriteRenderer>();
        
        if (_baseRenderer != null)
        {
            sr.sortingLayerID = _baseRenderer.sortingLayerID;
            sr.sortingOrder = _baseRenderer.sortingOrder + orderOffset;
        }
        
        sr.enabled = false;
        return sr;
    }

    private void SyncOverlaySprites()
    {
        if (_baseRenderer == null || _baseRenderer.sprite == null) return;

        if (_stateOverlayRenderer != null)
            _stateOverlayRenderer.sprite = _baseRenderer.sprite;

        if (_highlightOverlayRenderer != null && _highlightOverlayRenderer.sprite == null)
            _highlightOverlayRenderer.sprite = _baseRenderer.sprite;
    }
}