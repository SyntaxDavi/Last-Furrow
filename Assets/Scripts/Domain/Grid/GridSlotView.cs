using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// View de slot individual do Grid - Componente visual refatorado.
/// 
/// RESPONSABILIDADE:
/// - Renderizar estado visual (sprites, cores)
/// - Aplicar priority layered rendering (5 layers)
/// - Hover feedback (highlight)
/// - Drop validation via IDropValidator
/// - Animações básicas (flash, pulse)
/// 
/// ARQUITETURA:
/// - Recebe GridVisualContext via Initialize()
/// - Usa IDropValidator para validação
/// - Não conhece AppCore.Instance
/// - Priority layers: Base ? Plant ? State ? GameState ? Hover
/// 
/// NÃO FAZ:
/// - Decidir regras de jogo (IDropValidator faz)
/// - Acessar GridService diretamente
/// - Spawnar/destruir a si mesmo (GridManager faz)
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class GridSlotView : MonoBehaviour, IInteractable, IDropTarget
{
    [Header("Componentes Visuais")]
    [SerializeField] private SpriteRenderer _baseRenderer;
    [SerializeField] private SpriteRenderer _plantRenderer;
    [SerializeField] private SpriteRenderer _stateOverlayRenderer;
    [SerializeField] private SpriteRenderer _gameStateOverlayRenderer;
    [SerializeField] private SpriteRenderer _highlightRenderer;
    
    [Header("Crop Passive Score")]
    [Tooltip("TextMeshPro para mostrar pontos passivos (+10)")]
    [SerializeField] private TMPro.TextMeshPro _passiveScoreText;
    [Tooltip("CanvasGroup para fade in/out do texto")]
    [SerializeField] private CanvasGroup _passiveScoreGroup;
    [Tooltip("Duração da animação de fade")]
    [SerializeField] private float _scoreFadeDuration = 0.8f;

    [Header("Interacao")]
    [SerializeField] private int _interactionPriority = 0;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    private GridVisualContext _context;
    private int _index;
    private bool _isLocked;
    private bool _isInitialized;
    
    public int SlotIndex => _index;
    public int InteractionPriority => _interactionPriority;
    public event Action<int, CardView> OnDropInteraction;
    
    private void Awake()
    {
        ConfigureRenderers();
        
        // Inicializar texto de pontos passivos como invisível
        if (_passiveScoreGroup != null)
        {
            _passiveScoreGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Inicializa com GridVisualContext (injeção de dependências).
    /// </summary>
    public void Initialize(GridVisualContext context, int index)
    {
        if (context == null)
        {
            Debug.LogError($"[GridSlotView {index}] Context null!");
            return;
        }

        _context = context;
        _index = index;
        _isLocked = false;
        _isInitialized = true;

        ResetVisualState();
        SubscribeToEvents();

      //  if (_showDebugLogs)
        //    Debug.Log($"[GridSlotView {_index}] Inicializado com contexto");
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (_context != null && _context.GameStateEvents != null)
        {
            _context.GameStateEvents.OnStateChanged += HandleGameStateChanged;
            
            // Aplica estado inicial
            HandleGameStateChanged(_context.GameStateManager.CurrentState);
        }

        if (_context != null && _context.GridEvents != null)
        {
            // Escuta evento de análise (final do dia)
            _context.GridEvents.OnAnalyzeSlot += HandleAnalyzeSlot;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (_context != null && _context.GameStateEvents != null)
        {
            _context.GameStateEvents.OnStateChanged -= HandleGameStateChanged;
        }

        if (_context != null && _context.GridEvents != null)
        {
            _context.GridEvents.OnAnalyzeSlot -= HandleAnalyzeSlot;
        }
    }

    private void ResetVisualState()
    {
        _plantRenderer.enabled = false;
        _highlightRenderer.enabled = false;
        _gameStateOverlayRenderer.enabled = false;
        _baseRenderer.color = _context.VisualConfig.dryColor;
    }

    /// <summary>
    /// Define estado de bloqueio do slot.
    /// </summary>
    public void SetLockedState(bool isLocked)
    {
        _isLocked = isLocked;
        
        if (_isLocked)
        {
            _baseRenderer.color = _context.VisualConfig.lockedColor;
            _plantRenderer.enabled = false;

           // if (_showDebugLogs) ;
                // Debug.Log($"[GridSlotView {_index}] Estado: LOCKED");
        }
    }

    /// <summary>
    /// Atualiza visual do slot (Manager Push pattern).
    /// </summary>
    public void SetVisualState(Sprite plantSprite, bool isWatered, bool isMature = false, bool isWithered = false)
    {
        if (_isLocked) return;

        // Layer 1: Plant Sprite
        bool hasPlant = plantSprite != null;
        if (hasPlant)
        {
            _plantRenderer.sprite = plantSprite;
            _plantRenderer.enabled = true;
        }
        else
        {
            _plantRenderer.enabled = false;
            _plantRenderer.sprite = null;
        }

        // Layer 0: Base Color (dry/wet)
        _baseRenderer.color = isWatered 
            ? _context.VisualConfig.wetColor 
            : _context.VisualConfig.dryColor;

        // Layer 2: State Overlay (mature/withered/planted)
        if (hasPlant && _stateOverlayRenderer != null)
        {
            if (isMature)
            {
                // Verde forte - pronto para colher
                _stateOverlayRenderer.enabled = true;
                _stateOverlayRenderer.color = _context.VisualConfig.matureOverlay;
                
                if (_showDebugLogs)
                    Debug.Log($"[GridSlotView {_index}] STATE: MATURE (verde forte)");
            }
            else if (isWithered)
            {
                // Amarelo - planta murcha
                _stateOverlayRenderer.enabled = true;
                _stateOverlayRenderer.color = _context.VisualConfig.witheredOverlay;
                
                if (_showDebugLogs)
                    Debug.Log($"[GridSlotView {_index}] STATE: WITHERED (amarelo)");
            }
            else
            {
                // Verde suave - tem algo plantado (crescendo)
                _stateOverlayRenderer.enabled = true;
                _stateOverlayRenderer.color = _context.VisualConfig.plantedOverlay;
                
                if (_showDebugLogs)
                    Debug.Log($"[GridSlotView {_index}] STATE: PLANTED (verde suave) - Color: {_context.VisualConfig.plantedOverlay}");
            }
        }
        else if (_stateOverlayRenderer != null)
        {
            _stateOverlayRenderer.enabled = false;
            
            if (_showDebugLogs && hasPlant)
                Debug.LogWarning($"[GridSlotView {_index}] STATE: Tem planta mas _stateOverlayRenderer NULL!");
        }
        else if (hasPlant && _showDebugLogs)
        {
            Debug.LogError($"[GridSlotView {_index}] CRITICAL: Tem planta mas _stateOverlayRenderer é NULL!");
        }

        if (_showDebugLogs)
            Debug.Log($"[GridSlotView {_index}] Visual: plant={hasPlant}, water={isWatered}, mature={isMature}, withered={isWithered}");
    }

    /// <summary>
    /// Gerencia overlay de GameState (Layer 3).
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        if (_isLocked) return;

        bool isDisabled = (newState != GameState.Playing);
        
        if (_gameStateOverlayRenderer != null)
        {
            _gameStateOverlayRenderer.enabled = isDisabled;
            
            if (isDisabled)
            {
                _gameStateOverlayRenderer.color = _context.VisualConfig.disabledOverlay;
            }
        }

        if (_showDebugLogs && isDisabled)
            Debug.Log($"[GridSlotView {_index}] GameState overlay ativo: {newState}");
    }

    // --- INTERFACE IINTERACTABLE (HOVER) ---

    public void OnHoverEnter()
    {
        if (_highlightRenderer != null)
        {
            _highlightRenderer.enabled = true;
            _highlightRenderer.color = _context.VisualConfig.validHover;
        }
    }

    public void OnHoverExit()
    {
        if (_highlightRenderer != null)
        {
            _highlightRenderer.enabled = false;
        }
    }

    public void OnClick()
    {
        // Futuro: Abrir menu de detalhes
    }

    // --- INTERFACE IDROPTARGET (DRAG & DROP) ---

    public bool CanReceive(IDraggable draggable)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning($"[GridSlotView {_index}] CanReceive chamado antes de Initialize!");
            return false;
        }

        if (draggable is not CardView cardView) return false;

        bool canDrop = _context.DropValidator.CanDrop(_index, cardView.Data);

        if (!canDrop)
        {
            StartCoroutine(FlashError());
        }

        return canDrop;
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            OnDropInteraction?.Invoke(SlotIndex, cardView);
            OnHoverExit();
        }
    }

    /// <summary>
    /// Flash vermelho quando ação inválida.
    /// </summary>
    private IEnumerator FlashError()
    {
        if (_highlightRenderer == null) yield break;

        Color originalColor = _highlightRenderer.color;
        _highlightRenderer.enabled = true;
        _highlightRenderer.color = _context.VisualConfig.errorFlash;

        yield return new WaitForSeconds(_context.VisualConfig.flashDuration);

        _highlightRenderer.color = originalColor;
        _highlightRenderer.enabled = false;
    }

    /// <summary>
    /// Pulse rosa/branco quando slot está sendo analisado (final do dia).
    /// </summary>
    private void HandleAnalyzeSlot(int slotIndex)
    {
        if (slotIndex != _index) return;

        if (_showDebugLogs)
            Debug.Log($"[GridSlotView {_index}] Analisando slot...");

        StartCoroutine(PulseAnalyzing());
    }

    private IEnumerator PulseAnalyzing()
    {
        if (_highlightRenderer == null) yield break;

        Color originalColor = _highlightRenderer.color;
        bool wasEnabled = _highlightRenderer.enabled;

        _highlightRenderer.enabled = true;
        _highlightRenderer.color = _context.VisualConfig.analyzingPulse;

        yield return new WaitForSeconds(_context.VisualConfig.pulseDuration);

        _highlightRenderer.enabled = wasEnabled;
        _highlightRenderer.color = originalColor;
    }

    // --- CONFIGURAÇÃO AUTOMÁTICA (SETUP) ---

    private void ConfigureRenderers()
    {
        if (_baseRenderer == null) 
            _baseRenderer = GetComponent<SpriteRenderer>();

        if (_plantRenderer == null || _plantRenderer == _baseRenderer)
        {
            _plantRenderer = CreateChildSprite("PlantSprite", 1);
        }

        if (_stateOverlayRenderer == null || _stateOverlayRenderer == _baseRenderer)
        {
            _stateOverlayRenderer = CreateChildSprite("StateOverlay", 2);
            _stateOverlayRenderer.sprite = _baseRenderer.sprite;
            _stateOverlayRenderer.enabled = false;
        }

        if (_gameStateOverlayRenderer == null || _gameStateOverlayRenderer == _baseRenderer)
        {
            _gameStateOverlayRenderer = CreateChildSprite("GameStateOverlay", 3);
            _gameStateOverlayRenderer.sprite = _baseRenderer.sprite;
            _gameStateOverlayRenderer.enabled = false;
        }

        if (_highlightRenderer == null || _highlightRenderer == _baseRenderer)
        {
            _highlightRenderer = CreateChildSprite("HighlightOverlay", 4);
            _highlightRenderer.sprite = _baseRenderer.sprite;
        }
    }

    private SpriteRenderer CreateChildSprite(string name, int orderOffset)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(this.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingLayerID = _baseRenderer.sortingLayerID;
        sr.sortingOrder = _baseRenderer.sortingOrder + orderOffset;
        sr.enabled = false;

        return sr;
    }
    
    // --- API PÚBLICA PARA PATTERN HIGHLIGHTS (ONDA 5.5) ---
    
    /// <summary>
    /// Define cor do highlight (Layer 4) para pattern system.
    /// </summary>
    /// <param name="color">Cor do highlight</param>
    /// <param name="enable">Se true, ativa o highlight</param>
    public void SetPatternHighlight(Color color, bool enable = true)
    {
        if (_highlightRenderer == null) return;
        
        _highlightRenderer.color = color;
        _highlightRenderer.enabled = enable;
    }
    
    /// <summary>
    /// Limpa o highlight (desativa Layer 4).
    /// </summary>
    public void ClearPatternHighlight()
    {
        if (_highlightRenderer == null) return;
        
        _highlightRenderer.enabled = false;
    }
    
    /// <summary>
    /// Retorna se o highlight está ativo.
    /// </summary>
    public bool IsHighlightActive()
    {
        return _highlightRenderer != null && _highlightRenderer.enabled;
    }
    
    /// <summary>
    /// Verifica se o slot tem planta.
    /// </summary>
    public bool HasPlant()
    {
        // Verificar se _plantRenderer tem sprite atribuído
        return _plantRenderer != null && _plantRenderer.sprite != null;
    }
    
    /// <summary>
    /// NOVO (ONDA 5.5): Trigger pulse rosa (analyzing pulse).
    /// </summary>
    public void TriggerAnalyzingPulse(Color pulseColor, float duration = 0.2f)
    {
        if (_highlightRenderer == null) return;
        
        StartCoroutine(AnalyzingPulseRoutine(pulseColor, duration));
    }
    
    /// <summary>
    /// Coroutine de analyzing pulse.
    /// </summary>
    private IEnumerator AnalyzingPulseRoutine(Color pulseColor, float duration)
    {
        if (_highlightRenderer == null) yield break;
        
        bool wasEnabled = _highlightRenderer.enabled;
        Color originalColor = _highlightRenderer.color;
        
        // Ativar com cor de pulse
        _highlightRenderer.enabled = true;
        _highlightRenderer.color = pulseColor;
        
        yield return new WaitForSeconds(duration);
        
        // Restaurar estado original
        _highlightRenderer.enabled = wasEnabled;
        _highlightRenderer.color = originalColor;
    }
    
    // --- API PÚBLICA PARA CROP PASSIVE SCORE ---
    
    /// <summary>
    /// Mostra pontos passivos do crop em cima do slot com animação.
    /// </summary>
    /// <param name="points">Pontos a mostrar (ex: 10, 15)</param>
    public void ShowPassiveScore(int points)
    {
        if (_passiveScoreText == null || _passiveScoreGroup == null)
        {
            Debug.LogWarning($"[GridSlotView {_index}] PassiveScore não configurado no Inspector!");
            return;
        }
        
        // Configurar texto
        _passiveScoreText.text = $"+{points}";
        
        // Resetar estado inicial (escondido)
        _passiveScoreGroup.alpha = 0f;
        
        // Iniciar animação
        StartCoroutine(AnimatePassiveScore());
    }
    
    private IEnumerator AnimatePassiveScore()
    {
        // Guardar posição inicial
        Vector3 startPos = _passiveScoreText.transform.localPosition;
        float popUpHeight = 0.5f; // Altura do movimento para cima
        
        // Fase 1: Fade IN + Pop-up rápido (0.2s)
        float fadeInTime = _scoreFadeDuration * 0.25f;
        float elapsed = 0f;
        
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInTime;
            
            // Fade in
            float alpha = Mathf.Lerp(0f, 1f, t);
            _passiveScoreGroup.alpha = alpha;
            
            // Pop-up (movimento para cima com ease-out)
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out
            Vector3 newPos = startPos + Vector3.up * (popUpHeight * easedT);
            _passiveScoreText.transform.localPosition = newPos;
            
            yield return null;
        }
        
        _passiveScoreGroup.alpha = 1f;
        _passiveScoreText.transform.localPosition = startPos + Vector3.up * popUpHeight;
        
        // Fase 2: Hold (mostrar texto por 0.4s)
        yield return new WaitForSeconds(_scoreFadeDuration * 0.5f);
        
        // Fase 3: Fade OUT (0.3s)
        float fadeOutTime = _scoreFadeDuration * 0.375f;
        elapsed = 0f;
        
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutTime);
            _passiveScoreGroup.alpha = alpha;
            yield return null;
        }
        
        _passiveScoreGroup.alpha = 0f;
        
        // Restaurar posição original
        _passiveScoreText.transform.localPosition = startPos;
    }
}


