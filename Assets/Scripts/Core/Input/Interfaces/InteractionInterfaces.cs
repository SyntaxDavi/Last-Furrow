using UnityEngine;

/// <summary>
/// Interface para objetos interativos por clique
/// </summary>
public interface IInteractable
{
    void OnClick();
    void OnHoverEnter();
    void OnHoverExit();
}

/// <summary>
/// Interface para objetos arrastáveis
/// </summary>
public interface IDraggable
{
    void OnDragStart();
    void OnDragUpdate(Vector2 worldPosition);
    void OnDragEnd();
}

/// <summary>
/// Interface para alvos de drop
/// </summary>
public interface IDropTarget
{
    bool CanReceive(IDraggable draggable);
    void OnReceive(IDraggable draggable);
}
