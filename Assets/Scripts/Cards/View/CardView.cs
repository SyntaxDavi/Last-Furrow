using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using System;
using TMPro;

public enum CardVisualState
{
    Idle,       // Flutuando na mão
    Selected,   // Clicada (Subida)
    Dragging,   // Arrastando
    Consuming   // Morrendo
}

[RequireComponent(typeof(SortingGroup))]
public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    // ... (Referências e Config injetado mantidos igual) ...
    [Header("Configuração Injetada")]
    private CardVisualConfig _visualConfig;

    [SerializeField] private SortingGroup _sortingGroup;
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;

    // ESTADO
    public CardVisualState CurrentState { get; private set; } = CardVisualState.Idle;
    public CardData Data { get; private set; }
    public CardInstance Instance { get; private set; }

    // ALVOS
    private HandLayoutCalculator.CardTransformTarget _layoutTarget;

    // FÍSICA E FEELING
    private Vector3 _currentVelocityPos;
    private float _currentVelocityScale;
    private float _randomSeed; // Para cada carta flutuar em um tempo diferente
    private bool _isMouseOver; // Controle sutil separado do Estado

    // EVENTOS
    public event Action<CardView> OnDragStartEvent;
    public event Action<CardView> OnDragEndEvent;
    public event Action<CardView> OnClickEvent; // HandManager vai ouvir isso

    public void Initialize(CardData data, CardInstance instance, CardVisualConfig visualConfig)
    {
        Data = data;
        Instance = instance;
        _visualConfig = visualConfig ?? ScriptableObject.CreateInstance<CardVisualConfig>();

        if (Data != null)
        {
            _nameText.text = Data.Name;
            _artRenderer.sprite = Data.Icon;
        }

        // Gera um número aleatório único para essa carta
        _randomSeed = UnityEngine.Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (CurrentState == CardVisualState.Consuming) return;
        if (CurrentState == CardVisualState.Dragging) return;

        UpdateMotionLogic();
    }

    private void UpdateMotionLogic()
    {
        Vector3 finalPos = _layoutTarget.Position;
        Quaternion finalRot = _layoutTarget.Rotation;
        Vector3 finalScale = Vector3.one;
        int finalSort = _layoutTarget.SortingOrder;

        // --- 1. EFEITO BALATRO (FLUTUAÇÃO) ---
        // Só flutua se não estiver sendo arrastada
        if (CurrentState == CardVisualState.Idle || CurrentState == CardVisualState.Selected)
        {
            float time = Time.time + _randomSeed;

            // Flutuação Y (Sobe e desce suavemente)
            float floatY = Mathf.Sin(time * _visualConfig.IdleFloatSpeed) * _visualConfig.IdleFloatAmount;

            // Rotação Z (Balança bem pouquinho)
            float floatRot = Mathf.Cos(time * (_visualConfig.IdleFloatSpeed * 0.5f)) * _visualConfig.IdleRotationAmount;

            finalPos.y += floatY;
            finalRot *= Quaternion.Euler(0, 0, floatRot);
        }

        // --- 2. LÓGICA DE ESTADOS ---
        switch (CurrentState)
        {
            case CardVisualState.Selected: // CLICADA
                finalPos += Vector3.up * _visualConfig.SelectedYOffset;
                finalPos.z = -2f;
                finalRot = Quaternion.identity; // Fica reta
                finalScale = Vector3.one * _visualConfig.SelectedScale;
                finalSort = CardSortingConstants.HOVER_LAYER;
                break;

            case CardVisualState.Idle:
                // Se o mouse está em cima (mas não clicou), dá um "Peek" (espiadinha)
                if (_isMouseOver)
                {
                    finalPos += Vector3.up * _visualConfig.PeekYOffset;
                    finalScale = Vector3.one * _visualConfig.PeekScale;
                    finalSort = _layoutTarget.SortingOrder + 5; // Levemente acima das vizinhas
                }
                break;
        }

        // --- 3. APLICAÇÃO FÍSICA (SmoothDamp) ---
        // (Igual ao anterior, código omitido para brevidade, mas mantenha o SmoothDamp aqui)
        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref _currentVelocityPos, _visualConfig.PositionSmoothTime);
        float newScale = Mathf.SmoothDamp(transform.localScale.x, finalScale.x, ref _currentVelocityScale, _visualConfig.ScaleSmoothTime);
        transform.localScale = Vector3.one * newScale;
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, Time.deltaTime * _visualConfig.RotationSpeed);
        if (_sortingGroup.sortingOrder != finalSort) _sortingGroup.sortingOrder = finalSort;
    }

    // --- MÉTODOS PÚBLICOS PARA O MANAGER CONTROLAR ---

    public void Select()
    {
        if (CurrentState == CardVisualState.Dragging) return;
        CurrentState = CardVisualState.Selected;
    }

    public void Deselect()
    {
        if (CurrentState == CardVisualState.Dragging) return;
        CurrentState = CardVisualState.Idle;
    }

    // --- INPUTS ---

    public void UpdateLayoutTarget(HandLayoutCalculator.CardTransformTarget target) => _layoutTarget = target;

    public void OnHoverEnter() => _isMouseOver = true; 
    public void OnHoverExit() => _isMouseOver = false;
    public void OnClick() => OnClickEvent?.Invoke(this);
    public void OnPointerClick(PointerEventData d) => OnClickEvent?.Invoke(this);

    // DRAG (Mantido igual)
    public void OnDragStart()
    {
        CurrentState = CardVisualState.Dragging;
        _isMouseOver = false; // Limpa hover ao arrastar
        // ... zerar velocities ...
        OnDragStartEvent?.Invoke(this);
    }
    public void OnDragUpdate(Vector2 worldPos)
    {
        // 1. Posição: Segue o mouse, mas mantendo a profundidade (Z) configurada
        Vector3 targetPos = new Vector3(worldPos.x, worldPos.y, _visualConfig.DragZDepth);

        // Usamos Lerp aqui para um "hard follow" (segue rápido) em vez do SmoothDamp do Idle,
        // para a carta não parecer "bêbada" enquanto você tenta mirar no slot.
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f);

        // 2. Feeling (Tilt): Inclina a carta baseado na velocidade lateral
        // Se a carta está indo para a direita, ela inclina para a esquerda (efeito de arrasto de ar)
        float deltaX = (targetPos.x - transform.position.x) * -2f;

        // Limita a inclinação para não girar 360 graus
        float tiltZ = Mathf.Clamp(deltaX * _visualConfig.DragTiltAmount, -_visualConfig.DragTiltAmount, _visualConfig.DragTiltAmount);

        Quaternion targetRot = Quaternion.Euler(0, 0, tiltZ);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _visualConfig.DragTiltSpeed);
    }
    public void OnDragEnd()
    {
        // Ao soltar, volta para Idle (não selecionada)
        CurrentState = CardVisualState.Idle;
        OnDragEndEvent?.Invoke(this);
    }
}