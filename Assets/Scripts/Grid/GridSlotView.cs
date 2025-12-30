using UnityEngine;
using System; // Necessário para Func e Action

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class GridSlotView : MonoBehaviour, IInteractable, IDropTarget
{
    [Header("Componentes Visuais")]
    [SerializeField] private SpriteRenderer _baseRenderer;      // Chão (Muda de cor com água)
    [SerializeField] private SpriteRenderer _plantRenderer;     // A planta (Muda sprite)
    [SerializeField] private SpriteRenderer _highlightRenderer; // Overlay de Mouse (Liga/Desliga)

    [Header("Cores do Solo")]
    [SerializeField] private Color _dryColor = Color.white;
    [SerializeField] private Color _wetColor = new Color(0.6f, 0.6f, 1f); // Azulado

    [Header("Cores de Highlight")]
    [SerializeField] private Color _highlightColor = new Color(1f, 1f, 1f, 0.4f); // Branco transparente

    public int SlotIndex { get; private set; }

    // --- EVENTOS (A Ponte para o Controller) ---
    // O Controller assina isso para responder "Posso soltar aqui?"
    public event Func<int, CardData, bool> OnCheckDropInteraction;

    // O Controller assina isso para executar "Soltei aqui!"
    public event Action<int, CardData> OnDropInteraction;

    private void Awake()
    {
        ConfigureRenderers();
    }

    public void Initialize(int index)
    {
        SlotIndex = index;

        // Garante estado visual limpo ao nascer
        _plantRenderer.enabled = false;
        _highlightRenderer.enabled = false;
        _baseRenderer.color = _dryColor;
    }

    // --- MÉTODO VISUAL (CHAMADO PELO CONTROLLER) ---

    public void SetVisualState(Sprite plantSprite, bool isWatered)
    {
        // 1. Atualiza Planta
        if (plantSprite != null)
        {
            _plantRenderer.sprite = plantSprite;
            _plantRenderer.enabled = true;
        }
        else
        {
            _plantRenderer.enabled = false;
            _plantRenderer.sprite = null;
        }

        // 2. Atualiza Solo (Seco vs Molhado)
        // Note que isso não interfere no Highlight, pois são renderers diferentes
        _baseRenderer.color = isWatered ? _wetColor : _dryColor;
    }

    // --- INTERFACE IINTERACTABLE (HOVER) ---

    public void OnHoverEnter()
    {
        // Apenas liga o overlay de brilho. Não mexe na cor do chão.
        if (_highlightRenderer != null)
            _highlightRenderer.enabled = true;
    }

    public void OnHoverExit()
    {
        if (_highlightRenderer != null)
            _highlightRenderer.enabled = false;
    }

    public void OnClick()
    {
        // Futuro: Abrir menu de detalhes do slot
    }

    // --- INTERFACE IDROPTARGET (DRAG & DROP) ---

    public bool CanReceive(IDraggable draggable)
    {
        // Verifica se é uma carta
        if (draggable is CardView cardView && cardView.Data != null)
        {
            // PERGUNTA para quem estiver ouvindo (Controller -> Service)
            // Se ninguém estiver ouvindo, retorna false por segurança.
            return OnCheckDropInteraction?.Invoke(SlotIndex, cardView.Data) ?? false;
        }
        return false;
    }

    public void OnReceive(IDraggable draggable)
    {
        if (draggable is CardView cardView)
        {
            // AVISA que recebeu o drop
            OnDropInteraction?.Invoke(SlotIndex, cardView.Data);

            // Remove o destaque visual imediatamente
            OnHoverExit();
        }
    }

    // --- CONFIGURAÇÃO AUTOMÁTICA (SETUP) ---
    // Mantive sua lógica de criar objetos caso eles não existam no inspector
    private void ConfigureRenderers()
    {
        if (_baseRenderer == null) _baseRenderer = GetComponent<SpriteRenderer>();

        // Configura Planta
        if (_plantRenderer == null || _plantRenderer == _baseRenderer)
        {
            _plantRenderer = CreateChildSprite("PlantSprite", 1);
        }

        // Configura Highlight (Novo)
        if (_highlightRenderer == null || _highlightRenderer == _baseRenderer)
        {
            _highlightRenderer = CreateChildSprite("HighlightOverlay", 2);
            _highlightRenderer.color = _highlightColor;

            // O Highlight deve cobrir o slot, então usa o sprite do base se possível
            _highlightRenderer.sprite = _baseRenderer.sprite;
        }
    }

    private SpriteRenderer CreateChildSprite(string name, int orderOffset)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(this.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingLayerID = _baseRenderer.sortingLayerID;
        sr.sortingOrder = _baseRenderer.sortingOrder + orderOffset;
        sr.enabled = false;

        return sr;
    }
}