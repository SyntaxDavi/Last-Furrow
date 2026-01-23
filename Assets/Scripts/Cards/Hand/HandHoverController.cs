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
    [Tooltip("Collider que define a área da mão. Se null, usa detecção precisa por carta.")]
    [SerializeField] private Collider2D _handAreaCollider;
    
    [Tooltip("Tamanho da área de detecção ao redor de cada carta (Largura, Altura).")]
    [SerializeField] private Vector2 _cardDetectionSize = new Vector2(1.5f, 2.2f);
    
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmos = false;
    
    // Referências
    private HandManager _handManager;
    private IReadOnlyList<CardView> _cachedCards;
    
    // Estado
    private bool _isHandHovered = false;
    private bool _isSequenceRunning = false;
    private Coroutine _currentSequence;
    
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
        // 1. Se temos collider definido manualmente, usa ele
        if (_handAreaCollider != null)
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return _handAreaCollider.OverlapPoint(mouseWorld);
        }
        
        // 2. Fallback Preciso: União dos Retângulos das Cartas
        // Verifica se o mouse está próximo da posição BASE de qualquer carta.
        // Usamos a posição BASE (LayoutTarget) e não a visual para evitar flickering quando a carta sobe.
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards == null || cards.Count == 0) return false;
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // Ignora profundidade para o check 2D
        
        float halfWidth = _cardDetectionSize.x * 0.5f;
        float halfHeight = _cardDetectionSize.y * 0.5f;
        
        foreach (var card in cards)
        {
            // Pega a posição alvo do layout (estável)
            Vector3 cardBasePos = card.BaseLayoutTarget.Position;
            
            // Check AABB simples ao redor da carta
            bool insideX = mousePos.x >= cardBasePos.x - halfWidth && mousePos.x <= cardBasePos.x + halfWidth;
            bool insideY = mousePos.y >= cardBasePos.y - halfHeight && mousePos.y <= cardBasePos.y + halfHeight;
            
            if (insideX && insideY)
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
