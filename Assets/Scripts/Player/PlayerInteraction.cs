using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuração de Física")]
    [SerializeField] private LayerMask _draggableLayer;
    [SerializeField] private LayerMask _dropLayer;
    [SerializeField] private float _dragThresholdPx = 10f;

    [Header("Debug")]
    [SerializeField] private bool _debugMode = false;

    private InputManager _input;

    // Estado Geral
    private IInteractable _currentHover;

    // Estado de Drag & Drop
    private IDraggable _potentialDrag;
    private IDraggable _activeDrag;
    private IInteractable _currentDropHover; 
    private Vector2 _dragStartScreenPos;
    private bool _isDragging = false;

    // --- INICIALIZAÇÃO ---

    // Padrão Dependency Injection: Permite configurar sem depender do Singleton (bom para testes)
    public void Initialize(InputManager inputManager)
    {
        _input = inputManager;
        Log("PlayerInteraction inicializado via Injeção.");
    }

    private void Start()
    {
        // Fallback: Se não foi inicializado manualmente, tenta buscar no AppCore
        if (_input == null && AppCore.Instance != null)
        {
            _input = AppCore.Instance.InputManager;
        }

        if (_input == null)
        {
            Debug.LogError("[PlayerInteraction] InputManager não encontrado! O script será desabilitado.");
            this.enabled = false;
        }
    }

    // --- LOOP PRINCIPAL ---

    private void Update()
    {
        // Se o jogo estiver pausado ou input nulo, não faz nada
        if (_input == null) return;

        // (Opcional) Aqui você poderia checar: if (AppCore.Instance.GameStateManager.IsPaused) return;

        Vector2 mouseScreenPos = _input.MouseScreenPosition;
        Vector2 mouseWorldPos = _input.MouseWorldPosition;

        if (_isDragging && _activeDrag != null)
        {
            HandleActiveDrag(mouseWorldPos);
        }
        else
        {
            HandleHover(mouseWorldPos);
            HandleInput(mouseScreenPos, mouseWorldPos);
        }
    }

    // --- LÓGICA DE INPUT (Clique e Início de Drag) ---

    private void HandleInput(Vector2 screenPos, Vector2 worldPos)
    {
        // 1. Mouse Down: Identificar potencial alvo
        if (_input.IsPrimaryButtonDown)
        {
            Collider2D col = Physics2D.OverlapPoint(worldPos, _draggableLayer);

            if (col != null)
            {
                var draggable = col.GetComponent<IDraggable>();
                if (draggable != null)
                {
                    _potentialDrag = draggable;
                    _dragStartScreenPos = screenPos;
                    Log($"[Input] Potencial drag detectado: {col.name}");
                }
            }
        }

        // 2. Mouse Hold: Verificar threshold para iniciar Drag
        if (_potentialDrag != null && _input.IsPrimaryButtonHeld)
        {
            if (!_isDragging)
            {
                float dist = Vector2.Distance(screenPos, _dragStartScreenPos);
                if (dist > _dragThresholdPx)
                {
                    StartDrag();
                }
            }
        }

        // 3. Mouse Up: Clique simples ou cancelamento
        if (_input.IsPrimaryButtonUp)
        {
            if (_potentialDrag != null && !_isDragging)
            {
                // É um clique simples
                if (_potentialDrag is IInteractable interactable)
                {
                    interactable.OnClick();
                    Log("[Input] Clique processado.");
                }
            }
            _potentialDrag = null;
        }
    }

    // --- LÓGICA DE DRAG & DROP ---

    private void StartDrag()
    {
        _isDragging = true;
        _activeDrag = _potentialDrag;

        // Limpa hover antigo para evitar que fique "preso" visualmente enquanto arrasta
        if (_currentHover != null)
        {
            _currentHover.OnHoverExit();
            _currentHover = null;
        }

        _activeDrag.OnDragStart();
        Log("[Drag] Iniciado.");
    }

    private void HandleActiveDrag(Vector2 worldPos)
    {
        // Atualiza posição visual do objeto
        _activeDrag.OnDragUpdate(worldPos);

        // Verifica o que está embaixo para feedback visual (Ex: Slot brilhando)
        HandleDropZoneHover(worldPos);

        if (_input.IsPrimaryButtonUp)
        {
            FinishDrag(worldPos);
        }
    }

    private void HandleDropZoneHover(Vector2 worldPos)
    {
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IInteractable newDropHover = null;

        if (col != null)
        {
            // O Slot implementa tanto IDropTarget quanto IInteractable
            newDropHover = col.GetComponent<IInteractable>();
        }

        // Troca de estado de Hover da DropZone
        if (newDropHover != _currentDropHover)
        {
            // Saiu do anterior
            if (_currentDropHover != null && IsObjectAlive(_currentDropHover))
            {
                _currentDropHover.OnHoverExit();
            }

            _currentDropHover = newDropHover;

            // Entrou no novo
            if (_currentDropHover != null)
            {
                _currentDropHover.OnHoverEnter();
            }
        }
    }

    private void FinishDrag(Vector2 worldPos)
    {
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);
        IDropTarget target = null;

        if (col != null)
        {
            target = col.GetComponent<IDropTarget>();
        }

        // PERGUNTA CRUCIAL: O alvo aceita esse objeto?
        // Na nova arquitetura, o GridSlotView vai perguntar para o GridService via delegate
        if (target != null && target.CanReceive(_activeDrag))
        {
            Log($"[Drop] Sucesso em {col.name}");
            target.OnReceive(_activeDrag);
        }
        else
        {
            Log("[Drop] Cancelado ou alvo inválido.");
            // Opcional: Tocar som de "erro" ou voltar carta para a mão com animação
        }

        _activeDrag.OnDragEnd();

        // Limpeza final
        if (_currentDropHover != null && IsObjectAlive(_currentDropHover))
        {
            _currentDropHover.OnHoverExit();
            _currentDropHover = null;
        }

        _isDragging = false;
        _activeDrag = null;
        _potentialDrag = null;
    }

    // --- LÓGICA DE HOVER (Passivo/Mouse Over) ---

    private void HandleHover(Vector2 worldPos)
    {
        // Detecta objetos interagíveis OU drop zones (para mostrar tooltip, highlight, etc)
        LayerMask combinedMask = _draggableLayer | _dropLayer;
        Collider2D col = Physics2D.OverlapPoint(worldPos, combinedMask);

        IInteractable newHover = null;
        if (col != null)
        {
            newHover = col.GetComponent<IInteractable>();
        }

        if (newHover != _currentHover)
        {
            if (_currentHover != null && IsObjectAlive(_currentHover))
            {
                _currentHover.OnHoverExit();
            }

            _currentHover = newHover;

            if (_currentHover != null)
            {
                _currentHover.OnHoverEnter();
            }
        }
    }

    // --- UTILITÁRIOS ---

    // Helper para checar se interface Unity ainda existe (lidar com Destroy())
    private bool IsObjectAlive(object o)
    {
        return o != null && (o as UnityEngine.Object) != null;
    }

    private void Log(string msg)
    {
        if (_debugMode) Debug.Log($"[PlayerInteraction] {msg}");
    }
}