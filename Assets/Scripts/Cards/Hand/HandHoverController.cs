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
    
    [Header("Fade Gradual")]
    [Tooltip("Altura extra ACIMA do bounds onde o fade gradual acontece. As cartas vão descendo conforme o mouse sobe nessa zona.")]
    [SerializeField] private float _fadeOutZoneHeight = 2.0f;
    
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmos = false;
    
    // Referências
    private HandManager _handManager;
    private InputManager _inputManager;
    
    // Estado
    private bool _isHandHovered = false;
    private Coroutine _currentSequence;
    private float _lastHoverCheckTime;
    private int _currentPatternIndex = 0; 
    
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

        // Calcula o fator de elevação (0-1) baseado na posição Y
        float elevationFactor = CalculateElevationFactor();
        bool isCurrentlyHovered = elevationFactor > 0.01f; // Considera hovering se fator > 0
        
        // Se está na zona de fade (fator entre 0 e 1), aplica gradualmente a TODAS as cartas
        if (elevationFactor > 0.01f && elevationFactor < 0.99f)
        {
            ApplyGradualFade(elevationFactor);
        }
        
        // Mudou de estado? (entrou ou saiu completamente)
        if (isCurrentlyHovered != _isHandHovered)
        {
            _isHandHovered = isCurrentlyHovered;
            OnHandHoverChanged(_isHandHovered);
        }
    }
    
    /// <summary>
    /// Aplica o fator de elevação gradual a todas as cartas.
    /// Usado quando o mouse está na zona de transição (fade zone).
    /// </summary>
    private void ApplyGradualFade(float factor)
    {
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null) return;
        
        foreach (var card in cards)
        {
            card.SetElevationFactor(factor);
        }
    }
    
    private bool CanInteract()
    {
        // Bloqueia se estiver arrastando alguma carta (evita comportamento estranho)
        if (_handManager == null || _handManager.IsDraggingAnyCard) return false;

        // Fail-safe: se não consegue determinar estado, bloqueia
        if (AppCore.Instance == null || AppCore.Instance.GameStateManager == null) return false;
        
        var currentState = AppCore.Instance.GameStateManager.CurrentState;
        return currentState == GameState.Playing || currentState == GameState.Shopping;
    }
    
    // IsMouseOverHandArea() removido - usar CalculateElevationFactor() que é mais completo
    
    /// <summary>
    /// Calcula o fator de elevação (0.0 a 1.0) baseado na posição Y do mouse.
    /// 1.0 = totalmente dentro do bounds (cartas levantadas)
    /// 0.0 = fora da zona de fade (cartas abaixadas)
    /// Valores intermediários = transição gradual
    /// </summary>
    private float CalculateElevationFactor()
    {
        if (_inputManager == null) return 0f;
        if (!TryCalculateHandBounds(out Bounds handBounds)) return 0f;
        
        Vector2 mouseWorldPos = _inputManager.MouseWorldPosition;
        
        // Se o mouse está FORA dos limites X, fator = 0
        if (mouseWorldPos.x < handBounds.min.x || mouseWorldPos.x > handBounds.max.x)
        {
            return 0f;
        }
        
        float boundsTop = handBounds.max.y;
        float fadeZoneTop = boundsTop + _fadeOutZoneHeight;
        
        // Se o mouse está DENTRO do bounds, fator = 1
        if (mouseWorldPos.y >= handBounds.min.y && mouseWorldPos.y <= boundsTop)
        {
            return 1f;
        }
        
        // Se o mouse está NA ZONA DE FADE (acima do bounds, dentro da zona de transição)
        if (mouseWorldPos.y > boundsTop && mouseWorldPos.y < fadeZoneTop)
        {
            // Interpola de 1 (no topo do bounds) para 0 (no topo da zona de fade)
            float distanceIntoFadeZone = mouseWorldPos.y - boundsTop;
            float fadeProgress = distanceIntoFadeZone / _fadeOutZoneHeight;
            return 1f - Mathf.Clamp01(fadeProgress);
        }
        
        // Fora de tudo
        return 0f;
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
        
        // Inicializa com a primeira carta (null-check defensivo)
        if (cards[0] == null) return false;
        Vector2 firstPos = (Vector2)cards[0].BaseLayoutTarget.Position + _detectionCenterOffset;
        float minX = firstPos.x - halfWidth;
        float maxX = firstPos.x + halfWidth;
        float minY = firstPos.y - halfHeight;
        float maxY = firstPos.y + halfHeight;
        
        // Expande para incluir todas as outras cartas
        for (int i = 1; i < cards.Count; i++)
        {
            if (cards[i] == null) continue; // Pula cartas sendo destruídas
            
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
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0) yield break;
        
        float delay = _visualConfig != null 
            ? _visualConfig.HandElevationSequenceDelay 
            : 0.05f;
        
        // Obtém o padrão atual do ciclo (avança ANTES de iniciar, evita modificação durante coroutine)
        var pattern = (SequencePatternGenerator.SweepPattern)(_currentPatternIndex % 4);
        int patternToUse = _currentPatternIndex;
        if (isRaising) _currentPatternIndex++; // Só avança quando está SUBINDO
        
        // Gera a ordem de acordo com o padrão
        List<int> order = SequencePatternGenerator.GetOrder(cards.Count, pattern);
        
        // Se está descendo, inverte a ordem (efeito espelho)
        if (!isRaising)
        {
            order.Reverse();
        }
        
        // Executa na ordem gerada
        foreach (int index in order)
        {
            if (index >= 0 && index < cards.Count)
            {
                // Unificado: usar SetElevationFactor em vez de SetHandElevation
                // Isso evita conflito de estado entre sequência e fade gradual
                cards[index].SetElevationFactor(isRaising ? 1f : 0f);
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
            
            // Desenha a zona de FADE em amarelo (acima dos bounds)
            if (_fadeOutZoneHeight > 0)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Amarelo semi-transparente
                Vector3 fadeCenter = handBounds.center + Vector3.up * (handBounds.size.y * 0.5f + _fadeOutZoneHeight * 0.5f);
                Vector3 fadeSize = new Vector3(handBounds.size.x, _fadeOutZoneHeight, 0.1f);
                Gizmos.DrawWireCube(fadeCenter, fadeSize);
            }
        }
    }
}
