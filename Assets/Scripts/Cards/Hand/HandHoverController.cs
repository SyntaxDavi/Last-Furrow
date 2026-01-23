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
    
    [Header("Detecção de Área")]
    [Tooltip("Collider que define a área da mão. Se null, usa bounds das cartas.")]
    [SerializeField] private Collider2D _handAreaCollider;
    
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmos = false;
    
    // Referências
    private HandManager _handManager;
    private IReadOnlyList<CardView> _cachedCards;
    
    // Estado
    private bool _isHandHovered = false;
    private bool _isSequenceRunning = false;
    private Coroutine _currentSequence;
    
    // Cache de bounds (para detecção sem collider)
    private Bounds _handBounds;
    private float _boundsUpdateInterval = 0.1f;
    private float _lastBoundsUpdate;
    
    // ==================================================================================
    // INICIALIZAÇÃO
    // ==================================================================================
    
    /// <summary>
    /// Inicializa o controller com referência ao HandManager.
    /// Deve ser chamado pelo HandManager no Awake/Start.
    /// </summary>
    public void Initialize(HandManager handManager)
    {
        _handManager = handManager;
    }
    
    private void Update()
    {
        if (_handManager == null) return;
        
        // Atualiza bounds periodicamente (evita cálculo todo frame)
        if (Time.time - _lastBoundsUpdate > _boundsUpdateInterval)
        {
            UpdateHandBounds();
            _lastBoundsUpdate = Time.time;
        }
        
        // Verifica hover
        CheckHoverState();
    }
    
    // ==================================================================================
    // DETECÇÃO DE HOVER
    // ==================================================================================
    
    private void CheckHoverState()
    {
        bool isCurrentlyHovered = IsMouseOverHandArea();
        
        // Mudou de estado?
        if (isCurrentlyHovered != _isHandHovered)
        {
            _isHandHovered = isCurrentlyHovered;
            OnHandHoverChanged(_isHandHovered);
        }
    }
    
    private bool IsMouseOverHandArea()
    {
        // Se temos collider, usa detecção de collider
        if (_handAreaCollider != null)
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return _handAreaCollider.OverlapPoint(mouseWorld);
        }
        
        // Fallback: usa bounds calculados
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        return _handBounds.Contains(mouseWorldPos);
    }
    
    private void UpdateHandBounds()
    {
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0)
        {
            _handBounds = new Bounds(transform.position, Vector3.zero);
            return;
        }
        
        // Inicializa com a primeira carta
        _handBounds = new Bounds(cards[0].transform.position, Vector3.one);
        
        // Expande para incluir todas as cartas
        foreach (var card in cards)
        {
            // Considera tamanho aproximado da carta
            Vector3 cardSize = new Vector3(1.5f, 2f, 0.1f);
            Bounds cardBounds = new Bounds(card.transform.position, cardSize);
            _handBounds.Encapsulate(cardBounds);
        }
        
        // Adiciona padding
        _handBounds.Expand(new Vector3(0.5f, 1f, 0));
    }
    
    // ==================================================================================
    // SEQUÊNCIA DE ANIMAÇÃO
    // ==================================================================================
    
    private void OnHandHoverChanged(bool isHovered)
    {
        // Cancela sequência anterior se existir
        if (_currentSequence != null)
        {
            StopCoroutine(_currentSequence);
            _currentSequence = null;
        }
        
        // Inicia nova sequência
        _currentSequence = StartCoroutine(RunElevationSequence(isHovered));
    }
    
    /// <summary>
    /// Executa a sequência de elevação/descida carta por carta.
    /// Se isRaising=true: começa da esquerda para direita.
    /// Se isRaising=false: começa da direita para esquerda (efeito cascata).
    /// </summary>
    private IEnumerator RunElevationSequence(bool isRaising)
    {
        _isSequenceRunning = true;
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0)
        {
            _isSequenceRunning = false;
            yield break;
        }
        
        float delay = _visualConfig != null 
            ? _visualConfig.HandElevationSequenceDelay 
            : 0.05f;
        
        // Ordem da sequência: normal para levantar, reversa para abaixar
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
        
        _isSequenceRunning = false;
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
        if (!_showDebugGizmos) return;
        
        // Desenha bounds da mão
        Gizmos.color = _isHandHovered ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(_handBounds.center, _handBounds.size);
    }
}
