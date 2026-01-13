using UnityEngine;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    [Header("Dependências")]
    private CardVisualConfig _config;
    private InputManager _inputManager;

    [Header("Componentes")]
    [SerializeField] private CardMovementController _movement; 
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;

    // ESTADO
    public CardVisualState CurrentState { get; private set; } = CardVisualState.Idle;
    public CardData Data { get; private set; }
    public CardInstance Instance { get; private set; }
    public bool IsHovered { get; private set; }

    // DADOS DE LAYOUT
    private HandLayoutCalculator.CardTransformTarget _layoutTarget;

    // CONTROLE DE INPUT
    private float _lastClickTime;
    private const float CLICK_DEBOUNCE = 0.15f;

    // EVENTOS
    public event Action<CardView> OnDragStartEvent;
    public event Action<CardView> OnDragEndEvent;
    public event Action<CardView> OnClickEvent;

    public void Initialize(CardData data, CardInstance instance, CardVisualConfig config, InputManager inputManager)
    {
        if (config == null || inputManager == null || _movement == null)
        {
            Debug.LogError($"[CardView] Dependências faltando em {gameObject.name}.");
            enabled = false;
            return;
        }

        Data = data;
        Instance = instance;
        _config = config;
        _inputManager = inputManager;

        // Inicializa o sub-sistema de física
        _movement.Initialize(config);

        if (Data != null)
        {
            _nameText.text = Data.Name;
            _artRenderer.sprite = Data.Icon;
        }

#if UNITY_EDITOR
        name = $"Card_{Data?.Name}_{Instance.UniqueID.Substring(0, 4)}";
#endif
    }

    private void Update()
    {
        // O CardView decide QUAL comportamento usar
        // O MovementController decide COMO executar esse comportamento

        switch (CurrentState)
        {
            case CardVisualState.Consuming:
                break;

            case CardVisualState.Dragging:
                // Nota: O Drag agora é atualizado pelo OnDragUpdate (Input driven), 
                // não passivamente no Update, mas se precisar de lógica extra, vai aqui.
                break;

            case CardVisualState.Idle:
                // Passamos apenas os dados necessários
                _movement.HandleIdle(_layoutTarget, IsHovered);
                break;

            case CardVisualState.Selected:
                Vector3 focusPoint = GetInteractionPoint(_layoutTarget.Position);
                _movement.HandleSelected(_layoutTarget, focusPoint);
                break;
        }
    }

    // --- DRAG ---

    public void OnDragStart()
    {
        if (AppCore.Instance.GameStateManager.CurrentState != GameState.Playing) return;
        CurrentState = CardVisualState.Dragging;
        IsHovered = false;
        OnDragStartEvent?.Invoke(this);
    }

    public void OnDragUpdate(Vector2 worldPos)
    {
        // Delega diretamente para a física
        _movement.HandleDrag(worldPos);
    }

    public void OnDragEnd()
    {
        CurrentState = CardVisualState.Idle;
        OnDragEndEvent?.Invoke(this);
    }

    // --- AUXILIARES E INPUT ---

    private Vector3 GetInteractionPoint(Vector3 fallback)
    {
        return _inputManager != null ? _inputManager.MouseWorldPosition : fallback;
    }

    private void PerformClick()
    {
        var app = AppCore.Instance;
        if (app != null && (app.GameStateManager.CurrentState == GameState.Paused || app.GameStateManager.CurrentState == GameState.GameOver)) return;

        if (Time.time - _lastClickTime < CLICK_DEBOUNCE) return;
        _lastClickTime = Time.time;

        if (CurrentState == CardVisualState.Dragging || CurrentState == CardVisualState.Consuming) return;
        OnClickEvent?.Invoke(this);
    }

    public void OnClick() => PerformClick();
    public void OnPointerClick(PointerEventData d) => PerformClick();

    public void UpdateLayoutTarget(HandLayoutCalculator.CardTransformTarget target) => _layoutTarget = target;
    public void OnHoverEnter() => IsHovered = true;
    public void OnHoverExit() => IsHovered = false;

    public void Select() { if (CanChangeState()) CurrentState = CardVisualState.Selected; }
    public void Deselect() { if (CanChangeState()) CurrentState = CardVisualState.Idle; }
    private bool CanChangeState() => CurrentState != CardVisualState.Dragging && CurrentState != CardVisualState.Consuming;
}