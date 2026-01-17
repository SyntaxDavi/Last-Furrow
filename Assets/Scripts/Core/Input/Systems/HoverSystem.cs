using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema responsável APENAS por detectar e gerenciar hover.
/// 
/// Responsabilidades:
/// - Detectar qual objeto está sob o mouse
/// - Resolver prioridade (quem fica "por cima")
/// - Aplicar histerese (sticky hover)
/// - Disparar OnHoverEnter/Exit
/// 
/// NÃO decide se pode ou não hoverar - isso é do InteractionPolicy.
/// </summary>
public class HoverSystem
{
    // Configuração
    private readonly float _exitThreshold;
    private readonly int _checkInterval;
    
    // Estado
    private IInteractable _currentHover;
    private int _frameCounter;
    private Vector2 _lastCheckPosition;
    
    // Cache e Buffers
    private const int MAX_HITS = 16;
    private readonly Collider2D[] _hitsBuffer = new Collider2D[MAX_HITS];
    private readonly Dictionary<Collider2D, IInteractable> _interactableCache = new Dictionary<Collider2D, IInteractable>();

    public IInteractable CurrentHover => _currentHover;

    public HoverSystem(float exitThreshold = 0.5f, int checkInterval = 2)
    {
        _exitThreshold = exitThreshold;
        _checkInterval = checkInterval;
    }

    /// <summary>
    /// Atualiza o hover. Retorna true se houve mudança.
    /// </summary>
    public bool Update(Vector2 worldPos, LayerMask targetMask, IDraggable currentlyDragging)
    {
        _frameCounter++;
        
        // Throttle: só checa a cada N frames, exceto se mouse moveu
        bool shouldCheck = (_frameCounter % _checkInterval == 0) 
                           || Vector2.SqrMagnitude(worldPos - _lastCheckPosition) > 0.01f;
        
        if (!shouldCheck) return false;
        
        _lastCheckPosition = worldPos;
        
        // Encontra candidato
        IInteractable candidate = FindBestCandidate(worldPos, targetMask, currentlyDragging);
        
        // Aplica sticky hover (histerese)
        candidate = ApplyStickyHover(candidate, worldPos);
        
        // Aplica mudança se necessário
        return ApplyHoverChange(candidate);
    }

    /// <summary>
    /// Força limpeza do hover atual (ex: quando começa drag).
    /// </summary>
    public void ClearHover()
    {
        if (_currentHover != null && IsObjectAlive(_currentHover))
        {
            _currentHover.OnHoverExit();
        }
        _currentHover = null;
    }

    /// <summary>
    /// Limpa o cache de componentes.
    /// </summary>
    public void ClearCache()
    {
        _interactableCache.Clear();
    }

    private IInteractable FindBestCandidate(Vector2 worldPos, LayerMask mask, IDraggable currentlyDragging)
    {
        int hitCount = Physics2D.OverlapPointNonAlloc(worldPos, _hitsBuffer, mask);
        IInteractable best = null;
        int highestPriority = int.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
            var col = _hitsBuffer[i];
            var interactable = GetCachedInteractable(col);
            
            if (interactable == null) continue;
            
            // Ignora quem está sendo arrastado
            if (currentlyDragging != null && interactable is IDraggable drag && drag == currentlyDragging)
                continue;

            int priority = interactable.InteractionPriority;
            
            if (priority > highestPriority)
            {
                highestPriority = priority;
                best = interactable;
            }
        }

        return best;
    }

    private IInteractable ApplyStickyHover(IInteractable candidate, Vector2 worldPos)
    {
        // Se não temos hover atual ou o candidato é o mesmo, não precisa de sticky
        if (_currentHover == null || candidate == _currentHover)
            return candidate;

        // Verifica se o mouse ainda está perto do hover atual
        if (_currentHover is MonoBehaviour hoverMono && hoverMono != null)
        {
            var hoverCol = hoverMono.GetComponent<Collider2D>();
            if (hoverCol != null)
            {
                Vector2 closestPoint = hoverCol.ClosestPoint(worldPos);
                float distance = Vector2.Distance(worldPos, closestPoint);

                // Mantém o hover atual se ainda está perto
                if (distance < _exitThreshold)
                {
                    return _currentHover;
                }
            }
        }

        return candidate;
    }

    private bool ApplyHoverChange(IInteractable newHover)
    {
        if (newHover == _currentHover) return false;

        // Sai do anterior
        if (_currentHover != null && IsObjectAlive(_currentHover))
        {
            _currentHover.OnHoverExit();
        }

        _currentHover = newHover;

        // Entra no novo
        if (_currentHover != null)
        {
            _currentHover.OnHoverEnter();
        }

        return true;
    }

    private IInteractable GetCachedInteractable(Collider2D col)
    {
        if (col == null) return null;

        if (_interactableCache.TryGetValue(col, out var cached))
        {
            // Valida se ainda existe
            if (cached is Object obj && obj == null)
            {
                _interactableCache.Remove(col);
                return null;
            }
            return cached;
        }

        var interactable = col.GetComponent<IInteractable>();
        _interactableCache[col] = interactable;
        return interactable;
    }

    private bool IsObjectAlive(object obj)
    {
        return obj != null && !((obj is Object unityObj) && unityObj == null);
    }
}
