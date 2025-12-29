using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Camadas (Layers)")]
    [SerializeField] private LayerMask _draggableLayer; // layer "Draggable" aqui
    [SerializeField] private LayerMask _dropLayer;      // layer "DropZone" aqui

    [Header("Configuração")]
    [Tooltip("Distância em PIXELS para considerar arrasto")]
    [SerializeField] private float _dragThresholdPx = 10f; // 10 pixels é um bom padrão

    private InputManager _input;

    // Estado de Interação Geral
    private IInteractable _currentHover;

    // Estado de Drag
    private IDraggable _potentialDrag;
    private IDraggable _activeDrag;
    private Vector2 _dragStartScreenPos; // Guardamos em pixels agora
    private bool _isDragging = false;

    // Estado de Drop (Novo)
    private IInteractable _currentDropHover; // Slot que está brilhando durante o drag

    private void Start()
    {
        if (AppCore.Instance != null)
        {
            _input = AppCore.Instance.InputManager;
        }
        else
        {
            Debug.LogError("FATAL: AppCore ausente.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (_input == null) return;

        // Dados atuais
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

    private void HandleInput(Vector2 screenPos, Vector2 worldPos)
    {
        // 1. DOWN: Busca apenas na layer de DRAGGABLES
        if (_input.IsPrimaryButtonDown)
        {
            // Só queremos pegar cartas, ignoramos slots aqui
            Collider2D col = Physics2D.OverlapPoint(worldPos, _draggableLayer);

            if (col != null)
            {
                var draggable = col.GetComponent<IDraggable>();
                if (draggable != null)
                {
                    _potentialDrag = draggable;
                    _dragStartScreenPos = screenPos; // Salva pixel inicial
                }
            }
        }

        // 2. HOLD: Verifica threshold em PIXELS
        if (_potentialDrag != null && _input.IsPrimaryButtonHeld)
        {
            if (!_isDragging && Vector2.Distance(screenPos, _dragStartScreenPos) > _dragThresholdPx)
            {
                StartDrag();
            }
        }

        // 3. UP: Click ou Cancel
        if (_input.IsPrimaryButtonUp)
        {
            if (_potentialDrag != null && !_isDragging)
            {
                // Click simples (Interação Genérica)
                if (_potentialDrag is IInteractable interactable)
                {
                    interactable.OnClick();
                }
            }
            _potentialDrag = null;
        }
    }

    private void StartDrag()
    {
        _isDragging = true;
        _activeDrag = _potentialDrag;
        _activeDrag.OnDragStart();

        // Limpa o hover normal para não ficar "preso" na carta que estamos movendo
        if (_currentHover != null)
        {
            _currentHover.OnHoverExit();
            _currentHover = null;
        }
    }

    private void HandleActiveDrag(Vector2 worldPos)
    {
        // Atualiza posição da carta
        _activeDrag.OnDragUpdate(worldPos);

        HandleDropFeedback(worldPos);

        // Soltou?
        if (_input.IsPrimaryButtonUp)
        {
            FinishDrag(worldPos);
        }
    }

    private void HandleDropFeedback(Vector2 worldPos)
    {
        // Busca APENAS na layer de DROP ZONES (Slots)
        // Isso resolve o conflito: a carta (layer Draggable) não bloqueia o raio do slot.
        Collider2D col = Physics2D.OverlapPoint(worldPos, _dropLayer);

        IInteractable newDropHover = null;
        if (col != null) newDropHover = col.GetComponent<IInteractable>();

        if (newDropHover != _currentDropHover)
        {
            // Saiu do slot anterior
            if (_currentDropHover != null && (_currentDropHover as Object) != null)
            {
                _currentDropHover.OnHoverExit();
            }

            _currentDropHover = newDropHover;

            // Entrou no novo slot
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

        // 2. ORQUESTRAÇÃO DE LÓGICA (Aqui é o lugar certo!)
        // O Input diz: "Draggable X caiu no Target Y".
        if (target != null && target.CanReceive(_activeDrag))
        {
            // Drop válido: Executa a ação do jogo
            target.OnReceive(_activeDrag);

            // Opcional: Tocar som de sucesso
        }
        else
        {
            // Drop inválido: Tocar som de erro ou "swish" de retorno
        }

        // 3. Reseta o Visual da Carta
        // A carta não sabe se acertou ou errou, ela só sabe que o drag acabou.
        // Se acertou, o Slot (OnReceive) vai decidir se destrói a carta ou consome.
        // Se errou, o HandManager vai puxar ela de volta pra mão no próximo frame.
        _activeDrag.OnDragEnd();

        // 4. Limpeza de Estado Local
        if (_currentDropHover != null)
        {
            _currentDropHover.OnHoverExit();
            _currentDropHover = null;
        }

        _isDragging = false;
        _activeDrag = null;
        _potentialDrag = null;
    }

    // Hover Normal (Quando mouse está livre)
    private void HandleHover(Vector2 worldPos)
    {
        // Aqui queremos highlight tanto de cartas quanto de slots
        // Então combinamos as layers ou usamos uma layer genérica
        LayerMask combinedMask = _draggableLayer | _dropLayer;

        Collider2D col = Physics2D.OverlapPoint(worldPos, combinedMask);
        IInteractable newHover = null;

        if (col != null) newHover = col.GetComponent<IInteractable>();

        if (newHover != _currentHover)
        {
            if (_currentHover != null && (_currentHover as Object) != null)
                _currentHover.OnHoverExit();

            _currentHover = newHover;

            if (_currentHover != null)
                _currentHover.OnHoverEnter();
        }
    }
}