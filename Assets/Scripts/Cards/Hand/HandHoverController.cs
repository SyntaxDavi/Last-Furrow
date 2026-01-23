using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controla a elevação/descida sequencial da mão de cartas.
/// Detecta quando o mouse está sobre a área da mão e dispara animações sequenciais.
/// Segue SRP: apenas coordena a sequência de elevação, não modifica cartas diretamente.
/// </summary>
public class HandHoverController : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private CardVisualConfig _visualConfig;
    
    [Header("Detecção")]
    [Tooltip("Intervalo entre checagens de hover (segundos) para otimização.")]
    [SerializeField] private float _hoverCheckInterval = 0.05f;

    [Tooltip("Tamanho da área de detecção ao redor de cada carta (Largura, Altura).")]
    [SerializeField] private Vector2 _cardDetectionSize = new Vector2(3.0f, 4.0f);
    
    [Tooltip("Deslocamento do centro da área de detecção em relação ao pivô da carta.")]
    [SerializeField] private Vector2 _detectionCenterOffset = new Vector2(0f, 0.5f);
    
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmos = false;
    
    // Referências
    private HandManager _handManager;
    private Camera _mainCamera;
    
    // Estado
    private bool _isHandHovered = false;
    private Coroutine _currentSequence;
    private float _lastHoverCheckTime;
    
    // ==================================================================================
    // INICIALIZAÇÃO
    // ==================================================================================
    
    public void Initialize(HandManager handManager)
    {
        _handManager = handManager;
        _mainCamera = Camera.main;
    }
    
    private void Start()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
    }
    
    private void Update()
    {
        if (_handManager == null) return;
        
        // Polling Throttled (Industry Standard)
        // Reduz custo de cpu e ruído de input
        if (Time.time - _lastHoverCheckTime >= _hoverCheckInterval)
        {
            CheckHoverState();
            _lastHoverCheckTime = Time.time;
        }
    }
    
    // ==================================================================================
    // DETECÇÃO DE HOVER
    // ==================================================================================
    
    private void CheckHoverState()
    {
        // 0. Validações Globais (Fail Fast)
        if (!CanInteract())
        {
            if (_isHandHovered)
            {
                _isHandHovered = false;
                OnHandHoverChanged(false);
            }
            return;
        }

        bool isCurrentlyHovered = IsMouseOverHandArea();
        
        // Mudou de estado?
        if (isCurrentlyHovered != _isHandHovered)
        {
            _isHandHovered = isCurrentlyHovered;
            OnHandHoverChanged(_isHandHovered);
        }
    }
    
    private bool CanInteract()
    {
        // Bloqueia se estiver arrastando alguma carta (evita comportamento estranho)
        if (_handManager.IsDraggingAnyCard) return false;

        // Bloqueia se AppCore não estiver pronto ou Estado de jogo não for interativo
        if (AppCore.Instance == null || AppCore.Instance.GameStateManager == null) return true; // Default to true if decoupled
        
        var currentState = AppCore.Instance.GameStateManager.CurrentState;
        return currentState == GameState.Playing || currentState == GameState.Shopping;
    }
    
    private bool IsMouseOverHandArea()
    {
        if (_mainCamera == null) return false;

        Vector2 mousePosScreen = Input.mousePosition;
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mousePosScreen);
        
        // 1. Se temos collider definido manualmente, usa ele
        if (_handAreaCollider != null)
        {
            return _handAreaCollider.OverlapPoint(mouseWorldPos);
        }
        
        // 2. Fallback Preciso: União dos Retângulos das Cartas
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0) return false;
        
        // Otimização: não criar vector3 z=0 toda vez, usar float compare direto
        float mX = mouseWorldPos.x;
        float mY = mouseWorldPos.y;
        
        float halfWidth = _cardDetectionSize.x * 0.5f;
        float halfHeight = _cardDetectionSize.y * 0.5f;
        
        foreach (var card in cards)
        {
            // Pega a posição alvo do layout e aplica o offset configurado
            Vector2 centerPos = (Vector2)card.BaseLayoutTarget.Position + _detectionCenterOffset;
            
            // Check AABB 2D inlined
            if (mX >= centerPos.x - halfWidth && mX <= centerPos.x + halfWidth &&
                mY >= centerPos.y - halfHeight && mY <= centerPos.y + halfHeight)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // ==================================================================================
    // SEQUÊNCIA DE ANIMAÇÃO
    // ==================================================================================
    
    private void OnHandHoverChanged(bool isHovered)
    {
        if (_currentSequence != null)
        {
            StopCoroutine(_currentSequence);
            _currentSequence = null;
        }
        
        _currentSequence = StartCoroutine(RunElevationSequence(isHovered));
    }
    
    private IEnumerator RunElevationSequence(bool isRaising)
    {
        // _isSequenceRunning removido (código morto)
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0) yield break;
        
        float delay = _visualConfig != null 
            ? _visualConfig.HandElevationSequenceDelay 
            : 0.05f;
        
        int startIndex = isRaising ? 0 : cards.Count - 1;
        int endIndex = isRaising ? cards.Count : -1;
        int step = isRaising ? 1 : -1;
        
        for (int i = startIndex; i != endIndex; i += step)
        {
            if (i >= 0 && i < cards.Count)
            {
                cards[i].SetHandElevation(isRaising);
            }
            
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
        }
        
        _currentSequence = null;
    }
    
    // ==================================================================================
    // API PÚBLICA
    // ==================================================================================
    
    /// <summary>
    /// Retorna se a mão está atualmente elevada.
    /// </summary>
    public bool IsHandElevated => _isHandHovered;
    
    /// <summary>
    /// Força elevação imediata de todas as cartas (sem sequência).
    /// Útil para estados especiais ou reset.
    /// </summary>
    public void ForceElevation(bool elevated)
    {
        if (_currentSequence != null)
        {
            StopCoroutine(_currentSequence);
            _currentSequence = null;
        }
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null) return;
        
        foreach (var card in cards)
        {
            card.SetHandElevation(elevated);
        }
        
        _isHandHovered = elevated;
    }
    
    // ==================================================================================
    // DEBUG
    // ==================================================================================
    
    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos || _handManager == null) return;
        
        Gizmos.color = _isHandHovered ? Color.green : Color.yellow;
        var cards = _handManager.GetActiveCardsReadOnly();
        if (cards == null) return;
        
        foreach (var card in cards)
        {
            if (card != null)
            {
                 // Nota: Em editor time (sem play), BaseLayoutTarget pode estar zerado se não inicializado.
                 // Fallback para transform.position no Gizmo se necessário.
                 Vector3 pos = Application.isPlaying ? card.BaseLayoutTarget.Position : card.transform.position;
                 Gizmos.DrawWireCube(pos, new Vector3(_cardDetectionSize.x, _cardDetectionSize.y, 0.1f));
            }
        }
    }
}
