using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Camadas (Layers)")]
    [SerializeField] private LayerMask _draggableLayer;
    [SerializeField] private LayerMask _dropLayer;

    [Header("Configuração")]
    [Tooltip("Distância em PIXELS para considerar arrasto")]
    [SerializeField] private float _dragThresholdPx = 10f;

    private InputManager _input;

    // Estado de Interação Geral
    private IInteractable _currentHover;

    // Estado de Drag
    private IDraggable _potentialDrag;
    private IDraggable _activeDrag;
    private Vector2 _dragStartScreenPos;
    private bool _isDragging = false;

    // Estado de Drop
    private IInteractable _currentDropHover;

    private void Start()
    {
        if (AppCore.Instance != null)
        {
            _input = AppCore.Instance.InputManager;
            Debug.Log("[INIT] PlayerInteraction inicializado com InputManager.");
        }
        else
        {
            Debug.LogError("[INIT - FATAL] AppCore ausente. Script desativado.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (_input == null)
        {
            Debug.LogWarning("[UPDATE] InputManager é null. Update abortado.");
            return;
        }

        Vector2 mouseScreenPos = _input.MouseScreenPosition;
        Vector2 mouseWorldPos = _input.MouseWorldPosition;

        if (_isDragging && _activeDrag != null)
        {
            Debug.Log($"[DRAG] Atualizando drag ativo em {mouseWorldPos}");
            HandleActiveDrag(mouseWorldPos);
        }
        else
        {
            HandleHover(mouseWorldPos);
            HandleInput(mouseScreenPos, mouseWorldPos);
        }
    }

    private void HandleInput(Vector2 screenPos, Vector2 worldPos)
    {
        // DOWN
        if (_input.IsPrimaryButtonDown)
        {
            Debug.Log($"[INPUT] Mouse DOWN em {worldPos}");

            Collider2D col = Physics2D.OverlapPoint(worldPos, _draggableLayer);

            if (col != null)
            {
                Debug.Log($"[INPUT] Collider detectado (Draggable Layer): {col.name}");

                var draggable = col.GetComponent<IDraggable>();
                if (draggable != null)
                {
                    _potentialDrag = draggable;
                    _dragStartScreenPos = screenPos;
                    Debug.Log("[DRAG] PotentialDrag definido.");
                }
                else
                {
                    Debug.LogWarning("[DRAG] Collider não possui IDraggable.");
                }
            }
            else
            {
                Debug.Log("[INPUT] Nenhum draggable sob o mouse.");
            }
        }

        // HOLD
        if (_potentialDrag != null && _input.IsPrimaryButtonHeld)
        {
            float dist = Vector2.Distance(screenPos, _dragStartScreenPos);
            Debug.Log($"[DRAG] Hold detectado. Distância: {dist}");

            if (!_isDragging && dist > _dragThresholdPx)
            {
                Debug.Log("[DRAG] Threshold atingido. Iniciando drag.");
                StartDrag();
            }
        }

        // UP
        if (_input.IsPrimaryButtonUp)
        {
            Debug.Log("[INPUT] Mouse UP");

            if (_potentialDrag != null && !_isDragging)
            {
                Debug.Log("[CLICK] Click simples detectado.");

                if (_potentialDrag is IInteractable interactable)
                {
                    interactable.OnClick();
                    Debug.Log("[CLICK] OnClick executado.");
                }
            }

            _potentialDrag = null;
        }
    }

    private void StartDrag()
    {
        _isDragging = true;
        _activeDrag = _potentialDrag;

        Debug.Log("[DRAG] Drag iniciado.");
        _activeDrag.OnDragStart();

        if (_currentHover != null)
        {
            Debug.Log("[HOVER] Limpando hover anterior ao iniciar drag.");
            _currentHover.OnHoverExit();
            _currentHover = null;
        }
    }

    private void HandleActiveDrag(Vector2 worldPos)
    {
        _activeDrag.OnDragUpdate(worldPos);

        HandleDropFeedback(worldPos);

        if (_input.IsPrimaryButtonUp)
        {
            Debug.Log("[DRAG] Mouse solto durante drag.");
            FinishDrag(worldPos);
        }
    }

    private void HandleDropFeedback(Vector2 worldPos)
    {
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IInteractable newDropHover = null;

        if (col != null)
        {
            newDropHover = col.GetComponent<IInteractable>();
            Debug.Log($"[DROP] Hover sobre DropZone: {col.name}");
        }

        if (newDropHover != _currentDropHover)
        {
            if (_currentDropHover != null && (_currentDropHover as Object) != null)
            {
                Debug.Log("[DROP] Saindo do DropZone anterior.");
                _currentDropHover.OnHoverExit();
            }

            _currentDropHover = newDropHover;

            if (_currentDropHover != null)
            {
                Debug.Log("[DROP] Entrando em novo DropZone.");
                _currentDropHover.OnHoverEnter();
            }
        }
    }

    private void FinishDrag(Vector2 worldPos)
    {
        Debug.Log("[DRAG] Finalizando drag.");

        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IDropTarget target = null;

        if (col != null)
        {
            target = col.GetComponent<IDropTarget>();
            Debug.Log($"[DROP] Collider encontrado no drop: {col.name}");
        }
        else
        {
            Debug.Log("[DROP] Nenhum DropZone sob o mouse.");
        }

        if (target != null && target.CanReceive(_activeDrag))
        {
            Debug.Log("[DROP] Drop válido. Executando OnReceive.");
            target.OnReceive(_activeDrag);
        }
        else
        {
            Debug.LogWarning("[DROP] Drop inválido ou target não aceita.");
        }

        _activeDrag.OnDragEnd();
        Debug.Log("[DRAG] OnDragEnd executado.");

        if (_currentDropHover != null)
        {
            _currentDropHover.OnHoverExit();
            _currentDropHover = null;
        }

        _isDragging = false;
        _activeDrag = null;
        _potentialDrag = null;

        Debug.Log("[STATE] Estado de drag limpo.");
    }

    private void HandleHover(Vector2 worldPos)
    {
        LayerMask combinedMask = _draggableLayer | _dropLayer;
        Collider2D col = Physics2D.OverlapPoint(worldPos, combinedMask);

        IInteractable newHover = null;
        if (col != null)
        {
            newHover = col.GetComponent<IInteractable>();
        }

        if (newHover != _currentHover)
        {
            if (_currentHover != null && (_currentHover as Object) != null)
            {
                Debug.Log("[HOVER] Saindo do hover atual.");
                _currentHover.OnHoverExit();
            }

            _currentHover = newHover;

            if (_currentHover != null)
            {
                Debug.Log($"[HOVER] Entrando em hover: {(col != null ? col.name : "null")}");
                _currentHover.OnHoverEnter();
            }
        }
    }
}
