using UnityEngine;
using UnityEngine.EventSystems;
using System;
using TMPro;
using System.Collections.Generic;

public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    // ==============================================================================================
    // 1. INSPECTOR & DEPENDÊNCIAS
    // ==============================================================================================
    [Header("Referências Externas")]
    [SerializeField] private CardMovementController _movement;
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;

    [Header("Interação")]
    [SerializeField] private int _interactionPriority = 100; // Cartas = 100 (acima do grid)

    private CardVisualConfig _config;
    private InputManager _inputManager;

    // ==============================================================================================
    // 2. ESTADO PÚBLICO
    // ==============================================================================================
    public CardVisualState CurrentState { get; private set; } = CardVisualState.Idle;
    public CardData Data { get; private set; }
    public CardInstance Instance { get; private set; }
    public bool IsHovered { get; private set; }
    
    // IInteractable
    public int InteractionPriority => _interactionPriority;

    // ==============================================================================================
    // 3. ESTADO INTERNO (Runtime)
    // ==============================================================================================
    // Layout
    private HandLayoutCalculator.CardTransformTarget _baseLayoutTarget;
    private float _randomSeed;

    // Efeitos Visuais (Juice)
    private float _currentSquashVal = 0f;

    // Extensibilidade
    private List<ICardVisualModifier> _activeModifiers = new List<ICardVisualModifier>();

    // ==============================================================================================
    // 4. EVENTOS
    // ==============================================================================================
    public event Action<CardView> OnDragStartEvent;
    public event Action<CardView> OnDragEndEvent;
    public event Action<CardView> OnClickEvent;

    // ==============================================================================================
    // CICLO DE VIDA & INICIALIZAÇÃO
    // ==============================================================================================
    public void Initialize(CardData data, CardInstance instance, CardVisualConfig config, InputManager inputManager)
    {
        // Injeção de Dependência
        Data = data;
        Instance = instance;
        _config = config;
        _inputManager = inputManager;

        // Setup Inicial
        _randomSeed = UnityEngine.Random.Range(0f, 100f);
        _movement.ResetPhysicsState();

        // Setup Visual
        if (Data != null)
        {
            _nameText.text = Data.Name;
            _artRenderer.sprite = Data.Icon;
        }
        else
        {
            Debug.LogWarning($"[CardView] Inicializado com Data nula: {gameObject.name}");
        }

        SetState(CardVisualState.Idle);
    }

    private void Update()
    {
        if (_config == null) return;
        CalculateAndApplyVisuals();
    }

    // ==============================================================================================
    // PIPELINE VISUAL (O Coração da Renderização)
    // ==============================================================================================
    private void CalculateAndApplyVisuals()
    {
        CalculateVisualTarget(out CardVisualTarget target, out CardMovementProfile profile, out int sortOrder);

        _movement.SetSortingOrder(sortOrder);
        _movement.MoveTo(target, profile, Time.deltaTime);
    }

    private void CalculateVisualTarget(out CardVisualTarget target, out CardMovementProfile profile, out int sortOrder)
    {
        // 1. INICIALIZAÇÃO
        target = CardVisualTarget.Create(_baseLayoutTarget.Position, _baseLayoutTarget.Rotation, 1f);
        profile = GetMovementProfile();
        sortOrder = _baseLayoutTarget.SortingOrder;

        // 2. DADOS DE ENTRADA
        Vector3 interactionPoint = _inputManager.MouseWorldPosition;
        float time = Time.time + _randomSeed;

        // 3. PIPELINE DE ESTADO
        ApplyStateVisuals(ref target, ref sortOrder, interactionPoint, time, ref profile);

        // 4. MODIFICADORES EXTERNOS (AQUI! O loop que faltava)
        // Isso resolve o warning. Mesmo que a lista esteja vazia, a arquitetura está ativa.
        foreach (var modifier in _activeModifiers)
        {
            modifier.Apply(ref target, _config, time);
        }

        // 5. SQUASH
        ApplySquashEffect(ref target);
    }

    // ==============================================================================================
    // MODIFICADORES VISUAIS
    // ==============================================================================================
     private CardMovementProfile GetMovementProfile()
    {
        // Cria o perfil base de física
        return new CardMovementProfile
        {
            PositionSmoothTime = _config.PositionSmoothTime,
            ScaleSmoothTime = _config.ScaleSmoothTime,
            RotationSmoothTime = 0.15f, // Valor padrão suave para rotação
            MovementStretchAmount = 0.05f // O efeito "Splash/Juice"
        };
    }

    private void ApplyStateVisuals(ref CardVisualTarget target, ref int sortOrder, Vector3 interactionPoint, float time, ref CardMovementProfile profile)
    {
        switch (CurrentState)
        {
            // AQUI ESTÁ A CONFIRMAÇÃO:
            // O estado Selected agora usa a mesma lógica visual do Idle.
            // Removemos o código que fazia ele subir (SelectedYOffset) e girar (LookRotation).
            case CardVisualState.Idle:
            case CardVisualState.Selected:
                ApplyIdleVisuals(ref target, ref sortOrder, interactionPoint, time, ref profile);
                break;

            case CardVisualState.Dragging:
                ApplyDragVisuals(ref target, ref sortOrder, interactionPoint);
                break;
        }
    }
    private void ApplyIdleVisuals(ref CardVisualTarget target, ref int sortOrder, Vector3 interactionPoint, float time, ref CardMovementProfile profile)
    {
        // A. Flutuação (Senoide)
        float floatY = Mathf.Sin(time * _config.IdleFloatSpeed) * _config.IdleFloatAmount;
        float floatRot = Mathf.Cos(time * (_config.IdleFloatSpeed * 0.5f)) * _config.IdleRotationAmount;

        target.Position += Vector3.up * floatY;
        target.Rotation *= Quaternion.Euler(0, 0, floatRot);
        target.Position.z = _config.IdleZ;

        // B. Hover (Se mouse estiver em cima)
        if (IsHovered)
        {
            // Pop-up e Layer
            target.Position += Vector3.up * _config.PeekYOffset;
            target.Position.z = _config.HoverZ;
            target.Scale = Vector3.one * _config.PeekScale;
            sortOrder = CardSortingConstants.HOVER_LAYER;

            // Tilt 3D
            ApplyDynamicTilt(ref target, interactionPoint);

            // Perfil mais rápido para resposta ao tilt
            profile.RotationSmoothTime = 0.08f; // Mais responsivo que o padrão
        }
    }

    private void ApplyDragVisuals(ref CardVisualTarget target, ref int sortOrder, Vector3 focusPoint)
    {
        // Segue o mouse
        target.Position = new Vector3(focusPoint.x, focusPoint.y, _config.DragZ);

        // Inclinação baseada na "velocidade" (distância do centro)
        float deltaX = (target.Position.x - transform.position.x) * -2f;
        float tiltZ = Mathf.Clamp(deltaX * _config.DragTiltAmount, -_config.DragTiltAmount, _config.DragTiltAmount);
        target.Rotation = Quaternion.Euler(0, 0, tiltZ);

        sortOrder = CardSortingConstants.DRAG_LAYER;
    }

    private void ApplyDynamicTilt(ref CardVisualTarget target, Vector3 focusPoint)
    {
        // Tilt 3D estilo Balatro
        float deltaX = focusPoint.x - target.Position.x;
        float deltaY = focusPoint.y - target.Position.y;

        float normalizedX = Mathf.Clamp(deltaX / _config.TiltInfluenceRadius, -1f, 1f);
        float normalizedY = Mathf.Clamp(deltaY / _config.TiltInfluenceRadius, -1f, 1f);

        float rotY = normalizedX * _config.TiltAngleMax;
        float rotX = -normalizedY * _config.TiltAngleMax;

        target.Rotation = target.Rotation * Quaternion.Euler(rotX, rotY, 0);
    }

    private void ApplySquashEffect(ref CardVisualTarget target)
    {
        // Recupera o valor do squash para 0 suavemente
        _currentSquashVal = Mathf.Lerp(_currentSquashVal, 0f, Time.deltaTime * _config.ClickRecoverySpeed);
        target.Scale += Vector3.one * _currentSquashVal;
    }

    // ==============================================================================================
    // CONTROLE DE ESTADO & INPUT
    // ==============================================================================================

    public void OnDragStart()
    {
        if (CanChangeState()) SetState(CardVisualState.Dragging);
    }
    private void OnDestroy()
    {
        // Se a carta for destruída (ex: queimada, vendida) enquanto o jogador arrasta,
        // precisamos avisar o sistema para soltar o mouse.
        if (CurrentState == CardVisualState.Dragging)
        {
            OnDragEndEvent?.Invoke(this);
        }

        // Limpar lista para ajudar o Garbage Collector (opcional, mas boa prática)
        _activeModifiers.Clear();
    }
    public void OnDragUpdate(Vector2 worldPos)
    {
        // INTENCIONALMENTE VAZIO.
        // Motivo: O CardView usa o Update() padrão para ler o InputManager quando 
        // o estado é 'Dragging'. Este método existe apenas para satisfazer 
        // a interface IDraggable usada pelo PlayerInteraction.
    }
    public void OnDragEnd()
    {
        if (CurrentState == CardVisualState.Dragging) SetState(CardVisualState.Idle);
    }
    public void OnClick() => PerformClick();
    public void OnPointerClick(PointerEventData d) => PerformClick();

    public void OnHoverEnter() => IsHovered = true;
    public void OnHoverExit() => IsHovered = false;

    // Mantemos Select/Deselect para lógica do HandManager, mas visualmente é igual Idle
    public void Select() { if (CanChangeState()) SetState(CardVisualState.Selected); }
    public void Deselect() { if (CurrentState == CardVisualState.Selected) SetState(CardVisualState.Idle); }

    private void PerformClick()
    {
        if (!CanPerformClick()) return;
        _currentSquashVal = -_config.ClickSquashAmount;
        OnClickEvent?.Invoke(this);
    }

    private bool CanPerformClick()
    {
        var gameState = AppCore.Instance?.GameStateManager?.CurrentState ?? GameState.MainMenu;
        bool isAllowed = (gameState == GameState.Playing || gameState == GameState.Shopping);
        bool isFree = (CurrentState != CardVisualState.Dragging && CurrentState != CardVisualState.Consuming);
        return isAllowed && isFree;
    }

    private void SetState(CardVisualState newState)
    {
        if (CurrentState == newState) return;

        // Saída
        if (CurrentState == CardVisualState.Dragging) OnDragEndEvent?.Invoke(this);

        CurrentState = newState;

        // Entrada
        if (CurrentState == CardVisualState.Dragging)
        {
            _movement.ResetPhysicsState();
            OnDragStartEvent?.Invoke(this);
        }
    }

    private bool CanChangeState() => CurrentState != CardVisualState.Consuming;

    // API Externa
    public void UpdateLayoutTarget(HandLayoutCalculator.CardTransformTarget target) => _baseLayoutTarget = target;
}