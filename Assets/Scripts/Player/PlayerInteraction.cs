using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuração de Física")]
    [SerializeField] private LayerMask _draggableLayer;
    [SerializeField] private LayerMask _dropLayer;
    [SerializeField] private float _dragThresholdPx = 10f;

    [Header("Debug")]
    [SerializeField] private bool _debugMode = false;

    [Header("Configuração de Interação")]
    [Tooltip("Distância extra que o mouse pode se afastar antes de perder o hover (Evita tremedeira)")]
    [SerializeField] private float _hoverExitThreshold = 0.5f;

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
        if (_input == null) return;

        // 1. Contexto Atual
        var currentState = AppCore.Instance.GameStateManager.CurrentState;

        // Regra de Ouro:
        // - Drag: Só permitido em Playing (Produção)
        // - Click/Hover: Permitido em Playing E Shopping (para vender cartas)
        // - Pausa: Bloqueia tudo

        bool inputAllowed = (currentState == GameState.Playing || currentState == GameState.Shopping);
        bool dragAllowed = (currentState == GameState.Playing);

        if (!inputAllowed) return; // Bloqueio total (Pause / Game Over)

        Vector2 mouseScreenPos = _input.MouseScreenPosition;
        Vector2 mouseWorldPos = _input.MouseWorldPosition;

        if (_isDragging && _activeDrag != null)
        {
            HandleActiveDrag(mouseWorldPos);
        }
        else
        {
            HandleHover(mouseWorldPos);
            // Passamos o 'dragAllowed' para dentro do HandleInput
            HandleInput(mouseScreenPos, mouseWorldPos, dragAllowed);
        }
    }

    // --- LÓGICA DE INPUT (Clique e Início de Drag) ---

    private void HandleInput(Vector2 screenPos, Vector2 worldPos, bool dragAllowed)
    {
        // 1. Mouse Down: Identificar
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
                }
            }
        }

        // 2. Mouse Hold: Drag (SOMENTE SE PERMITIDO)
        if (dragAllowed && _potentialDrag != null && _input.IsPrimaryButtonHeld)
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

        // 3. Mouse Up: Clique (SEMPRE PERMITIDO SE INPUTALLOWED = TRUE)
        if (_input.IsPrimaryButtonUp)
        {
            if (_potentialDrag != null && !_isDragging)
            {
                // É um clique simples (Serve para selecionar no grid ou VENDER na loja)
                if (_potentialDrag is IInteractable interactable)
                {
                    interactable.OnClick();
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

    private void HandleHover(Vector2 worldPos)
    {
        // 1. Quem estamos hoverando AGORA (Raycast físico)
        LayerMask targetMask = _draggableLayer;
        if (AppCore.Instance.GameStateManager.CurrentState == GameState.Playing) targetMask |= _dropLayer;

        Collider2D[] hitColliders = Physics2D.OverlapPointAll(worldPos, targetMask);
        IInteractable candidate = null;
        int highestOrder = int.MinValue;

        // Encontra o melhor candidato (Rei da Montanha por Sorting Order)
        foreach (var col in hitColliders)
        {
            var interactable = col.GetComponent<IInteractable>();
            if (interactable == null) continue;

            // Ignora quem está sendo arrastado
            if (interactable is IDraggable drag && IsObjectDragging(drag)) continue;

            var group = col.GetComponent<UnityEngine.Rendering.SortingGroup>();
            int order = (group != null) ? group.sortingOrder : 0;

            if (order > highestOrder)
            {
                highestOrder = order;
                candidate = interactable;
            }
        }

        // --- A CORREÇÃO MÁGICA (STICKY HOVER) ---

        // Se já temos um hover ativo, damos prioridade para MANTER ele (Histerese)
        // Isso evita que a carta "fuja" do mouse quando ela se move ou inclina.
        if (_currentHover != null && candidate != _currentHover)
        {

            if (_currentHover is MonoBehaviour hoverMono && hoverMono != null)
            {
                Collider2D hoverCol = hoverMono.GetComponent<Collider2D>();
                if (hoverCol != null)
                {
                    // Calcula o ponto do collider mais próximo do mouse
                    Vector2 closestPoint = hoverCol.ClosestPoint(worldPos);
                    float distance = Vector2.Distance(worldPos, closestPoint);

                    // Se o mouse ainda está pertinho (ex: 0.5 unidades), NÃO troca.
                    // Mantém o hover antigo.
                    if (distance < _hoverExitThreshold)
                    {
                        candidate = _currentHover;
                    }
                }
            }
        }
        // ----------------------------------------

        // 3. Aplica a Lógica de Troca (Entrada/Saída)
        if (candidate != _currentHover)
        {
            if (_currentHover != null && IsObjectAlive(_currentHover)) _currentHover.OnHoverExit();

            _currentHover = candidate;

            if (_currentHover != null) _currentHover.OnHoverEnter();
        }
    }


    // --- UTILITÁRIOS ---

    // Helper para checar se interface Unity ainda existe (lidar com Destroy())

    private bool IsObjectDragging(IDraggable draggable)
    {
        return _isDragging && _activeDrag == draggable;
    }

    // Helper de segurança para Unity (checa se foi destruído)
    private bool IsObjectAlive(object obj)
    {
        return obj != null && !((obj is UnityEngine.Object unityObj) && unityObj == null);
    }

    private void Log(string msg)
    {
        if (_debugMode) Debug.Log($"[PlayerInteraction] {msg}");
    }
}