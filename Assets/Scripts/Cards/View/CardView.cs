using UnityEngine;
using UnityEngine.EventSystems;
using System;
using TMPro;
using System.Collections.Generic;

public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    [Header("Dependências")]
    private CardVisualConfig _config;
    private InputManager _inputManager;

    [Header("Componentes")]
    [SerializeField] private CardMovementController _movement;
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;
    public bool IsHovered { get; private set; }
    // --- ESTADO (STATE MACHINE) ---
    // A propriedade pública é apenas para leitura. Mudanças passam por SetState.
    public CardVisualState CurrentState { get; private set; } = CardVisualState.Idle;

    // --- DADOS ---
    public CardData Data { get; private set; }
    public CardInstance Instance { get; private set; }

    // Onde a carta "deveria" estar segundo o Layout Manager (A Verdade Estrutural)
    private HandLayoutCalculator.CardTransformTarget _baseLayoutTarget;

    // Sementes para randomização visual (evita que todas as cartas flutuem em sincronia)
    private float _randomSeed;

    // Lista de modificadores ativos (Extensibilidade)
    private List<ICardVisualModifier> _activeModifiers = new List<ICardVisualModifier>();

    // EVENTOS
    public event Action<CardView> OnDragStartEvent;
    public event Action<CardView> OnDragEndEvent;
    public event Action<CardView> OnClickEvent;

    // --- INICIALIZAÇÃO ---
    public void Initialize(CardData data, CardInstance instance, CardVisualConfig config, InputManager inputManager)
    {
        Data = data;
        Instance = instance;
        _config = config;
        _inputManager = inputManager;
        _randomSeed = UnityEngine.Random.Range(0f, 100f);

        if (Data != null)
        {
            _nameText.text = Data.Name;
            _artRenderer.sprite = Data.Icon;
        }

        // Força estado inicial limpo
        _movement.ResetPhysicsState();
        SetState(CardVisualState.Idle);
    }

    // --- LOOP PRINCIPAL (FRAME-BASED) ---
    private void Update()
    {
        // 1. Calcula o Target (Pipeline)
        CalculateVisualTarget(out CardVisualTarget target, out CardMovementProfile profile, out int sortOrder);

        // 2. Aplica ao Motor
        _movement.SetSortingOrder(sortOrder);
        _movement.MoveTo(target, profile);
    }

    // --- CORE: STATE MACHINE ---

    private void SetState(CardVisualState newState)
    {
        if (CurrentState == newState) return;

        // 1. Exit Logic (Limpeza)
        OnExitState(CurrentState);

        // 2. Troca
        CurrentState = newState;

        // 3. Enter Logic (Configuração)
        OnEnterState(newState);
    }

    private void OnEnterState(CardVisualState state)
    {
        switch (state)
        {
            case CardVisualState.Idle:
                // Pode adicionar som, partículas de "poof", etc.
                break;

            case CardVisualState.Dragging:
                // Reseta a física para garantir responsividade instantânea
                _movement.ResetPhysicsState();
                OnDragStartEvent?.Invoke(this);
                break;

            case CardVisualState.Consuming:
                // Exemplo do "Bug Futuro" resolvido:
                // Toca animação, espera X segundos e se destrói.
                // StartCoroutine(ConsumeSequence()); 
                break;
        }
    }

    private void OnExitState(CardVisualState state)
    {
        switch (state)
        {
            case CardVisualState.Dragging:
                OnDragEndEvent?.Invoke(this);
                break;
        }
    }

    // --- CORE: PIPELINE DE VISUALIZAÇÃO ---

    private void CalculateVisualTarget(out CardVisualTarget target, out CardMovementProfile profile, out int sortOrder)
    {
        // Passo 1: Definir a BASE (A Verdade)
        // No Drag, a verdade é o Mouse. No Idle, a verdade é o Layout.
        if (CurrentState == CardVisualState.Dragging)
        {
            Vector3 mousePos = _inputManager.MouseWorldPosition;
            // DragZ ganha de tudo
            target = CardVisualTarget.Create(new Vector3(mousePos.x, mousePos.y, _config.DragZ), Quaternion.identity, 1f);

            // Drag precisa ser rápido e responsivo
            profile = new CardMovementProfile
            {
                PositionSmoothTime = 0.05f,
                RotationSpeed = _config.DragTiltSpeed,
                ScaleSmoothTime = 0.1f
            };
            sortOrder = CardSortingConstants.DRAG_LAYER;
        }
        else
        {
            // Default: Usa o Layout calculado pelo HandManager
            target = CardVisualTarget.Create(_baseLayoutTarget.Position, _baseLayoutTarget.Rotation, 1f);

            // Default Profile
            profile = new CardMovementProfile
            {
                PositionSmoothTime = _config.PositionSmoothTime,
                RotationSpeed = _config.RotationSpeed,
                ScaleSmoothTime = _config.ScaleSmoothTime
            };
            sortOrder = _baseLayoutTarget.SortingOrder;
        }

        // Passo 2: Aplicar Modificadores Baseados em Estado
        // (Aqui substituímos o antigo "Switch" gigante por chamadas semânticas)

        float time = Time.time + _randomSeed;

        switch (CurrentState)
        {
            case CardVisualState.Idle:
                ApplyIdleModifiers(ref target, time);

                // Hover é um sub-estado do Idle visualmente
                if (IsHovered)
                {
                    ApplyHoverModifiers(ref target, ref sortOrder);
                }
                break;

            case CardVisualState.Selected:
                ApplySelectedModifiers(ref target, ref sortOrder);
                break;

            case CardVisualState.Dragging:
                ApplyDragModifiers(ref target); // Tilt effect
                break;
        }

        // Passo 3: Aplicar Modificadores Genéricos (Ex: Buffs, Debuffs, Status Effects)
        // Isso permite que sistemas externos afetem a carta sem sujar este switch
        foreach (var modifier in _activeModifiers)
        {
            modifier.Apply(ref target, _config, time);
        }
    }

    // --- MODIFICADORES INTERNOS (Hardcoded comuns) ---
    // Em um futuro ideal, estes seriam classes ICardVisualModifier separadas,
    // mas mantê-los como métodos privados aqui é um bom balanço para agora.

    private void ApplyIdleModifiers(ref CardVisualTarget target, float time)
    {
        // Float Effect
        float floatY = Mathf.Sin(time * _config.IdleFloatSpeed) * _config.IdleFloatAmount;
        float floatRot = Mathf.Cos(time * (_config.IdleFloatSpeed * 0.5f)) * _config.IdleRotationAmount;

        target.Position += Vector3.up * floatY;
        target.Rotation *= Quaternion.Euler(0, 0, floatRot);
        target.Position.z = _config.IdleZ;
    }

    private void ApplyHoverModifiers(ref CardVisualTarget target, ref int sortOrder)
    {
        target.Position += Vector3.up * _config.PeekYOffset;
        target.Position.z = _config.HoverZ;
        target.Scale = Vector3.one * _config.PeekScale;
        sortOrder = CardSortingConstants.HOVER_LAYER;
    }

    private void ApplySelectedModifiers(ref CardVisualTarget target, ref int sortOrder)
    {
        Vector3 anchorPos = _baseLayoutTarget.Position + (Vector3.up * _config.SelectedYOffset);
        Vector3 mousePos = _inputManager.MouseWorldPosition;
        Vector3 dirToMouse = Vector3.ClampMagnitude(mousePos - anchorPos, _config.MaxInteractionDistance);

        // Magnetismo
        target.Position = anchorPos + (dirToMouse * _config.MagneticPullStrength);
        target.Position.z = _config.SelectedZ;

        // Olhar para o mouse
        float lookZ = -dirToMouse.x * _config.LookRotationStrength;
        target.Rotation = Quaternion.Euler(0, 0, lookZ);

        target.Scale = Vector3.one * _config.SelectedScale;
        sortOrder = CardSortingConstants.HOVER_LAYER;
    }

    private void ApplyDragModifiers(ref CardVisualTarget target)
    {
        // Tilt baseado no movimento horizontal (necessita comparar com posição atual real)
        float deltaX = (target.Position.x - transform.position.x) * -2f; // -2f é magic number de sensibilidade
        float tiltZ = Mathf.Clamp(deltaX * _config.DragTiltAmount, -_config.DragTiltAmount, _config.DragTiltAmount);

        target.Rotation = Quaternion.Euler(0, 0, tiltZ);
    }

    // --- API DE INPUT & CONTROLE ---

    public void OnDragStart()
    {
        if (CanChangeState()) SetState(CardVisualState.Dragging);
    }

    public void OnDragUpdate(Vector2 worldPos)
    {
        // O Update() já cuida de ler o InputManager e posicionar no mouse se o estado for Dragging.
        // Nenhuma lógica necessária aqui, o PlayerInteraction apenas garante que chama.
    }

    public void OnDragEnd()
    {
        if (CurrentState == CardVisualState.Dragging) SetState(CardVisualState.Idle);
    }

    public void OnClick() => PerformClick();
    public void OnPointerClick(PointerEventData d) => PerformClick();

    private void PerformClick()
    {
        if (AppCore.Instance.GameStateManager.CurrentState != GameState.Playing) return;
        if (CurrentState == CardVisualState.Dragging || CurrentState == CardVisualState.Consuming) return;

        OnClickEvent?.Invoke(this);
    }

    public void OnHoverEnter() => IsHovered = true;
    public void OnHoverExit() => IsHovered = false;

    public void Select()
    {
        if (CanChangeState()) SetState(CardVisualState.Selected);
    }

    public void Deselect()
    {
        // Se estiver arrastando, não força Idle, espera o Drag acabar
        if (CurrentState == CardVisualState.Selected) SetState(CardVisualState.Idle);
    }

    private bool CanChangeState()
    {
        // Regras de bloqueio de transição
        return CurrentState != CardVisualState.Consuming;
    }

    // --- API DE DADOS ---
    public void UpdateLayoutTarget(HandLayoutCalculator.CardTransformTarget target)
    {
        _baseLayoutTarget = target;
    }
}