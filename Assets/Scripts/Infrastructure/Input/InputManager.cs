using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InputManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private CursorSettings _cursorSettings;

    private CursorLogic _cursorLogic;
    private Camera _currentCamera;

    private bool _useNewInputSystem;
    private bool _isInitialized = false;

    // --- LEITURA PÚBLICA ---
    public Vector2 MouseScreenPosition { get; private set; }
    public Vector2 MouseWorldPosition { get; private set; }

    public bool IsPrimaryButtonDown { get; private set; }
    public bool IsPrimaryButtonUp { get; private set; }
    public bool IsPrimaryButtonHeld { get; private set; }

    // Eventos puramente locais (sem dependência global)
    // Útil para UI ou Feedback Visual local
    public event System.Action OnPrimaryClick;
    public event System.Action OnAnyInputDetected;
    public event System.Action OnBackInput;

    public void Initialize()
    {
        if (_cursorSettings == null) _cursorSettings = new CursorSettings();
        _cursorLogic = new CursorLogic(_cursorSettings);

        DetermineInputStrategy();

        if (_currentCamera == null) _currentCamera = Camera.main;

        _isInitialized = true;
    }

    public void SetCamera(Camera newCamera)
    {
        _currentCamera = newCamera;
    }

    private void DetermineInputStrategy()
    {
#if ENABLE_INPUT_SYSTEM
        // Detecta UMA VEZ na inicialização.
        // Nota: Se houver hot-plug de mouse depois, pode precisar reiniciar ou ouvir eventos de device.
        _useNewInputSystem = Mouse.current != null;
#else
        _useNewInputSystem = false;
#endif
        Debug.Log($"[InputManager] Estratégia definida: {(_useNewInputSystem ? "New System" : "Legacy")}");
    }

    private void Update()
    {
        if (!_isInitialized) return;

        if (_useNewInputSystem) ReadNewInput();
        else ReadLegacyInput();

        CalculateWorldPosition();

        // Dispara eventos locais se necessário
        if (IsPrimaryButtonDown) OnPrimaryClick?.Invoke();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackInput?.Invoke();
        }

        DetectAnyInput();
    }

    private void ReadNewInput()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse == null) return;

        MouseScreenPosition = mouse.position.ReadValue();
        IsPrimaryButtonDown = mouse.leftButton.wasPressedThisFrame;
        IsPrimaryButtonUp = mouse.leftButton.wasReleasedThisFrame;
        IsPrimaryButtonHeld = mouse.leftButton.isPressed;
#endif
    }

    private void ReadLegacyInput()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        MouseScreenPosition = Input.mousePosition;
        IsPrimaryButtonDown = Input.GetMouseButtonDown(0);
        IsPrimaryButtonUp = Input.GetMouseButtonUp(0);
        IsPrimaryButtonHeld = Input.GetMouseButton(0);
#endif
    }

    private void CalculateWorldPosition()
    {
        if (_currentCamera != null)
        {
            MouseWorldPosition = _cursorLogic.GetWorldPosition(_currentCamera, MouseScreenPosition);
        }
    }

    private void DetectAnyInput()
    {
        bool anyInput = false;

        if (_useNewInputSystem)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) anyInput = true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) anyInput = true;
#endif
        }
        else
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.anyKeyDown) anyInput = true;
#endif
        }

        if (anyInput)
        {
            OnAnyInputDetected?.Invoke();
        }
    }
}