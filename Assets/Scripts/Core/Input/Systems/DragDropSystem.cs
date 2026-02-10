using System;
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
/// 
/// REFATORAÇÃO (Fase 2):
/// - Agora chama PlayInvalidDropFeedback() quando drop falha
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
    private bool _isOverValidDropTarget;

    // -- Eventos Públicos --
    public event Action<IDraggable> OnDragStarted;
    public event Action<IDraggable> OnDragEnded;
    public bool IsDragging => _isDragging;
    public IDraggable ActiveDrag => _activeDrag;
    public IDraggable PotentialDrag => _potentialDrag;
    
    /// <summary>
    /// Retorna true se o drag atual está sobre um drop target válido.
    /// Usado para feedback visual (ex: transparência ghost).
    /// </summary>
    public bool IsOverValidDropTarget => _isOverValidDropTarget;

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

        OnDragStarted?.Invoke(_activeDrag);
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
    /// Fase 2: Chama PlayInvalidDropFeedback() quando drop falha.
    /// </summary>
    public DragResult FinishDrag(Vector2 worldPos)
    {
        if (!_isDragging || _activeDrag == null)
            return new DragResult(false, null);

        // Encontra target
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IDropTarget target = col != null ? col.GetComponent<IDropTarget>() : null;

        bool success = false;

        if (target != null)
        {
            if (target.CanReceive(_activeDrag))
            {
                target.OnReceive(_activeDrag);
                success = true;
            }
            else
            {
                // Fase 2: Feedback visual de drop inválido
                // Verifica se o target suporta o método de feedback
                if (target is GridSlotView slotView)
                {
                    slotView.PlayInvalidDropFeedback();
                }
            }
        }

        _activeDrag.OnDragEnd();
        OnDragEnded?.Invoke(_activeDrag);

        // Limpa drop hover e validade
        ClearDropHover();
        _isOverValidDropTarget = false;

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
            OnDragEnded?.Invoke(_activeDrag);
        }

        ClearDropHover();
        _isOverValidDropTarget = false;
        _isDragging = false;
        _activeDrag = null;
        _potentialDrag = null;
    }

    private void UpdateDropZoneHover(Vector2 worldPos)
    {
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IInteractable newDropHover = col != null ? col.GetComponent<IInteractable>() : null;

        // Verifica se é um drop target válido para o draggable atual
        IDropTarget dropTarget = col != null ? col.GetComponent<IDropTarget>() : null;
        _isOverValidDropTarget = dropTarget != null && _activeDrag != null && dropTarget.CanReceive(_activeDrag);

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
        return obj != null && !((obj is UnityEngine.Object unityObj) && unityObj == null);
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
