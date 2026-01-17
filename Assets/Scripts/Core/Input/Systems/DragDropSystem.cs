using UnityEngine;

/// <summary>
/// Sistema responsável APENAS por gerenciar drag & drop.
/// 
/// Responsabilidades:
/// - Detectar início de drag (threshold de pixels)
/// - Atualizar posição durante drag
/// - Detectar drop zones e fazer hover nelas
/// - Finalizar drop (sucesso ou cancelamento)
/// 
/// NÃO decide se pode ou não arrastar - isso é do InteractionPolicy.
/// </summary>
public class DragDropSystem
{
    // Configuração
    private readonly float _dragThresholdPx;
    private readonly LayerMask _dropLayer;
    
    // Estado
    private IDraggable _potentialDrag;
    private IDraggable _activeDrag;
    private IInteractable _currentDropHover;
    private Vector2 _dragStartScreenPos;
    private bool _isDragging;

    public bool IsDragging => _isDragging;
    public IDraggable ActiveDrag => _activeDrag;
    public IDraggable PotentialDrag => _potentialDrag;

    public DragDropSystem(float dragThresholdPx, LayerMask dropLayer)
    {
        _dragThresholdPx = dragThresholdPx;
        _dropLayer = dropLayer;
    }

    /// <summary>
    /// Registra um potencial drag quando mouse é pressionado sobre um draggable.
    /// </summary>
    public void RegisterPotentialDrag(IDraggable draggable, Vector2 screenPos)
    {
        _potentialDrag = draggable;
        _dragStartScreenPos = screenPos;
    }

    /// <summary>
    /// Limpa o potencial drag (mouse liberado sem arrastar).
    /// </summary>
    public void ClearPotentialDrag()
    {
        _potentialDrag = null;
    }

    /// <summary>
    /// Verifica se deve iniciar drag baseado na distância do mouse.
    /// </summary>
    public bool ShouldStartDrag(Vector2 currentScreenPos)
    {
        if (_potentialDrag == null || _isDragging) return false;
        
        float dist = Vector2.Distance(currentScreenPos, _dragStartScreenPos);
        return dist > _dragThresholdPx;
    }

    /// <summary>
    /// Inicia o drag.
    /// </summary>
    public void StartDrag()
    {
        if (_potentialDrag == null) return;
        
        _isDragging = true;
        _activeDrag = _potentialDrag;
        _activeDrag.OnDragStart();
    }

    /// <summary>
    /// Atualiza o drag ativo.
    /// </summary>
    public void UpdateDrag(Vector2 worldPos)
    {
        if (!_isDragging || _activeDrag == null) return;
        
        _activeDrag.OnDragUpdate(worldPos);
        UpdateDropZoneHover(worldPos);
    }

    /// <summary>
    /// Finaliza o drag, tentando fazer drop.
    /// </summary>
    public DragResult FinishDrag(Vector2 worldPos)
    {
        if (!_isDragging || _activeDrag == null)
            return new DragResult(false, null);

        // Encontra target
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IDropTarget target = col != null ? col.GetComponent<IDropTarget>() : null;
        
        bool success = false;
        
        if (target != null && target.CanReceive(_activeDrag))
        {
            target.OnReceive(_activeDrag);
            success = true;
        }

        _activeDrag.OnDragEnd();

        // Limpa drop hover
        ClearDropHover();

        // Reset estado
        var result = new DragResult(success, target);
        _isDragging = false;
        _activeDrag = null;
        _potentialDrag = null;
        
        return result;
    }

    /// <summary>
    /// Cancela drag em andamento (ex: estado do jogo mudou).
    /// </summary>
    public void CancelDrag()
    {
        if (_isDragging && _activeDrag != null)
        {
            _activeDrag.OnDragEnd();
        }
        
        ClearDropHover();
        _isDragging = false;
        _activeDrag = null;
        _potentialDrag = null;
    }

    private void UpdateDropZoneHover(Vector2 worldPos)
    {
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IInteractable newDropHover = col != null ? col.GetComponent<IInteractable>() : null;

        if (newDropHover != _currentDropHover)
        {
            // Sai do anterior
            if (_currentDropHover != null && IsObjectAlive(_currentDropHover))
            {
                _currentDropHover.OnHoverExit();
            }

            _currentDropHover = newDropHover;

            // Entra no novo
            if (_currentDropHover != null)
            {
                _currentDropHover.OnHoverEnter();
            }
        }
    }

    private void ClearDropHover()
    {
        if (_currentDropHover != null && IsObjectAlive(_currentDropHover))
        {
            _currentDropHover.OnHoverExit();
        }
        _currentDropHover = null;
    }

    private bool IsObjectAlive(object obj)
    {
        return obj != null && !((obj is Object unityObj) && unityObj == null);
    }
}

/// <summary>
/// Resultado de uma operação de drop.
/// </summary>
public readonly struct DragResult
{
    public readonly bool Success;
    public readonly IDropTarget Target;

    public DragResult(bool success, IDropTarget target)
    {
        Success = success;
        Target = target;
    }
}
