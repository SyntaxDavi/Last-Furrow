using System; 
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SortingGroup))]
public class CardView : MonoBehaviour, IInteractable, IDraggable, IPointerClickHandler
{
    // --- CONSTANTES (Fim dos números mágicos) ---
    private const string LAYER_IDLE = "Cards_Idle";
    private const string LAYER_DRAG = "Cards_Drag";

    private const float Z_DEPTH_IDLE = 0f;
    private const float Z_DEPTH_DRAG = -5f;

    private int _handIndexOrder = 0;

    [Header("Referencias")]
    [SerializeField] private SortingGroup _sortingGroup;
    [SerializeField] private SpriteRenderer _artRenderer;
    [SerializeField] private TextMeshPro _nameText;
    [SerializeField] private TextMeshPro _costText;

    [Header("Configuração Visual")]
    [SerializeField] private float _moveSpeed = 15f;
    [SerializeField] private float _hoverScale = 1.2f;

    public event Action<CardView> OnDragStartedEvent;
    public event Action<CardView> OnDragEndedEvent;
    public static event Action<CardView> OnCardClickedGlobal;

    // Estado Público (Apenas leitura para inputs, escrita restrita)
    public bool IsDragging { get; private set; }
    public Vector3 TargetPosition { get; set; }
    public int HandIndex { get; set; }
    public CardInstance Instance { get; private set; }

    // Dados: Mantemos apenas para renderização. A lógica não deve ler isso daqui para tomar decisões.
    public CardData Data { get; private set; }

    private Vector3 _baseScale = Vector3.one;
    private bool _isHovered;

    private void Awake()
    {
        if (_sortingGroup == null) _sortingGroup = GetComponent<SortingGroup>();
        _baseScale = transform.localScale;
    }

    public void Initialize(CardData data, CardInstance instance)
    {
        Data = data;
        Instance = instance;
        if (data == null) return;

        if (_nameText) _nameText.text = data.Name;
        if (_costText) _costText.text = data.Cost.ToString();
        if (_artRenderer) _artRenderer.sprite = data.Icon;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. Movimento Suave
        // Usa MoveTowards para Z para garantir precisão exata de profundidade
        float currentZ = transform.position.z;
        float targetZ = IsDragging ? Z_DEPTH_DRAG : Z_DEPTH_IDLE;
        float newZ = Mathf.MoveTowards(currentZ, targetZ, _moveSpeed * dt);

        // Usa Lerp para XY para dar sensação de peso/suavidade
        Vector2 currentXY = transform.position;
        Vector2 targetXY = TargetPosition;
        Vector2 newXY = Vector2.Lerp(currentXY, targetXY, _moveSpeed * dt);

        transform.position = new Vector3(newXY.x, newXY.y, newZ);

        // 2. Escala
        bool showHoverScale = _isHovered && !IsDragging;
        Vector3 targetScale = showHoverScale ? _baseScale * _hoverScale : _baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, _moveSpeed * dt);
    }

    // --- CONTROLE VISUAL ---

    public void SetSortingOrder(int handIndex)
    {
        _handIndexOrder = handIndex;
        UpdateSorting(LAYER_IDLE, _handIndexOrder);
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
        // Volta para a layer de repouso
        UpdateSorting(LAYER_IDLE, _handIndexOrder * 10);
    }

    // --- INTERFACES (Inputs) ---

    public void OnHoverEnter() => _isHovered = true;
    public void OnHoverExit() => _isHovered = false;

    public void OnClick()
    {
        Debug.Log($"Visual Click: {Data?.Name}");
        OnCardClickedGlobal?.Invoke(this);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsDragging)
        {
            OnCardClickedGlobal?.Invoke(this);
        }
    }
    // --- IDraggable Implementation ---

    public void OnDragStart()
    {
        IsDragging = true;
        // Joga para a layer exclusiva de Drag (acima de todas as outras cartas)
        UpdateSorting(LAYER_DRAG, 100);
        OnDragStartedEvent?.Invoke(this);
    }

    public void OnDragUpdate(Vector2 worldPosition)
    {
        // View apenas atualiza para onde ela QUER ir. 
        // Não decide regras de colisão.
        TargetPosition = new Vector3(worldPosition.x, worldPosition.y, 0);
    }

    public void OnDragEnd()
    {
        IsDragging = false;
        OnDragEndedEvent?.Invoke(this);
        RestoreSorting();
    }
}