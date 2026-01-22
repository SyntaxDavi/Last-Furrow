using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;


[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class GridSlotView : MonoBehaviour, IInteractable, IDropTarget
{
    [Header("Componentes Visuais")]
    [SerializeField] private SpriteRenderer _baseRenderer;
    [SerializeField] private SpriteRenderer _plantRenderer;
    [SerializeField] private SpriteRenderer _stateOverlayRenderer;
    [SerializeField] private SpriteRenderer _highlightRenderer;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshPro _passiveScoreText;
    [SerializeField] private CanvasGroup _passiveScoreGroup;

    private GridVisualContext _context;
    private int _index;
    private bool _isLocked;

    private SlotHighlightController _highlightController;
    private Tween _scoreTween;

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

    public void Initialize(GridVisualContext context, int index)
    {
        _context = context;
        _index = index;
        _isLocked = false;

        // CRÍTICO: Garante que highlight tem sprite base configurado
        if (_highlightRenderer != null && _context.VisualConfig.selectionBorderSprite != null)
        {
            _highlightRenderer.sprite = _context.VisualConfig.selectionBorderSprite;
        }

        // Injeta dependências no controller
        _highlightController = new SlotHighlightController(_highlightRenderer, _context.VisualConfig);

        // Sincroniza sprites dos overlays com a base
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
        // Agora usamos o estado explícito "Analyzing"
        // O AnalyzingPhaseController geralmente chama isso no início da análise
        _highlightController?.SetAnalyzing(true);

        // Como o AnalyzingPhaseController não chama "StopAnalyzing" explicitamente no seu código antigo,
        // mantemos um failsafe aqui, mas o ideal é que o Controller externo gerencie start/stop.
        StopAnalyzeAfterDelay(duration).Forget();
    }
    private async UniTaskVoid StopAnalyzeAfterDelay(float duration)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: this.GetCancellationTokenOnDestroy());
        _highlightController?.SetAnalyzing(false);
    }
    public void ShowPassiveScore(int points)
    {
        if (_passiveScoreText == null || _passiveScoreGroup == null) return;

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
        if (_highlightRenderer == null) _highlightRenderer = CreateChildSprite("HighlightOverlay", 4);
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