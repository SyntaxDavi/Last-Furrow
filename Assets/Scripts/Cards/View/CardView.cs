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
        // 1. INICIALIZAÇÃO (Base Layout)
        target = CardVisualTarget.Create(_baseLayoutTarget.Position, _baseLayoutTarget.Rotation, 1f);

        // Perfil Padrão
        profile = new CardMovementProfile
        {
            PositionSmoothTime = _config.PositionSmoothTime,
            RotationSpeed = _config.RotationSpeed,
            ScaleSmoothTime = _config.ScaleSmoothTime
        };
        sortOrder = _baseLayoutTarget.SortingOrder;

        // 2. DADOS DE ENTRADA (Input Injection)
        // Aqui decidimos QUEM influencia a carta. Hoje é o mouse.
        // Se fosse um replay, viria de um ReplayManager.
        Vector3 interactionPoint = _inputManager.MouseWorldPosition;
        float time = Time.time + _randomSeed;

        // 3. PIPELINE DE ESTADOS
        switch (CurrentState)
        {
            case CardVisualState.Idle:
                ApplyIdleFloat(ref target, time); // Apenas flutuação

                if (IsHovered)
                {
                    ApplyHoverFeedback(ref target, ref sortOrder); // Apenas Scale/Z/Y
                    ApplyDynamicTilt(ref target, interactionPoint); // Apenas Rotação 3D

                    // Opcional: Perfil mais ágil para o tilt responder rápido
                    profile.RotationSpeed = _config.TiltRotationSpeed;
                }
                break;

            case CardVisualState.Selected:
                ApplySelectedModifiers(ref target, ref sortOrder, interactionPoint);
                break;

            case CardVisualState.Dragging:
                // Drag tem pipeline próprio no OnDragUpdate, mas se quiser centralizar:
                ApplyDragVisuals(ref target, ref sortOrder, interactionPoint);
                break;
        }
    }

    // --- MODIFICADORES INTERNOS (Hardcoded comuns) ---
    // Em um futuro ideal, estes seriam classes ICardVisualModifier separadas,
    // mas mantê-los como métodos privados aqui é um bom balanço para agora.

    private void ApplyIdleFloat(ref CardVisualTarget target, float time)
    {
        float floatY = Mathf.Sin(time * _config.IdleFloatSpeed) * _config.IdleFloatAmount;
        float floatRot = Mathf.Cos(time * (_config.IdleFloatSpeed * 0.5f)) * _config.IdleRotationAmount;

        target.Position += Vector3.up * floatY;
        target.Rotation *= Quaternion.Euler(0, 0, floatRot);
        target.Position.z = _config.IdleZ;
    }
    private void ApplyHoverFeedback(ref CardVisualTarget target, ref int sortOrder)
    {
        target.Position += Vector3.up * _config.PeekYOffset;
        target.Position.z = _config.HoverZ; // Traz para frente
        target.Scale = Vector3.one * _config.PeekScale;
        sortOrder = CardSortingConstants.HOVER_LAYER;
    }
    private void ApplyDragVisuals(ref CardVisualTarget target, ref int sortOrder, Vector3 focusPoint)
    {
        // 1. A carta segue o foco (mouse)
        target.Position = new Vector3(focusPoint.x, focusPoint.y, _config.DragZ);

        // 2. Tilt Effect (Baseado na velocidade horizontal)
        // Comparamos onde queremos ir (target) com onde estamos fisicamente (transform)
        float deltaX = (target.Position.x - transform.position.x) * -2f;
        float tiltZ = Mathf.Clamp(deltaX * _config.DragTiltAmount, -_config.DragTiltAmount, _config.DragTiltAmount);

        target.Rotation = Quaternion.Euler(0, 0, tiltZ);

        // 3. Layer Máximo
        sortOrder = CardSortingConstants.DRAG_LAYER;
    }
    private void ApplyDynamicTilt(ref CardVisualTarget target, Vector3 focusPoint)
    {
        // 1. Cálculo Relativo (Sem Input Global)
        // Usamos target.Position (onde a carta VAI estar) para estabilidade
        float deltaX = focusPoint.x - target.Position.x;
        float deltaY = focusPoint.y - target.Position.y;

        // 2. Normalização (Sem Magic Numbers)
        // Normaliza de -1 a 1 baseado no raio de influência configurado
        float normalizedX = Mathf.Clamp(deltaX / _config.TiltInfluenceRadius, -1f, 1f);
        float normalizedY = Mathf.Clamp(deltaY / _config.TiltInfluenceRadius, -1f, 1f);

        // 3. Conversão para Ângulos
        // Mouse na direita (X+) -> Gira eixo Y negativo (ou positivo dependendo da câmera)
        float rotY = normalizedX * _config.TiltAngleMax;
        // Mouse em cima (Y+) -> Gira eixo X para trás
        float rotX = -normalizedY * _config.TiltAngleMax;

        // 4. Composição de Rotação (Sem destruir Z)
        // Criamos a rotação do tilt pura
        Quaternion tiltRotation = Quaternion.Euler(rotX, rotY, 0);

        // Multiplicamos pela rotação que JÁ existia no pipeline (Arco + Float)
        // A ordem importa: (Tilt * Original) aplica o tilt NO EIXO da carta original
        // (Original * Tilt) aplica o tilt NO EIXO DO MUNDO
        // Geralmente para UI, (Original * Tilt) sente mais natural.
        target.Rotation = target.Rotation * tiltRotation;
    }

    private void ApplySelectedModifiers(ref CardVisualTarget target, ref int sortOrder, Vector3 focusPoint)
    {
        Vector3 anchorPos = _baseLayoutTarget.Position + (Vector3.up * _config.SelectedYOffset);

        Vector3 dirToFocus = Vector3.ClampMagnitude(focusPoint - anchorPos, _config.MaxInteractionDistance);

        // Magnetismo
        target.Position = anchorPos + (dirToFocus * _config.MagneticPullStrength);
        target.Position.z = _config.SelectedZ;

        // Olhar para o foco
        float lookZ = -dirToFocus.x * _config.LookRotationStrength;
        target.Rotation = Quaternion.Euler(0, 0, lookZ);

        target.Scale = Vector3.one * _config.SelectedScale;
        sortOrder = CardSortingConstants.HOVER_LAYER;
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