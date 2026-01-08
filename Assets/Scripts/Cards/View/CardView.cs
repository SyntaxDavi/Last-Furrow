using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using System;
using TMPro;

public enum CardVisualState
{
    Idle,
    Hover,
    Dragging,
    Consuming
}

[RequireComponent(typeof(SortingGroup))]
public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    [Header("Configuração Injetada")]
    private CardVisualConfig _visualConfig;

    [Header("Referências")]
    [SerializeField] private SortingGroup _sortingGroup;
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;

    // --- ESTADO & DADOS ---
    public CardVisualState CurrentState { get; private set; } = CardVisualState.Idle;
    public CardData Data { get; private set; }
    public CardInstance Instance { get; private set; }

    // Alvo calculado pelo Manager
    private HandLayoutCalculator.CardTransformTarget _layoutTarget;

    // Variáveis de Física (SmoothDamp)
    private Vector3 _currentVelocityPos;
    private float _currentVelocityScale;

    // Controle de Input (Debounce)
    private float _lastClickTime;
    private const float CLICK_DEBOUNCE = 0.1f;

    // --- EVENTOS (Locais, não estáticos) ---
    public event Action<CardView> OnDragStartEvent;
    public event Action<CardView> OnDragEndEvent;
    public event Action<CardView> OnClickEvent;

    // --- INICIALIZAÇÃO ---
    public void Initialize(CardData data, CardInstance instance, CardVisualConfig visualConfig)
    {
        Data = data;
        Instance = instance;
        _visualConfig = visualConfig;

        if (Data != null)
        {
            _nameText.text = Data.Name;
            _artRenderer.sprite = Data.Icon;
        }

        // Nome único para debug na hierarquia
        name = $"Card_{Data?.Name}_{Instance.UniqueID.Substring(0, 4)}";
    }

    // --- LOOP PRINCIPAL ---
    private void Update()
    {
        if (CurrentState == CardVisualState.Consuming) return;

        // Em Drag, a física é controlada pelo OnDragUpdate (Inputs do mouse)
        // O Update apenas garante a escala correta ou efeitos secundários
        if (CurrentState == CardVisualState.Dragging) return;

        UpdateMotionLogic();
    }

    private void UpdateMotionLogic()
    {
        // 1. Determinar Alvos baseados no Estado
        Vector3 finalPos = _layoutTarget.Position;
        Quaternion finalRot = _layoutTarget.Rotation;
        Vector3 finalScale = Vector3.one;
        int finalSort = _layoutTarget.SortingOrder;

        if (CurrentState == CardVisualState.Hover)
        {
            finalPos += Vector3.up * _visualConfig.HoverYOffset;
            finalPos.z = -2f; // Levemente para frente
            finalRot = Quaternion.identity; // Endireita a carta
            finalScale = Vector3.one * _visualConfig.HoverScale;
            finalSort = CardSortingConstants.HOVER_LAYER;
        }

        // 2. Aplicar Movimento Suave (SmoothDamp)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalPos,
            ref _currentVelocityPos,
            _visualConfig.PositionSmoothTime
        );

        // 3. Aplicar Escala Suave
        float newScale = Mathf.SmoothDamp(
            transform.localScale.x,
            finalScale.x,
            ref _currentVelocityScale,
            _visualConfig.ScaleSmoothTime
        );
        transform.localScale = Vector3.one * newScale;

        // 4. Rotação (Lerp/Slerp é melhor que SmoothDamp para quatermions)
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRot,
            Time.deltaTime * _visualConfig.RotationSpeed
        );

        // 5. Sorting (Imediato)
        if (_sortingGroup.sortingOrder != finalSort)
            _sortingGroup.sortingOrder = finalSort;
    }

    // --- INPUTS DO HAND MANAGER ---
    public void UpdateLayoutTarget(HandLayoutCalculator.CardTransformTarget target)
    {
        _layoutTarget = target;
    }

    // --- INTERAÇÃO (HOVER) ---
    public void OnHoverEnter()
    {
        if (CurrentState != CardVisualState.Dragging)
            CurrentState = CardVisualState.Hover;
    }

    public void OnHoverExit()
    {
        if (CurrentState != CardVisualState.Dragging)
            CurrentState = CardVisualState.Idle;
    }

    // --- INTERAÇÃO (DRAG) ---
    public void OnDragStart()
    {
        CurrentState = CardVisualState.Dragging;

        // ZERAR VELOCIDADES: Crítico para evitar "overshoot" quando soltar ou pegar
        _currentVelocityPos = Vector3.zero;
        _currentVelocityScale = 0f;

        _sortingGroup.sortingOrder = CardSortingConstants.DRAG_LAYER;
        OnDragStartEvent?.Invoke(this);
    }

    public void OnDragUpdate(Vector2 worldPos)
    {
        // Movimento
        Vector3 targetPos = new Vector3(worldPos.x, worldPos.y, _visualConfig.DragZDepth);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f); // Hard follow suavizado

        // Tilt Dinâmico (Feeling)
        float deltaX = (targetPos.x - transform.position.x) * -2f; // Inverte para drag physics
        float tiltZ = Mathf.Clamp(deltaX * _visualConfig.DragTiltAmount, -_visualConfig.DragTiltAmount, _visualConfig.DragTiltAmount);

        Quaternion targetRot = Quaternion.Euler(0, 0, tiltZ);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _visualConfig.DragTiltSpeed);
    }

    public void OnDragEnd()
    {
        // Zera novamente para garantir pouso suave no arco
        _currentVelocityPos = Vector3.zero;

        CurrentState = CardVisualState.Idle;
        OnDragEndEvent?.Invoke(this);
    }

    // --- INTERAÇÃO (CLICK) ---
    // Unifica UI Click e Physics Click com Debounce
    private void TryClick()
    {
        if (CurrentState == CardVisualState.Dragging) return;
        if (Time.time - _lastClickTime < CLICK_DEBOUNCE) return;

        _lastClickTime = Time.time;
        OnClickEvent?.Invoke(this);
    }

    public void OnClick() => TryClick(); // IInteractable (Physics Raycast)
    public void OnPointerClick(PointerEventData eventData) => TryClick(); // IPointerClickHandler (UI Raycast)
}