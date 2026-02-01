using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;
using LastFurrow.Infrastructure.Visual;


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
    private PlayerInteraction _cachedInteraction;

    private Animator _cursorAnimator;
    private VisualElevationProcessor _elevationProcessor = new();
    private Vector3 _originalLocalPos;
    private Transform _visualsRoot;

    public int SlotIndex => _index;
    public int InteractionPriority => 0;
    public event Action<int, CardView> OnDropInteraction;

    private void Awake()
    {
        // CRÍTICO: Garante que todos os renderers existem ANTES de qualquer uso
        ConfigureRenderers();

        // Configuração inicial do passive score
        if (_passiveScoreGroup != null) _passiveScoreGroup.alpha = 0f;
    }

    private void Update()
    {
        if (_context?.VisualConfig == null) return;

        float maxOffset = _context.VisualConfig.patternElevationOffset;
        if (maxOffset <= 0) maxOffset = 0.3f; 

        float speed = _context.VisualConfig.elevationSpeed;
        if (speed <= 0) speed = 10f;

        _elevationProcessor.Update(maxOffset, speed, Time.deltaTime);

        if (_visualsRoot != null)
            _visualsRoot.localPosition = _elevationProcessor.Apply(_originalLocalPos);
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

        _cachedInteraction = FindFirstObjectByType<PlayerInteraction>();

        ResetVisualState();
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
        if (_cachedInteraction != null && _cachedInteraction.DragSystem.IsDragging)
        {
            if (CanReceive(_cachedInteraction.DragSystem.ActiveDrag))
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
        // Define pattern como branco/falso, efetivamente limpando
        _highlightController?.SetPattern(Color.white, false);
    }
    public void TriggerAnalyzingPulse(Color pulseColor, float duration)
    {
        // Usa o método PlayScannerPulse que já gerencia o estado analyzing internamente
        _highlightController?.PlayScannerPulse(duration, this.GetCancellationTokenOnDestroy()).Forget();
    }
    public void TriggerWhiteFlash()
    {
        _highlightController?.PlayWhiteFlash(this.GetCancellationTokenOnDestroy()).Forget();
    }
    public void SetElevationFactor(float factor)
    {
        _elevationProcessor.SetElevationFactor(factor);
    }
    public void ShowPassiveScore(int points)
    {
        if (_passiveScoreText == null || _passiveScoreGroup == null) return;

        // Trigger Levitation
        SetElevationFactor(1f);
        
        // Trigger White Flash FX
        TriggerWhiteFlash();

        _scoreTween?.Kill();

        _passiveScoreText.text = $"+{points}";
        _passiveScoreGroup.alpha = 0f;

        Vector3 startPos = Vector3.zero;
        Vector3 targetPos = startPos + Vector3.up * 0.7f;
        _passiveScoreText.transform.localPosition = startPos;

        Sequence seq = DOTween.Sequence();
        seq.Append(_passiveScoreGroup.DOFade(1f, 0.2f));
        seq.Join(_passiveScoreText.transform.DOLocalMove(targetPos, 0.4f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.3f);
        seq.Append(_passiveScoreGroup.DOFade(0f, 0.3f));
        seq.Join(_passiveScoreText.transform.DOLocalMove(targetPos + Vector3.up * 0.2f, 0.3f));
        seq.OnComplete(() => _passiveScoreText.transform.localPosition = startPos);

        _scoreTween = seq;
    }

    // --- LÓGICA DE DROP ---

    public bool CanReceive(IDraggable draggable)
    {
        if (draggable is not CardView cardView) return false;
        bool canDrop = _context.DropValidator.CanDrop(_index, cardView.Data);

        if (!canDrop)
        {
            // Passamos o token do View para garantir link
            _highlightController?.PlayErrorFlash(this.GetCancellationTokenOnDestroy()).Forget();
        }

        return canDrop;
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            StopPulse();
            
            // Delay o punch para sincronizar com o slam da carta
            float punchDelay = _context.VisualConfig.slotReceivePunchDelay;
            DOVirtual.DelayedCall(punchDelay, () => TriggerReceivePop());

            OnHoverExit();
            OnDropInteraction?.Invoke(SlotIndex, cardView);
        }
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

    // --- VISUALIZAÇÃO DE DADOS (Mantida simples) ---

    public void SetVisualState(Sprite plantSprite, bool isWatered, bool isMature = false, bool isWithered = false)
    {
        if (_isLocked) return;

        // Atualiza sprite base (solo seco/molhado)
        Sprite soilSprite = isWatered ? _context.VisualConfig.wetSoilSprite : _context.VisualConfig.drySoilSprite;
        
        if (_baseRenderer != null)
        {
            _baseRenderer.sprite = soilSprite;
            _baseRenderer.color = Color.white;
            
            // Se o renderer estiver na raiz, ele não move. 
            // Sincronizamos o sprite com o Visuals_Root se tivermos um substituto.
            UpdateBaseRendererShadow();
        }

        // CRÍTICO: Sincroniza overlays com sprite da base para evitar problemas visuais
        SyncOverlaySprites();

        // Lógica da planta
        bool hasPlant = plantSprite != null;
        _plantRenderer.enabled = hasPlant;
        _plantRenderer.sprite = hasPlant ? plantSprite : null;

        // Overlay de estado (mature/withered/planted)
        if (_stateOverlayRenderer != null)
        {
            _stateOverlayRenderer.enabled = hasPlant;
            if (hasPlant)
            {
                if (isMature) 
                    _stateOverlayRenderer.color = _context.VisualConfig.matureOverlay;
                else if (isWithered) 
                    _stateOverlayRenderer.color = _context.VisualConfig.witheredOverlay;
                else 
                    _stateOverlayRenderer.color = _context.VisualConfig.plantedOverlay;
            }
        }
    }

    public void SetLockedState(bool isLocked)
    {
        _isLocked = isLocked;

        // Proteção caso Config não esteja carregada ainda
        if (_context?.VisualConfig == null) return;

        if (_isLocked)
        {
            // LÓGICA RESTAURADA: Usa o sprite da config se existir, senão usa tint cinza
            if (_context.VisualConfig.lockedSoilSprite != null)
            {
                _baseRenderer.sprite = _context.VisualConfig.lockedSoilSprite;
                _baseRenderer.color = Color.white; // Reseta cor para não misturar tint com sprite
            }
            else
            {
                _baseRenderer.color = Color.gray; // Fallback
            }

            _plantRenderer.enabled = false;

            // Importante: Bloqueado não deve ter highlight de hover
            _highlightController?.SetHover(false);
        }
        else
        {
            // Ao desbloquear, volta para terra seca (padrão)
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
        // Note: Outros renderers serão resolvidos/criados no SetupVisualHierarchy durante Initialize
    }

    private void SetupVisualHierarchy()
    {
        if (_visualsRoot != null) return;

        // Cria container para visuais (Root das animações)
        GameObject root = new GameObject("Visuals_Root");
        root.transform.SetParent(this.transform, false);
        _visualsRoot = root.transform;

        // Reparenta renderers existentes para o novo root
        // Isso garante que eles sigam as animações de escala/posição do root
        ReparentIfNotNull(_plantRenderer);
        ReparentIfNotNull(_stateOverlayRenderer);
        ReparentIfNotNull(_highlightOverlayRenderer);
        ReparentIfNotNull(_cursorRenderer);

        // Especial: Se a base estiver no objeto principal, precisamos de um proxy nela
        // para que o solo também levite/pulse.
        if (_baseRenderer != null && _baseRenderer.gameObject == this.gameObject)
        {
            var originalSprite = _baseRenderer.sprite;
            _baseRenderer.enabled = false; // Desliga a versão estática
            _baseRenderer = CreateChildSprite("BaseSoil_Proxy", 0);
            _baseRenderer.sprite = originalSprite;
            _baseRenderer.enabled = true;
        }

        // Se algum não existia, cria agora dentro do root
        if (_plantRenderer == null) _plantRenderer = CreateChildSprite("PlantSprite", 1);
        if (_stateOverlayRenderer == null) _stateOverlayRenderer = CreateChildSprite("StateOverlay", 2);
        if (_highlightOverlayRenderer == null) _highlightOverlayRenderer = CreateChildSprite("OverlayFill", 3);
        if (_cursorRenderer == null) _cursorRenderer = CreateChildSprite("CursorArrows", 4);
    }

    private void UpdateBaseRendererShadow()
    {
        // Se a base foi substituída por um proxy no Visuals_Root, garantimos que ela tenha o sprite certo
        // Esse método é redundante se _baseRenderer já aponta para o proxy, mas mantemos por segurança
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
        // Tenta encontrar por nome sob o root
        var child = _visualsRoot.Find(name);
        if (child != null) return child.GetComponent<SpriteRenderer>();

        // Cria novo
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

    /// <summary>
    /// Sincroniza o sprite dos overlays com o sprite da base.
    /// Isso garante que overlays de cor funcionem corretamente.
    /// </summary>
    private void SyncOverlaySprites()
    {
        if (_baseRenderer == null || _baseRenderer.sprite == null) return;

        // State overlay usa o mesmo sprite da base para aplicar cor (ex: mature/planted)
        if (_stateOverlayRenderer != null)
            _stateOverlayRenderer.sprite = _baseRenderer.sprite;

        // SENIOR FIX: O Highlight Overlay PRECISA do sprite para mostrar as cores de Glow/Pattern/Analysis.
        // Se ele não tiver um sprite específico (borda), usamos o sprite da base.
        if (_highlightOverlayRenderer != null && _highlightOverlayRenderer.sprite == null)
            _highlightOverlayRenderer.sprite = _baseRenderer.sprite;
    }
}