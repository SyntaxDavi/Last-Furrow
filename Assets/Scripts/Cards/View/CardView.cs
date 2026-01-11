using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using System;
using TMPro;

public enum CardVisualState
{
    Idle,
    Selected, // Magnético
    Dragging,
    Consuming,
    // Futuro: Locked, Preview, Disabled...
}

[RequireComponent(typeof(SortingGroup))]
public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    [Header("Dependências")]
    private CardVisualConfig _config;
    private InputManager _inputManager;

    [Header("Referências")]
    [SerializeField] private SortingGroup _sortingGroup;
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;

    // ESTADO
    public CardVisualState CurrentState { get; private set; } = CardVisualState.Idle;
    public CardData Data { get; private set; }
    public CardInstance Instance { get; private set; }
    public bool IsHovered { get; private set; }

    // DADOS DE LAYOUT & FÍSICA
    private HandLayoutCalculator.CardTransformTarget _layoutTarget;
    private Vector3 _currentVelocityPos;
    private float _currentVelocityScale;
    private float _randomSeed;
    private bool _isMouseOver;

    // CONTROLE DE INPUT
    private float _lastClickTime;
    private const float CLICK_DEBOUNCE = 0.15f; // Aumentado levemente para segurança

    // EVENTOS
    public event Action<CardView> OnDragStartEvent;
    public event Action<CardView> OnDragEndEvent;
    public event Action<CardView> OnClickEvent;

    // --- INICIALIZAÇÃO ---
    public void Initialize(CardData data, CardInstance instance, CardVisualConfig config, InputManager inputManager)
    {
        // Fail Fast: Garante que dependencies existem
        if (config == null || inputManager == null)
        {
            Debug.LogError($"[CardView] Dependências críticas faltando no objeto {gameObject.name}. Desabilitando.");
            enabled = false;
            return;
        }

        Data = data;
        Instance = instance;
        _config = config;
        _inputManager = inputManager;

        // Setup Visual
        if (Data != null)
        {
            _nameText.text = Data.Name;
            _artRenderer.sprite = Data.Icon;
        }

        _randomSeed = UnityEngine.Random.Range(0f, 225f);

#if UNITY_EDITOR
        // Lógica de Debug apenas no editor para não poluir build
        name = $"Card_{Data?.Name}_{Instance.UniqueID.Substring(0, 4)}";
#endif
    }


    // --- LOOP PRINCIPAL (STATE MACHINE) ---
    private void Update()
    {
        // Em vez de "guardrails" com return, usamos um dispatcher claro
        switch (CurrentState)
        {
            case CardVisualState.Consuming:
                // Não faz nada, espera animação de morte terminar
                break;

            case CardVisualState.Dragging:
                // Física controlada pelo Input do Mouse (OnDragUpdate)
                break;

            case CardVisualState.Idle:
                UpdateIdleState();
                break;

            case CardVisualState.Selected:
                UpdateSelectedState();
                break;
        }
    }

    // --- LÓGICA POR ESTADO ---

    private void UpdateIdleState()
    {
        Vector3 targetPos = _layoutTarget.Position;
        Quaternion targetRot = _layoutTarget.Rotation;
        Vector3 targetScale = Vector3.one;
        int targetSort = _layoutTarget.SortingOrder;

        // 1. Aplica Flutuação (Balatro)
        ApplyFloatEffect(ref targetPos, ref targetRot);

        // 2. Modificadores de Hover (Peek)
        if (_isMouseOver)
        {
            targetPos += Vector3.up * _config.PeekYOffset;
            targetPos.z = _config.HoverZ;
            targetScale = Vector3.one * _config.PeekScale;
            targetSort = CardSortingConstants.HOVER_LAYER;
        }
        else
        {
            targetPos.z = _config.IdleZ;
        }

        ApplyPhysics(targetPos, targetRot, targetScale, targetSort, _config.PositionSmoothTime);
    }

    private void UpdateSelectedState()
    {
        // Posição Base (Ancorada no slot, mas subida)
        Vector3 anchorPos = _layoutTarget.Position + (Vector3.up * _config.SelectedYOffset);

        // Ponto de Foco (Abstração do Mouse)
        Vector3 focusPoint = GetInteractionPoint(anchorPos);

        // Vetor Direção
        Vector3 dirToFocus = focusPoint - anchorPos;
        dirToFocus = Vector3.ClampMagnitude(dirToFocus, _config.MaxInteractionDistance);

        // Efeito Magnético (Olhar e Puxar)
        float lookZ = -dirToFocus.x * _config.LookRotationStrength;
        Vector3 magneticOffset = dirToFocus * _config.MagneticPullStrength;

        // Definição dos Alvos
        Vector3 targetPos = anchorPos + magneticOffset;
        targetPos.z = _config.SelectedZ;

        // Nota: Ignoramos a rotação do arco (_layoutTarget.Rotation) propositalmente aqui
        // para dar destaque total à carta selecionada.
        Quaternion targetRot = Quaternion.Euler(0, 0, lookZ);

        Vector3 targetScale = Vector3.one * _config.SelectedScale;
        int targetSort = CardSortingConstants.HOVER_LAYER;

        ApplyPhysics(targetPos, targetRot, targetScale, targetSort, _config.PositionSmoothTime);
    }

    // --- CÁLCULOS AUXILIARES ---

    private void ApplyFloatEffect(ref Vector3 pos, ref Quaternion rot)
    {
        float time = Time.time + _randomSeed;
        float floatY = Mathf.Sin(time * _config.IdleFloatSpeed) * _config.IdleFloatAmount;
        float floatRot = Mathf.Cos(time * (_config.IdleFloatSpeed * 0.5f)) * _config.IdleRotationAmount;

        pos.y += floatY;
        rot *= Quaternion.Euler(0, 0, floatRot);
    }

    private void ApplyPhysics(Vector3 tPos, Quaternion tRot, Vector3 tScale, int tSort, float smoothTime)
    {
        transform.position = Vector3.SmoothDamp(transform.position, tPos, ref _currentVelocityPos, smoothTime);

        float newScale = Mathf.SmoothDamp(transform.localScale.x, tScale.x, ref _currentVelocityScale, _config.ScaleSmoothTime);
        transform.localScale = Vector3.one * newScale;

        transform.rotation = Quaternion.Slerp(transform.rotation, tRot, Time.deltaTime * _config.RotationSpeed);

        if (_sortingGroup.sortingOrder != tSort)
            _sortingGroup.sortingOrder = tSort;
    }

    // Abstração para o futuro (Gamepad/Touch)
    private Vector3 GetInteractionPoint(Vector3 fallback)
    {
        if (_inputManager != null)
        {
            return _inputManager.MouseWorldPosition;
        }
        return fallback;
    }

    // --- GESTÃO DE INPUT UNIFICADA ---

    private void PerformClick()
    {
        // Debounce Global
        if (Time.time - _lastClickTime < CLICK_DEBOUNCE) return;
        _lastClickTime = Time.time;

        // Bloqueio por Estado
        if (CurrentState == CardVisualState.Dragging || CurrentState == CardVisualState.Consuming) return;

        OnClickEvent?.Invoke(this);
    }

    // Interfaces de Entrada (Redirecionam para o PerformClick central)
    public void OnClick() => PerformClick(); // IInteractable (Physics)
    public void OnPointerClick(PointerEventData d) => PerformClick(); // IPointerClickHandler (UI)

    // --- ESTADOS EXTERNOS ---

    public void UpdateLayoutTarget(HandLayoutCalculator.CardTransformTarget target) => _layoutTarget = target;
    public void OnHoverEnter() => _isMouseOver = true;
    public void OnHoverExit() => _isMouseOver = false;

    public void Select()
    {
        if (CanChangeState()) CurrentState = CardVisualState.Selected;
    }

    public void Deselect()
    {
        if (CanChangeState()) CurrentState = CardVisualState.Idle;
    }

    private bool CanChangeState()
    {
        return CurrentState != CardVisualState.Dragging && CurrentState != CardVisualState.Consuming;
    }

    // --- DRAG (Sistema separado) ---

    public void OnDragStart()
    {
        CurrentState = CardVisualState.Dragging;
        _isMouseOver = false;
        IsHovered = false;
        _currentVelocityPos = Vector3.zero;
        _sortingGroup.sortingOrder = CardSortingConstants.DRAG_LAYER;
        OnDragStartEvent?.Invoke(this);
    }

    public void OnDragUpdate(Vector2 worldPos)
    {
        // Drag é físico e direto, bypass no sistema de SmoothDamp
        Vector3 targetPos = new Vector3(worldPos.x, worldPos.y, _config.DragZ);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f);

        // Tilt Calculation
        float deltaX = (targetPos.x - transform.position.x) * -2f;
        float tiltZ = Mathf.Clamp(deltaX * _config.DragTiltAmount, -_config.DragTiltAmount, _config.DragTiltAmount);

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, tiltZ), Time.deltaTime * _config.DragTiltSpeed);
    }

    public void OnDragEnd()
    {
        CurrentState = CardVisualState.Idle;
        OnDragEndEvent?.Invoke(this);
    }
}