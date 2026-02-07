/// <summary>
/// Adapter que expõe o estado de drag do PlayerInteraction via interface.
/// Permite injeção de dependência em Views sem acoplamento direto.
/// </summary>
public class PlayerInteractionDragAdapter : IDragStateProvider
{
    private readonly PlayerInteraction _source;

    public PlayerInteractionDragAdapter(PlayerInteraction source)
    {
        _source = source;
    }

    public bool IsDragging => _source?.DragSystem?.IsDragging ?? false;
    public IDraggable ActiveDrag => _source?.DragSystem?.ActiveDrag;
}