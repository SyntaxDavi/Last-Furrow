using UnityEngine;
using UnityEngine.InputSystem; 

public class InputManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private CursorSettings _cursorSettings;

    private CursorLogic _cursorLogic;
    private Camera _mainCamera;

    public Vector2 MouseWorldPosition { get; private set; }

    // Eventos locais de Input (ainda úteis para coisas de baixo nível como arrastar carta)
    public event System.Action OnPrimaryClick;

    public void Initialize()
    {
        _cursorLogic = new CursorLogic(_cursorSettings);
        UpdateCameraReference();
    }

    public void UpdateCameraReference()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        DetectAnyInput();

        if (_mainCamera == null) return;

        Vector2 mousePos = Vector2.zero;
        bool clickDown = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
            clickDown = Mouse.current.leftButton.wasPressedThisFrame;
        }
#else
        mousePos = Input.mousePosition;
        clickDown = Input.GetMouseButtonDown(0);
#endif

        // Atualiza posição
        MouseWorldPosition = _cursorLogic.GetWorldPosition(_mainCamera, mousePos);

        // Dispara clique
        if (clickDown)
        {
            OnPrimaryClick?.Invoke();
            // Opcional: Se quiser que todo clique conte como AnyInput
            AppCore.Instance.Events.TriggerAnyInput();
        }
    }

    private void DetectAnyInput()
    {
        bool anyInput = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) anyInput = true;
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) anyInput = true;
#else
        if (Input.anyKeyDown) anyInput = true;
#endif

        if (anyInput)
        {
            AppCore.Instance.Events.TriggerAnyInput();
        }
    }
    public bool IsFastForwardHeld
    {
        get
        {
            // Aqui você centraliza a lógica física
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }
}