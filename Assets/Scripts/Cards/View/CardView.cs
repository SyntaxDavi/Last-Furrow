using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems; 
using TMPro;
using System;

[RequireComponent(typeof(SortingGroup))]
public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    // --- LAYERS (Configuração de Arquitetura Visual) ---
    private const string LAYER_IDLE = "Cards_Idle";
    private const string LAYER_DRAG = "Cards_Drag";

    [Header("Referências")]
    [SerializeField] private SortingGroup _sortingGroup;
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;
    [SerializeField] private TextMeshPro _costText;

    [Header("Configuração Visual")]
    [SerializeField] private float _moveSpeed = 15f;
    [SerializeField] private float _rotationSpeed = 10f; // Velocidade de girar
    [SerializeField] private float _hoverScale = 1.2f;

    // Eventos
    public event Action<CardView> OnDragStartedEvent;
    public event Action<CardView> OnDragEndedEvent;
    public static event Action<CardView> OnCardClickedGlobal; // Para a loja

    // Estado Público
    public bool IsDragging { get; private set; }
    public Vector3 TargetPosition { get; set; }
    public Quaternion TargetRotation { get; set; } // NOVO: Rotação alvo
    public int HandIndex { get; set; }

    // Dados
    public CardData Data { get; private set; }
    public CardInstance Instance { get; private set; }

    private Vector3 _baseScale = Vector3.one;
    private bool _isHovered;
    private int _currentOrder;

    private void Awake()
    {
        if (_sortingGroup == null) _sortingGroup = GetComponent<SortingGroup>();
        _baseScale = transform.localScale;
        TargetRotation = Quaternion.identity; // Começa sem rotação
    }

    public void Initialize(CardData data, CardInstance instance)
    {
        Data = data;
        Instance = instance;
        if (data == null) return;

        if (_nameText) _nameText.text = data.Name;
        if (_costText) _costText.text = data.Cost.ToString();
        if (_artRenderer) _artRenderer.sprite = data.Icon;

        name = $"Card_{data.Name}_{instance.UniqueID.Substring(0, 4)}";
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. Movimento (Posição)
        float currentZ = transform.position.z;
        float targetZ = IsDragging ? -5f : 0f; // Traz pra frente no drag

        Vector2 newXY = Vector2.Lerp(transform.position, TargetPosition, _moveSpeed * dt);
        float newZ = Mathf.MoveTowards(currentZ, targetZ, _moveSpeed * dt);

        transform.position = new Vector3(newXY.x, newXY.y, newZ);

        // 2. Rotação (NOVO: Slerp para girar suave)
        // Se estiver arrastando, zera a rotação para ficar reto
        Quaternion finalRot = IsDragging ? Quaternion.identity : TargetRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, _rotationSpeed * dt);

        // 3. Escala
        bool showHoverScale = _isHovered && !IsDragging;
        Vector3 targetScale = showHoverScale ? _baseScale * _hoverScale : _baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, _moveSpeed * dt);
    }

    // --- VISUAL ---

    public void SetSortingOrder(int order)
    {
        _currentOrder = order;
        UpdateSorting(LAYER_IDLE, order);
    }

    private void UpdateSorting(string layerName, int order)
    {
        if (_sortingGroup)
        {
            _sortingGroup.sortingLayerName = layerName;
            _sortingGroup.sortingOrder = order;
        }
    }

    public void RestoreSorting()
    {
        UpdateSorting(LAYER_IDLE, _currentOrder);
    }

    // --- INPUTS ---

    public void OnHoverEnter() => _isHovered = true;
    public void OnHoverExit() => _isHovered = false;

    // Clique Físico (Raycast do PlayerInteraction)
    public void OnClick()
    {
        Debug.Log($"Click Físico: {Data?.Name}");
        OnCardClickedGlobal?.Invoke(this);
    }

    // Clique UI (Se tiver Physics2DRaycaster) - Fallback
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsDragging) OnCardClickedGlobal?.Invoke(this);
    }

    // --- DRAG ---

    public void OnDragStart()
    {
        IsDragging = true;
        UpdateSorting(LAYER_DRAG, 100);
        OnDragStartedEvent?.Invoke(this);
    }

    public void OnDragUpdate(Vector2 worldPosition)
    {
        TargetPosition = new Vector3(worldPosition.x, worldPosition.y, 0);
    }

    public void OnDragEnd()
    {
        IsDragging = false;
        OnDragEndedEvent?.Invoke(this);
        RestoreSorting();
    }
}