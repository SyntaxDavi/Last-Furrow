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

    private Animator _cursorAnimator;
    private VisualElevationProcessor _elevationProcessor = new();
    private Vector3 _originalLocalPos;

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

        // Fall-safe: Se a config estiver com 0 (comum após adicionar campos via script), usamos um default
        float maxOffset = _context.VisualConfig.patternElevationOffset;
        if (maxOffset <= 0) maxOffset = 0.3f; 

        float speed = _context.VisualConfig.elevationSpeed;
        if (speed <= 0) speed = 10f;

        _elevationProcessor.Update(maxOffset, speed, Time.deltaTime);

        transform.localPosition = _elevationProcessor.Apply(_originalLocalPos);
    }

    public void Initialize(GridVisualContext context, int index)
    {
        _context = context;
        _index = index;
        _isLocked = false;
        _originalLocalPos = transform.localPosition;

        // 1. Configura Overlay
        // SÓ atribui o sprite da base se o overlay estiver sem sprite (permitindo bordas customizadas no prefab)
        if (_highlightOverlayRenderer != null && _highlightOverlayRenderer.sprite == null && _baseRenderer != null)
        {
            _highlightOverlayRenderer.sprite = _baseRenderer.sprite;
        }

        // 2. Configura o Cursor Animado (Setup Automático)
        if (_cursorRenderer != null && _context.VisualConfig.cursorAnimatorController != null)
        {
            // Garante que existe um Animator no mesmo objeto do SpriteRenderer
            if (_cursorAnimator == null)
                _cursorAnimator = _cursorRenderer.gameObject.GetComponent<Animator>();

            if (_cursorAnimator == null)
                _cursorAnimator = _cursorRenderer.gameObject.AddComponent<Animator>();

            // Atribui o controller de animação (Aseprite clip logic)
            _cursorAnimator.runtimeAnimatorController = _context.VisualConfig.cursorAnimatorController;
        }

        // 3. Inicializa Controller passando o Animator
        _highlightController = new SlotHighlightController(
            _highlightOverlayRenderer,
            _cursorRenderer,
            _cursorAnimator, 
            _context.VisualConfig
        );

        SyncOverlaySprites();
        ResetVisualState();
    }

    private void OnDestroy()
    {
        _highlightController?.KillAll();
        _scoreTween?.Kill();
    }

    // --- API PÚBLICA DE HIGHLIGHT (Delegada) ---

    public void OnHoverEnter() => _highlightController?.SetHover(true);
    public void OnHoverExit() => _highlightController?.SetHover(false);
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
            OnHoverExit();
            OnDropInteraction?.Invoke(SlotIndex, cardView);
        }
    }

    // --- VISUALIZAÇÃO DE DADOS (Mantida simples) ---

    public void SetVisualState(Sprite plantSprite, bool isWatered, bool isMature = false, bool isWithered = false)
    {
        if (_isLocked) return;

        // Atualiza sprite base (solo seco/molhado)
        _baseRenderer.sprite = isWatered ? _context.VisualConfig.wetSoilSprite : _context.VisualConfig.drySoilSprite;
        _baseRenderer.color = Color.white;

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
        if (_plantRenderer == null) _plantRenderer = CreateChildSprite("PlantSprite", 1);
        if (_stateOverlayRenderer == null) _stateOverlayRenderer = CreateChildSprite("StateOverlay", 2);

        // LAYER 3: Overlay de Cor (Verde/Amarelo/Vermelho)
        if (_highlightOverlayRenderer == null)
            _highlightOverlayRenderer = CreateChildSprite("OverlayFill", 3);

        // LAYER 4: Cursor (Setas brancas em cima de tudo)
        if (_cursorRenderer == null)
            _cursorRenderer = CreateChildSprite("CursorArrows", 4);
    }


    private SpriteRenderer CreateChildSprite(string name, int orderOffset)
    {
        var child = transform.Find(name);
        if (child != null) return child.GetComponent<SpriteRenderer>();

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(this.transform, false);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingLayerID = _baseRenderer.sortingLayerID;
        sr.sortingOrder = _baseRenderer.sortingOrder + orderOffset;
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

        // State overlay usa o mesmo sprite da base para aplicar cor
        if (_stateOverlayRenderer != null)
            _stateOverlayRenderer.sprite = _baseRenderer.sprite;

        // Highlight pode usar sprite específico (borda) ou o mesmo da base
        // Como configuramos no Initialize, não precisamos sobrescrever aqui
    }
}