using System;

namespace LastFurrow.UI.Components
{
    /// <summary>
    /// Abstraction for menu navigation input.
    /// Allows MenuGroup to be input-source agnostic.
    /// Future: Can be implemented by InputManager for gamepad/rebind support.
    /// </summary>
    public interface IMenuInputProvider
    {
        event Action OnNavigateUp;
        event Action OnNavigateDown;
        event Action OnConfirm;
        event Action OnCancel;
    }
}
