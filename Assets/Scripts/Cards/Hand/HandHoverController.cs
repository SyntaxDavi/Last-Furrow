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
    private InputManager _inputManager;
    
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
        _inputManager = AppCore.Instance?.InputManager;
    }
    
    private void Start()
    {
        if (_inputManager == null)
            _inputManager = AppCore.Instance?.InputManager;
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
        if (_inputManager == null) return false;

        // Usa a posição do mouse já calculada corretamente pelo InputManager
        Vector2 mouseWorldPos = _inputManager.MouseWorldPosition;
        
        // Calcula bounds dinâmicos que englobam todas as cartas
        if (!TryCalculateHandBounds(out Bounds handBounds)) return false;
        
        // Verifica se o mouse está dentro dos bounds (2D check)
        return mouseWorldPos.x >= handBounds.min.x && mouseWorldPos.x <= handBounds.max.x &&
               mouseWorldPos.y >= handBounds.min.y && mouseWorldPos.y <= handBounds.max.y;
    }
    
    /// <summary>
    /// Calcula o bounding box que engloba todas as cartas ativas + padding.
    /// Retorna false se não há cartas.
    /// </summary>
    private bool TryCalculateHandBounds(out Bounds bounds)
    {
        bounds = default;
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0) return false;
        
        float halfWidth = _cardDetectionSize.x * 0.5f;
        float halfHeight = _cardDetectionSize.y * 0.5f;
        
        // Inicializa com a primeira carta
        Vector2 firstPos = (Vector2)cards[0].BaseLayoutTarget.Position + _detectionCenterOffset;
        float minX = firstPos.x - halfWidth;
        float maxX = firstPos.x + halfWidth;
        float minY = firstPos.y - halfHeight;
        float maxY = firstPos.y + halfHeight;
        
        // Expande para incluir todas as outras cartas
        for (int i = 1; i < cards.Count; i++)
        {
            Vector2 cardPos = (Vector2)cards[i].BaseLayoutTarget.Position + _detectionCenterOffset;
            
            float cardMinX = cardPos.x - halfWidth;
            float cardMaxX = cardPos.x + halfWidth;
            float cardMinY = cardPos.y - halfHeight;
            float cardMaxY = cardPos.y + halfHeight;
            
            if (cardMinX < minX) minX = cardMinX;
            if (cardMaxX > maxX) maxX = cardMaxX;
            if (cardMinY < minY) minY = cardMinY;
            if (cardMaxY > maxY) maxY = cardMaxY;
        }
        
        // Cria o bounds
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.1f);
        bounds = new Bounds(center, size);
        
        return true;
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
        
        // Tenta calcular os bounds dinâmicos (mesmo método usado na detecção)
        if (TryCalculateHandBounds(out Bounds handBounds))
        {
            // Verde se hovering, Cyan se não
            Gizmos.color = _isHandHovered ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(handBounds.center, handBounds.size);
        }
    }
}
