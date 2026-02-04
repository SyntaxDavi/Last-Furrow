using UnityEngine;
using LastFurrow.Visual.Camera;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InputManager : MonoBehaviour, ICameraInputProvider
{
    [Header("Configuracao")]
    [SerializeField] private CursorSettings _cursorSettings;

    private CursorLogic _cursorLogic;     
    private Camera _currentCamera;        

    private bool _useNewInputSystem;      
    private bool _isInitialized = false;  

    // --- LEITURA PUBLICA ---
    public Vector2 MouseScreenPosition { get; private set; }
    public Vector2 MouseWorldPosition { get; private set; }

    public bool IsPrimaryButtonDown { get; private set; }
    public bool IsPrimaryButtonUp { get; private set; }
    public bool IsPrimaryButtonHeld { get; private set; }

    // Eventos puramente locais (sem dependencia global)
    // Util para UI ou Feedback Visual local
    public event System.Action OnPrimaryClick;
    public event System.Action OnAnyInputDetected;
    public event System.Action OnBackInput;
    public event System.Action OnShopToggleInput;

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
        // Detecta UMA VEZ na inicializacao.
        // Nota: Se houver hot-plug de mouse depois, pode precisar reiniciar ou ouvir eventos de device.
        _useNewInputSystem = Mouse.current != null;
#else
        _useNewInputSystem = false;       
#endif
        Debug.Log($"[InputManager] Estrategia definida: {(_useNewInputSystem ? "New System" : "Legacy")}");
    }

    private void Update()
    {
        if (!_isInitialized) return;      

        if (_useNewInputSystem) ReadNewInput();
        else ReadLegacyInput();

        CalculateWorldPosition();

        // Dispara eventos locais se necessario
        if (IsPrimaryButtonDown) OnPrimaryClick?.Invoke();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackInput?.Invoke();        
        }

        if (Input.GetKeyDown(KeyCode.S))  
        {
            OnShopToggleInput?.Invoke();  
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

    // --- ICameraInputProvider Implementation ---
    
    public Vector2 LookIntent 
    {
        get
        {
            if (!_isInitialized) return Vector2.zero;
            // Normaliza posicao do mouse (-1 a 1) para a camera
            return new Vector2(
                (MouseScreenPosition.x / (float)Screen.width - 0.5f) * 2f,
                (MouseScreenPosition.y / (float)Screen.height - 0.5f) * 2f
            );
        }
    }

    public bool IsInputLocked => AppCore.Instance != null &&
                                 ((AppCore.Instance.GameStateManager != null && 
                                   (AppCore.Instance.GameStateManager.CurrentState == GameState.Shopping || 
                                    AppCore.Instance.GameStateManager.CurrentState == GameState.MainMenu ||
                                    AppCore.Instance.GameStateManager.CurrentState == GameState.Analyzing ||
                                    AppCore.Instance.GameStateManager.CurrentState == GameState.Paused)) ||
                                  (AppCore.Instance.ShopService != null && AppCore.Instance.ShopService.CurrentSession != null));
}
