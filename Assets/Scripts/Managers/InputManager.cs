using UnityEngine;
using UnityEngine.InputSystem; 

public class InputManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private CursorSettings _cursorSettings;
    private CursorLogic _cursorLogic;
    private Camera _mainCamera;

    public Vector2 MouseWorldPosition { get; private set; }
    public Vector2 MouseScreenPosition { get; private set; }
    public bool IsPrimaryButtonDown { get; private set; } // Clicou neste frame?
    public bool IsPrimaryButtonUp { get; private set; }   // Soltou neste frame?
    public bool IsPrimaryButtonHeld { get; private set; } // Está segurando?

    // Eventos locais de Input (ainda úteis para coisas de baixo nível como arrastar carta)
    public event System.Action OnPrimaryClick;

    public void Initialize()
    {
        _cursorLogic = new CursorLogic(_cursorSettings);
        UpdateCameraReference();
    }

    public void UpdateCameraReference() => _mainCamera = Camera.main;


    private void Update()
    {
        if (_mainCamera == null) return;

        // 1. Leitura do Hardware (Centralizada aqui)
        Vector2 rawMousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            MouseScreenPosition = Mouse.current.position.ReadValue();
            IsPrimaryButtonDown = Mouse.current.leftButton.wasPressedThisFrame;
            IsPrimaryButtonUp = Mouse.current.leftButton.wasReleasedThisFrame;
            IsPrimaryButtonHeld = Mouse.current.leftButton.isPressed;
        }
#else
        MouseScreenPosition = Input.mousePosition;
        IsPrimaryButtonDown = Input.GetMouseButtonDown(0);
        IsPrimaryButtonUp = Input.GetMouseButtonUp(0);
        IsPrimaryButtonHeld = Input.GetMouseButton(0);
#endif

        // 2. Processamento Lógico
        MouseWorldPosition = _cursorLogic.GetWorldPosition(_mainCamera, rawMousePos);

        // 3. Disparo de Eventos (Opcional, mas mantido para compatibilidade)
        if (IsPrimaryButtonDown) OnPrimaryClick?.Invoke();

        DetectAnyInput();
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