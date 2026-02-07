/// <summary>
/// Abstração para consulta de estado de drag.
/// Permite que Views consultem se há drag ativo sem acoplar a PlayerInteraction.
/// </summary>
public interface IDragStateProvider
{
    bool IsDragging { get; }
    IDraggable ActiveDrag { get; }
}