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
    
    [Tooltip("Padding extra aplicado ao bounding box da mão para detecção de hover.")]
    [SerializeField] private Vector2 _boundsPadding = new Vector2(0.5f, 0.5f);
    
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
    private float _lastAppliedFactor;
    private int _currentPatternIndex = 0; 
    private bool _isAtTop = false;
    private bool _isAtBottom = true; // Começa na base    
    // Cache de Performance
    private Bounds _cachedBounds;
    private bool _isBoundsDirty = true;
    private readonly List<int> _sequenceIndexBuffer = new List<int>(15);
    
    // Constantes
    private const float HOVER_ENTER_THRESHOLD = 0.01f;
    private const float HOVER_EXIT_THRESHOLD = 0.99f;
    private const float FADE_CHANGE_THRESHOLD = 0.005f;
    private const int MAX_CARDS_SUPPORTED = 15;
    
    // ==================================================================================
    // INICIALIZAÇÃO
    // ==================================================================================
    
    public void Initialize(HandManager handManager)
    {
        // Limpeza de inscrição anterior (Prevenção de Memory Leak)
        if (_handManager != null)
        {
            _handManager.OnHandLayoutChanged -= InvalidateBoundsCache;
        }

        _handManager = handManager;
        _inputManager = AppCore.Instance?.InputManager;
        
        // Invalida cache quando o layout da mão muda fisicamente
        if (_handManager != null)
        {
            _handManager.OnHandLayoutChanged += InvalidateBoundsCache;
        }
    }
    
    private void OnDestroy()
    {
        if (_handManager != null)
        {
            _handManager.OnHandLayoutChanged -= InvalidateBoundsCache;
        }
    }
    
    public void InvalidateBoundsCache() => _isBoundsDirty = true;
    
    private void Start()
    {
        if (_inputManager == null)
            _inputManager = AppCore.Instance?.InputManager;
    }
    
    private void Update()
    {
        if (_handManager == null || _inputManager == null) return;
        
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
        if (!CanInteract())
        {
            if (_isHandHovered)
            {
                _isHandHovered = false;
                OnHandHoverChanged(false);
            }
            return;
        }

        float elevationFactor = CalculateElevationFactor();
        bool isCurrentlyHovered = elevationFactor > HOVER_ENTER_THRESHOLD;
        
        // CORREÇÃO CRÍTICA: Se o mouse está na zona de fade, interrompe a sequência
        // para dar controle analógico imediato ao jogador.
        if (elevationFactor > HOVER_ENTER_THRESHOLD && elevationFactor < HOVER_EXIT_THRESHOLD)
        {
            if (_currentSequence != null)
            {
                StopCoroutine(_currentSequence);
                _currentSequence = null;
            }
        }

        // Se mudou radicalmente de estado (entrou ou saiu completamente do box)
        if (isCurrentlyHovered != _isHandHovered)
        {
            _isHandHovered = isCurrentlyHovered;
            OnHandHoverChanged(_isHandHovered);
        }
        
        // Detecção de limites para Áudio
        UpdateLimitTriggers(elevationFactor);

        if (isCurrentlyHovered)
        {
            // Dentro da área de hover (fade ou full):
            // Só aplicamos o fade contínuo se não estivermos rodando uma sequência
            // de animação (evita "briga" entre a coroutine e o mouse)
            if (_currentSequence == null)
            {
                ApplyGradualFade(elevationFactor);
            }
        }
    }
    
    /// <summary>
    /// Aplica o fator de elevação gradual a todas as cartas.
    /// Usado quando o mouse está na zona de transição (fade zone).
    /// </summary>
    private void ApplyGradualFade(float factor)
    {
        // Proteção contra setter spam
        if (Mathf.Abs(factor - _lastAppliedFactor) < FADE_CHANGE_THRESHOLD) return;
        _lastAppliedFactor = factor;

        // Se estamos degradando gradualmente, a sequência automática deve parar 
        // para dar controle total ao mouse do jogador.
        if (_currentSequence != null)
        {
            StopCoroutine(_currentSequence);
            _currentSequence = null;
        }

        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null) return;
        
        foreach (var card in cards)
        {
            if (card != null) card.SetElevationFactor(factor);
        }
    }

    private void UpdateLimitTriggers(float factor)
    {
        // Topo (Elevated)
        if (factor >= HOVER_EXIT_THRESHOLD)
        {
            if (!_isAtTop)
            {
                _isAtTop = true;
                _isAtBottom = false;
                _handManager?.TriggerHandFullyElevated();
            }
        }
        // Base (Lowered)
        else if (factor <= HOVER_ENTER_THRESHOLD)
        {
            if (!_isAtBottom)
            {
                _isAtBottom = true;
                _isAtTop = false;
                _handManager?.TriggerHandFullyLowered();
            }
        }
        else
        {
            // Resetar flags quando estiver no meio do caminho para permitir disparar de novo ao chegar nos limites
            _isAtTop = false;
            _isAtBottom = false;
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
        if (!_isBoundsDirty)
        {
            bounds = _cachedBounds;
            return true;
        }

        bounds = default;
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0) return false;
        
        float halfWidth = _cardDetectionSize.x * 0.5f;
        float halfHeight = _cardDetectionSize.y * 0.5f;
        
        bool foundFirstValid = false;
        float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;

        foreach (var card in cards)
        {
            if (card == null) continue;

            Vector2 cardPos = (Vector2)card.BaseLayoutTarget.Position + _detectionCenterOffset;
            float cMinX = cardPos.x - halfWidth;
            float cMaxX = cardPos.x + halfWidth;
            float cMinY = cardPos.y - halfHeight;
            float cMaxY = cardPos.y + halfHeight;

            if (!foundFirstValid)
            {
                minX = cMinX;
                maxX = cMaxX;
                minY = cMinY;
                maxY = cMaxY;
                foundFirstValid = true;
            }
            else
            {
                if (cMinX < minX) minX = cMinX;
                if (cMaxX > maxX) maxX = cMaxX;
                if (cMinY < minY) minY = cMinY;
                if (cMaxY > maxY) maxY = cMaxY;
            }
        }

        if (!foundFirstValid) return false;
        
        // Cria e cacheia o bounds (com padding)
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3((maxX - minX) + _boundsPadding.x * 2f, (maxY - minY) + _boundsPadding.y * 2f, 0.1f);
        
        _cachedBounds = new Bounds(center, size);
        _isBoundsDirty = false;
        
        bounds = _cachedBounds;
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
        
        // Só avança o padrão quando está começando a SUBIDA
        if (isHovered) _currentPatternIndex++;
        
        _currentSequence = StartCoroutine(RunElevationSequence(isHovered));
    }
    
    private IEnumerator RunElevationSequence(bool isRaising)
    {
        // DELAY GLOBAL antes de iniciar a sequência
        if (_visualConfig != null)
        {
            float startDelay = isRaising 
                ? _visualConfig.HandElevationStartDelay 
                : _visualConfig.HandLoweringStartDelay;
            
            if (startDelay > 0)
            {
                yield return new WaitForSeconds(startDelay);
            }
        }
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0) yield break;
        
        float delay = _visualConfig != null 
            ? _visualConfig.HandElevationSequenceDelay 
            : 0.05f;
        
        // Obtém o padrão atual de forma estável
        var pattern = (SequencePatternGenerator.SweepPattern)(_currentPatternIndex % 4);
        
        // Gera a ordem de acordo com o padrão usando o buffer zero-allocation
        SequencePatternGenerator.FillOrder(_sequenceIndexBuffer, cards.Count, pattern);
        
        // Se está descendo, inverte a ordem (efeito espelho)
        if (!isRaising)
        {
            _sequenceIndexBuffer.Reverse();
        }
        
        // Executa na ordem gerada
        foreach (int index in _sequenceIndexBuffer)
        {
            if (index >= 0 && index < cards.Count)
            {
                var card = cards[index];
                if (card != null)
                {
                    card.SetElevationFactor(isRaising ? 1f : 0f);
                }
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
            if (card != null) card.SetElevationFactor(elevated ? 1f : 0f);
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
            // Gizmo das cartas individuais (Wire)
            var cards = _handManager?.GetActiveCardsReadOnly();
            if (cards != null)
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                foreach(var card in cards)
                {
                    if (card == null) continue;
                    Vector3 cardPos = (Vector3)card.BaseLayoutTarget.Position + (Vector3)_detectionCenterOffset;
                    Gizmos.DrawWireCube(cardPos, (Vector3)_cardDetectionSize);
                }
            }

            // Verde se hovering, Cyan se não
            Gizmos.color = _isHandHovered ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(handBounds.center, handBounds.size);
            
            // Desenha a zona de FADE em amarelo (acima dos bounds)
            if (_fadeOutZoneHeight > 0)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Amarelo semi-transparente
                Vector3 fadeCenter = handBounds.center + Vector3.up * (handBounds.size.y * 0.5f + _fadeOutZoneHeight * 0.5f);
                Vector3 fadeSize = new Vector3(handBounds.size.x, _fadeOutZoneHeight, 0.1f);
                Gizmos.DrawWireCube(fadeCenter, fadeSize);
            }
        }
    }
}
