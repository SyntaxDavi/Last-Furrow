using UnityEngine;

/// <summary>
/// Orquestrador de interações do jogador.
/// 
/// Responsabilidade: Coordenar os sistemas de input (Hover, Drag, Click).
/// NÃO contém lógica de negócio - delega para sistemas especializados.
/// 
/// Arquitetura:
/// - InteractionPolicy: Decide O QUE pode ser feito (regras de estado)
/// - HoverSystem: Gerencia hover com prioridade e histerese
/// - DragDropSystem: Gerencia arrastar e soltar
/// - ClickSystem: Gerencia cliques simples
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuração de Física")]
    [SerializeField] private LayerMask _draggableLayer;
    [SerializeField] private LayerMask _dropLayer;
    [SerializeField] private LayerMask _clickableLayer;
    [SerializeField] private float _dragThresholdPx = 10f;

    [Header("Debug")]
    [SerializeField] private bool _debugMode = false;

    [Header("Configuração de Hover")]
    [Tooltip("Distância extra que o mouse pode se afastar antes de perder o hover")]
    [SerializeField] private float _hoverExitThreshold = 0.5f;
    [Tooltip("Executa hover check a cada N frames")]
    [SerializeField] private int _hoverCheckInterval = 2;

    // Dependências
    private InputManager _input;
    
    // Sistemas
    private InteractionPolicy _policy;
    private HoverSystem _hoverSystem;
    private DragDropSystem _dragSystem;
    private ClickSystem _clickSystem;

    // --- INICIALIZAÇÃO ---

    public void Initialize(InputManager inputManager)
    {
        _input = inputManager;
        Log("PlayerInteraction inicializado via Injeção.");
    }

    private void Start()
    {
        // Fallback para AppCore
        if (_input == null && AppCore.Instance != null)
        {
            _input = AppCore.Instance.InputManager;
        }

        if (_input == null)
        {
            Debug.LogError("[PlayerInteraction] InputManager não encontrado! O script será desabilitado.");
            enabled = false;
            return;
        }

        // Inicializa sistemas
        InitializeSystems();
    }

    private void InitializeSystems()
    {
        var gameState = AppCore.Instance.GameStateManager;
        
        _policy = new InteractionPolicy(gameState);
        _hoverSystem = new HoverSystem(_hoverExitThreshold, _hoverCheckInterval);
        _dragSystem = new DragDropSystem(_dragThresholdPx, _dropLayer);
        
        // ClickableLayer inclui draggables + qualquer coisa clicável
        LayerMask fullClickLayer = _draggableLayer | _clickableLayer;
        _clickSystem = new ClickSystem(fullClickLayer);
        
        Log("Sistemas inicializados.");
    }

    // --- LOOP PRINCIPAL ---

    private void Update()
    {
        if (_input == null || _policy == null) return;

        // Early-out: Nada permitido (pause, game over, etc)
        if (!_policy.IsInputAllowed())
        {
            // Se estava arrastando, cancela
            if (_dragSystem.IsDragging)
            {
                _dragSystem.CancelDrag();
                Log("[Drag] Cancelado - estado do jogo mudou.");
            }
            return;
        }

        Vector2 screenPos = _input.MouseScreenPosition;
        Vector2 worldPos = _input.MouseWorldPosition;

        if (_dragSystem.IsDragging)
        {
            ProcessActiveDrag(worldPos);
        }
        else
        {
            ProcessHover(worldPos);
            ProcessInput(screenPos, worldPos);
        }
    }

    // --- PROCESSAMENTO ---

    private void ProcessHover(Vector2 worldPos)
    {
        if (!_policy.CanHover()) return;
        
        // Monta a máscara baseada no estado
        LayerMask hoverMask = _draggableLayer | _clickableLayer;
        if (_policy.ShouldIncludeDropLayerInHover())
        {
            hoverMask |= _dropLayer;
        }
        
        _hoverSystem.Update(worldPos, hoverMask, _dragSystem.ActiveDrag);
    }

    private void ProcessInput(Vector2 screenPos, Vector2 worldPos)
    {
        // Mouse Down
        if (_input.IsPrimaryButtonDown)
        {
            HandleMouseDown(worldPos, screenPos);
        }

        // Mouse Hold (tentativa de drag)
        if (_input.IsPrimaryButtonHeld && _policy.CanDrag())
        {
            if (_dragSystem.ShouldStartDrag(screenPos))
            {
                StartDrag();
            }
        }

        // Mouse Up
        if (_input.IsPrimaryButtonUp)
        {
            HandleMouseUp(worldPos);
        }
    }

    private void HandleMouseDown(Vector2 worldPos, Vector2 screenPos)
    {
        // Registra potencial drag
        Collider2D col = Physics2D.OverlapPoint(worldPos, _draggableLayer);
        if (col != null)
        {
            var draggable = col.GetComponent<IDraggable>();
            if (draggable != null)
            {
                _dragSystem.RegisterPotentialDrag(draggable, screenPos);
            }
        }
        
        // Registra potencial click
        if (_policy.CanClick())
        {
            _clickSystem.RegisterClickTarget(worldPos);
        }
    }

    private void HandleMouseUp(Vector2 worldPos)
    {
        // Se não arrastou, tenta clicar
        if (!_dragSystem.IsDragging && _policy.CanClick())
        {
            if (_clickSystem.TryExecuteClick(worldPos))
            {
                Log("[Click] Executado.");
            }
        }
        
        _dragSystem.ClearPotentialDrag();
        _clickSystem.Clear();
    }

    private void StartDrag()
    {
        // Limpa hover antes de arrastar
        _hoverSystem.ClearHover();
        _clickSystem.Clear();
        
        _dragSystem.StartDrag();
        Log("[Drag] Iniciado.");
    }

    private void ProcessActiveDrag(Vector2 worldPos)
    {
        _dragSystem.UpdateDrag(worldPos);

        if (_input.IsPrimaryButtonUp)
        {
            var result = _dragSystem.FinishDrag(worldPos);
            Log(result.Success ? "[Drop] Sucesso." : "[Drop] Cancelado.");
        }
    }

    // --- API PÚBLICA ---

    /// <summary>
    /// Limpa todos os caches. Chamar quando objetos são destruídos em massa.
    /// </summary>
    public void ClearCaches()
    {
        _hoverSystem?.ClearCache();
        Log("[Cache] Limpo.");
    }

    // --- UTILITÁRIOS ---

    private void Log(string msg)
    {
        if (_debugMode) Debug.Log($"[PlayerInteraction] {msg}");
    }
}